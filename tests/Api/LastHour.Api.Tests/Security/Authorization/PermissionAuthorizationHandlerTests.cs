using System.Security.Claims;
using LastHour.Api.Security.Authorization;
using LastHour.Modules.Identity.Domain.Users;
using Microsoft.AspNetCore.Authorization;

namespace LastHour.Api.Tests.Security.Authorization;

/// <summary>
/// Exercises the permission authorization handler: an authenticated caller whose live
/// permission set contains the required code succeeds, and every other shape — unauthenticated,
/// no permission, missing or malformed subject — fails safely.
/// </summary>
public class PermissionAuthorizationHandlerTests
{
    private static readonly Guid UserIdValue = Guid.NewGuid();

    private static ClaimsPrincipal AuthenticatedPrincipal(string? subject = null)
    {
        var claims = new List<Claim>();
        if (subject is not null)
        {
            claims.Add(new Claim("sub", subject));
        }

        return new ClaimsPrincipal(new ClaimsIdentity(claims, authenticationType: "Bearer"));
    }

    private static AuthorizationHandlerContext CreateContext(
        PermissionRequirement requirement,
        ClaimsPrincipal principal)
        => new(new[] { requirement }, principal, resource: null);

    private static async Task<bool> HandlesAsync(
        StubRoleRepository roles,
        ClaimsPrincipal principal,
        string requiredPermission)
    {
        var requirement = new PermissionRequirement(requiredPermission);
        var handler = new PermissionAuthorizationHandler(roles);
        AuthorizationHandlerContext context = CreateContext(requirement, principal);

        await handler.HandleAsync(context);

        return context.HasSucceeded;
    }

    [Fact]
    public async Task Handle_AuthenticatedUserWithPermission_Succeeds()
    {
        var roles = new StubRoleRepository();
        roles.SetPermissions(UserId.Create(UserIdValue), "Users.Read", "Roles.Read");

        bool succeeded = await HandlesAsync(
            roles,
            AuthenticatedPrincipal(UserIdValue.ToString()),
            "Users.Read");

        Assert.True(succeeded);
    }

    [Fact]
    public async Task Handle_AuthenticatedUserWithoutPermission_Fails()
    {
        var roles = new StubRoleRepository();
        roles.SetPermissions(UserId.Create(UserIdValue), "Roles.Read");

        bool succeeded = await HandlesAsync(
            roles,
            AuthenticatedPrincipal(UserIdValue.ToString()),
            "Users.Manage");

        Assert.False(succeeded);
    }

    [Fact]
    public async Task Handle_UserWithNoRoles_Fails()
    {
        var roles = new StubRoleRepository();

        bool succeeded = await HandlesAsync(
            roles,
            AuthenticatedPrincipal(UserIdValue.ToString()),
            "Users.Read");

        Assert.False(succeeded);
    }

    [Fact]
    public async Task Handle_UnauthenticatedPrincipal_Fails()
    {
        var roles = new StubRoleRepository();
        roles.SetPermissions(UserId.Create(UserIdValue), "Users.Read");

        bool succeeded = await HandlesAsync(
            roles,
            new ClaimsPrincipal(new ClaimsIdentity()),
            "Users.Read");

        Assert.False(succeeded);
    }

    [Fact]
    public async Task Handle_MissingSubjectClaim_Fails()
    {
        var roles = new StubRoleRepository();
        roles.SetPermissions(UserId.Create(UserIdValue), "Users.Read");

        bool succeeded = await HandlesAsync(
            roles,
            AuthenticatedPrincipal(subject: null),
            "Users.Read");

        Assert.False(succeeded);
    }

    [Fact]
    public async Task Handle_MalformedSubjectClaim_Fails()
    {
        var roles = new StubRoleRepository();
        roles.SetPermissions(UserId.Create(UserIdValue), "Users.Read");

        bool succeeded = await HandlesAsync(
            roles,
            AuthenticatedPrincipal("not-a-guid"),
            "Users.Read");

        Assert.False(succeeded);
    }

    [Fact]
    public async Task Handle_PermissionGrantedThroughMultipleRoles_SucceedsExactlyOnce()
    {
        // The repository guarantees a deduplicated set; the handler treats the set as a
        // membership check, so overlapping grants cannot double-succeed or fail.
        var roles = new StubRoleRepository();
        roles.SetPermissions(UserId.Create(UserIdValue), "Users.Read", "Users.Read", "Roles.Read");

        bool succeeded = await HandlesAsync(
            roles,
            AuthenticatedPrincipal(UserIdValue.ToString()),
            "Users.Read");

        Assert.True(succeeded);
    }

    [Fact]
    public async Task Handle_UnknownPermission_AlwaysFails()
    {
        var roles = new StubRoleRepository();
        roles.SetPermissions(UserId.Create(UserIdValue), "Users.Read");

        bool succeeded = await HandlesAsync(
            roles,
            AuthenticatedPrincipal(UserIdValue.ToString()),
            "Does.Not.Exist");

        Assert.False(succeeded);
    }
}
