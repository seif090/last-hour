using LastHour.BuildingBlocks.Application.Cqrs;

namespace LastHour.Modules.Identity.Application.Queries;

/// <summary>
/// Resolves the permissions granted to a role.
/// </summary>
public sealed record GetRolePermissionsQuery : IQuery<IReadOnlyList<PermissionInfo>>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GetRolePermissionsQuery"/> class.
    /// </summary>
    /// <param name="roleId">The role whose permissions are resolved.</param>
    public GetRolePermissionsQuery(Guid roleId)
    {
        RoleId = roleId;
    }

    /// <summary>
    /// Gets the role whose permissions are resolved.
    /// </summary>
    public Guid RoleId { get; }
}
