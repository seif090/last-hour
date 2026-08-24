using Microsoft.AspNetCore.Authorization;

namespace LastHour.Api.Security.Authorization;

/// <summary>
/// The authorization requirement for a single application capability. The permission is the
/// stable catalog code (for example <c>Users.Read</c>) from the centralized permission
/// catalog, never a client-supplied value. The requirement is immutable and carries no
/// database access or authorization logic — the handler decides.
/// </summary>
public sealed record PermissionRequirement : IAuthorizationRequirement
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PermissionRequirement"/> class.
    /// </summary>
    /// <param name="permission">The required permission code from the centralized catalog.</param>
    /// <exception cref="ArgumentNullException"><paramref name="permission"/> is <see langword="null"/>.</exception>
    public PermissionRequirement(string permission)
    {
        ArgumentNullException.ThrowIfNull(permission);
        Permission = permission;
    }

    /// <summary>
    /// Gets the required permission code.
    /// </summary>
    public string Permission { get; }
}
