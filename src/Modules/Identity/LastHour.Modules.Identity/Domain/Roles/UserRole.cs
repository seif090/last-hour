using LastHour.Modules.Identity.Domain.Users;

namespace LastHour.Modules.Identity.Domain.Roles;

/// <summary>
/// The association between a <see cref="User"/> and a <see cref="Role"/>. Modeled as an
/// entity with a composite key so the relationship itself is a first-class, constrained row:
/// the database enforces that a user can hold the same role only once and that both sides
/// reference existing rows. The aggregate roots own the assignment invariants — a user gains
/// a role only through <see cref="User.AssignRole"/> and a role is linked to users only
/// through this association — while the uniqueness is additionally enforced by the composite
/// primary key. No privilege data is ever client-controlled; assignments originate from
/// application use cases only.
/// </summary>
public sealed class UserRole
{
    private UserRole()
    {
        // EF Core materialization only.
    }

    private UserRole(UserId userId, RoleId roleId)
    {
        UserId = userId;
        RoleId = roleId;
    }

    /// <summary>
    /// Gets the identifier of the user holding the role.
    /// </summary>
    public UserId UserId { get; private set; } = null!;

    /// <summary>
    /// Gets the identifier of the assigned role.
    /// </summary>
    public RoleId RoleId { get; private set; } = null!;

    /// <summary>
    /// Creates a new assignment between a user and a role. The user aggregate remains the
    /// intended creation path (<see cref="User.AssignRole"/>); this factory exists for
    /// persistence and testing code that must construct the association directly.
    /// </summary>
    /// <param name="userId">The user receiving the role.</param>
    /// <param name="roleId">The role being assigned.</param>
    /// <returns>A new <see cref="UserRole"/> association.</returns>
    public static UserRole Create(UserId userId, RoleId roleId) => new UserRole(userId, roleId);
}
