namespace LastHour.Modules.Identity.Domain.Roles;

/// <summary>
/// The grant of a <see cref="Permission"/> to a <see cref="Role"/>. Modeled as an entity with
/// a composite key so the grant itself is a first-class, constrained row: the database
/// enforces that a role can hold the same permission only once and that both sides reference
/// existing rows. The <see cref="Role"/> aggregate owns the grant invariants (a role gains a
/// permission only through <see cref="Role.AssignPermission"/>), while the composite primary
/// key enforces uniqueness independently of the application. Deleting a referenced role or
/// permission is prevented by the database (restrict referential action), so authorization
/// state can never be orphaned silently.
/// </summary>
public sealed class RolePermissionGrant
{
    private RolePermissionGrant()
    {
        // EF Core materialization only.
    }

    private RolePermissionGrant(RoleId roleId, PermissionId permissionId)
    {
        RoleId = roleId;
        PermissionId = permissionId;
    }

    /// <summary>
    /// Gets the identifier of the role holding the permission.
    /// </summary>
    public RoleId RoleId { get; private set; } = null!;

    /// <summary>
    /// Gets the identifier of the granted permission.
    /// </summary>
    public PermissionId PermissionId { get; private set; } = null!;

    /// <summary>
    /// Gets the role that holds the permission. Required navigation used by authorization
    /// resolution (permissions of a user flow through the role).
    /// </summary>
    public Role Role { get; private set; } = null!;

    /// <summary>
    /// Creates a new grant of a permission to a role. The role aggregate remains the intended
    /// creation path (<see cref="Role.AssignPermission"/>); this factory exists for
    /// persistence and testing code that must construct the association directly.
    /// </summary>
    /// <param name="roleId">The role receiving the permission.</param>
    /// <param name="permissionId">The permission being granted.</param>
    /// <returns>A new <see cref="RolePermissionGrant"/>.</returns>
    public static RolePermissionGrant Create(RoleId roleId, PermissionId permissionId) => new RolePermissionGrant(roleId, permissionId);
}
