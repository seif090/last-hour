using LastHour.Api.Security.Authentication;
using LastHour.Modules.Identity.Domain.Roles;
using LastHour.Modules.Identity.Domain.Users;
using Microsoft.AspNetCore.Authorization;

namespace LastHour.Api.Security.Authorization;

/// <summary>
/// Evaluates a <see cref="PermissionRequirement"/> against the caller's live permission set.
/// The user identity always comes from the validated access-token claims (via
/// <see cref="ClaimsPrincipalExtensions.GetUserId"/>), never from request data. The effective
/// permissions are resolved through the module's dedicated authorization query
/// (<see cref="IRoleRepository.GetPermissionsForUserAsync"/>), which is a single database
/// projection — so a permission check never degrades into N+1 lookups and always reflects the
/// current role and permission grants (the documented consistency model). An unauthenticated
/// principal, a token without a valid subject, or a user holding no matching permission all
/// fail the requirement safely; no internal state is exposed to the caller.
/// </summary>
public sealed class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    private readonly IRoleRepository _roles;

    /// <summary>
    /// Initializes a new instance of the <see cref="PermissionAuthorizationHandler"/> class.
    /// </summary>
    /// <param name="roles">The role repository used to resolve the caller's effective permissions.</param>
    /// <exception cref="ArgumentNullException"><paramref name="roles"/> is <see langword="null"/>.</exception>
    public PermissionAuthorizationHandler(IRoleRepository roles)
    {
        ArgumentNullException.ThrowIfNull(roles);
        _roles = roles;
    }

    /// <inheritdoc/>
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        if (context.User.Identity?.IsAuthenticated != true)
        {
            // Unauthenticated: the requirement fails without claiming success, so the
            // authorization middleware surfaces the framework challenge (401).
            return;
        }

        Guid? userId = context.User.GetUserId();
        if (userId is null)
        {
            // Authenticated token without a usable subject: cannot resolve permissions, so
            // the requirement fails.
            return;
        }

        IReadOnlyList<Permission> permissions = await _roles
            .GetPermissionsForUserAsync(UserId.Create(userId.Value), context.GetCancellationToken())
            .ConfigureAwait(false);

        // The repository returns a deduplicated set, so a permission granted through several
        // roles still satisfies the requirement exactly once.
        if (permissions.Any(permission => string.Equals(
                permission.Code,
                requirement.Permission,
                StringComparison.Ordinal)))
        {
            context.Succeed(requirement);
        }
    }
}

/// <summary>
/// The authorization-handler extensions used by <see cref="PermissionAuthorizationHandler"/>.
/// </summary>
internal static class AuthorizationHandlerContextExtensions
{
    /// <summary>
    /// Gets the request cancellation token from the authorization context, when one is
    /// available.
    /// </summary>
    /// <param name="context">The authorization handler context.</param>
    /// <returns>The request cancellation token, or a cancelled-safe default when unavailable.</returns>
    public static CancellationToken GetCancellationToken(this AuthorizationHandlerContext context)
    {
        if (context.Resource is HttpContext httpContext)
        {
            return httpContext.RequestAborted;
        }

        return CancellationToken.None;
    }
}
