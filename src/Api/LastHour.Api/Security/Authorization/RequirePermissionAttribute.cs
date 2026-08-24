using Microsoft.AspNetCore.Authorization;

namespace LastHour.Api.Security.Authorization;

/// <summary>
/// Declares the permission required to invoke an endpoint. The attribute is pure metadata: it
/// maps a stable catalog code (for example <c>Users.Read</c>) onto the <c>Permission.&lt;code&gt;</c>
/// policy name that <see cref="PermissionPolicyProvider"/> resolves into a
/// <see cref="PermissionRequirement"/>. No authorization logic lives here — the requirement
/// and its handler own the decision.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
public sealed class RequirePermissionAttribute : AuthorizeAttribute
{
    /// <summary>
    /// The prefix that distinguishes permission policies from other policy names. Permission
    /// codes are appended verbatim, so <c>Users.Read</c> maps to <c>Permission.Users.Read</c>.
    /// </summary>
    public const string PolicyPrefix = "Permission.";

    /// <summary>
    /// Initializes a new instance of the <see cref="RequirePermissionAttribute"/> class.
    /// </summary>
    /// <param name="permission">The required permission code from the centralized catalog.</param>
    /// <exception cref="ArgumentNullException"><paramref name="permission"/> is <see langword="null"/>.</exception>
    public RequirePermissionAttribute(string permission)
    {
        ArgumentNullException.ThrowIfNull(permission);
        Policy = $"{PolicyPrefix}{permission}";
    }
}
