using LastHour.BuildingBlocks.SharedKernel.Domain;
using LastHour.BuildingBlocks.SharedKernel.Domain.Exceptions;
using LastHour.Modules.Identity.Domain.Users;

namespace LastHour.Modules.Identity.Domain.Roles;

/// <summary>
/// The authorization role aggregate root. A role is a named, normalized collection of
/// permissions (<see cref="RolePermissionGrant"/>) that can be assigned to users
/// (<see cref="UserRole"/>). The name is a <see cref="RoleName"/> so normalization and
/// uniqueness live in one place, and the database enforces both the unique normalized name
/// and the composite keys of the association rows, so duplicate roles and duplicate grants
/// are impossible even under concurrency. The aggregate owns the grant invariants: a
/// permission can be assigned or revoked only through this type, and a duplicate grant is
/// rejected. Which users hold the role is state owned by the <see cref="User"/> aggregate;
/// this side exposes only the read-only association for authorization resolution.
/// </summary>
public sealed class Role : AggregateRoot<RoleId>, IAuditableEntity
{
    /// <summary>
    /// The maximum length of a role description.
    /// </summary>
    public const int MaxDescriptionLength = 256;

    private readonly List<UserRole> _userRoles = new List<UserRole>();

    private readonly List<RolePermissionGrant> _rolePermissions = new List<RolePermissionGrant>();

    private Role()
    {
        // EF Core materialization only.
    }

    private Role(RoleId id, RoleName name, string? description)
    {
        Id = id;
        Name = name;
        Description = description;
    }

    /// <summary>
    /// Gets the role name. The original case is preserved for display; equality and lookups
    /// use <see cref="RoleName.NormalizedValue"/>.
    /// </summary>
    public RoleName Name { get; private set; } = null!;

    /// <summary>
    /// Gets the human-readable purpose of the role, when one is defined.
    /// </summary>
    public string? Description { get; private set; }

    /// <summary>
    /// Gets the users holding this role. The inverse of the assignments owned by
    /// <see cref="User"/>, used by authorization resolution queries.
    /// </summary>
    public IReadOnlyCollection<UserRole> UserRoles => _userRoles;

    /// <summary>
    /// Gets the permissions granted to this role.
    /// </summary>
    public IReadOnlyCollection<RolePermissionGrant> RolePermissions => _rolePermissions;

    /// <inheritdoc/>
    public DateTime CreatedAt { get; set; }

    /// <inheritdoc/>
    public string? CreatedBy { get; set; }

    /// <inheritdoc/>
    public DateTime? UpdatedAt { get; set; }

    /// <inheritdoc/>
    public string? UpdatedBy { get; set; }

    /// <summary>
    /// Creates a role with a validated, normalized name.
    /// </summary>
    /// <param name="name">The role name to validate and normalize.</param>
    /// <param name="description">An optional human-readable purpose.</param>
    /// <returns>A new <see cref="Role"/>.</returns>
    /// <exception cref="InvalidRoleNameException"><paramref name="name"/> violates the
    /// role-name invariant.</exception>
    public static Role Create(string name, string? description = null)
    {
        RoleName roleName = RoleName.From(name);
        string? trimmedDescription = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        if (trimmedDescription?.Length > MaxDescriptionLength)
        {
            throw new InvalidRoleNameException($"A role description cannot exceed {MaxDescriptionLength} characters.");
        }

        return new Role(RoleId.New(), roleName, trimmedDescription);
    }

    /// <summary>
    /// Grants a permission to the role. Assigning the same permission twice is rejected so a
    /// grant is a set membership, never a count.
    /// </summary>
    /// <param name="permissionId">The permission to grant.</param>
    /// <exception cref="BusinessRuleValidationException">The permission is already granted.</exception>
    public void AssignPermission(PermissionId permissionId)
    {
        if (HasPermission(permissionId))
        {
            throw new BusinessRuleValidationException(
                new RolePermissionDuplicateRule($"The role already holds permission '{permissionId}'."));
        }

        _rolePermissions.Add(RolePermissionGrant.Create(Id, permissionId));
    }

    /// <summary>
    /// Revokes a permission from the role.
    /// </summary>
    /// <param name="permissionId">The permission to revoke.</param>
    /// <exception cref="BusinessRuleValidationException">The role does not hold the permission.</exception>
    public void RevokePermission(PermissionId permissionId)
    {
        RolePermissionGrant? grant = _rolePermissions.FirstOrDefault(grant => grant.PermissionId == permissionId);
        if (grant is null)
        {
            throw new BusinessRuleValidationException(
                new RolePermissionMissingRule($"The role does not hold permission '{permissionId}'."));
        }

        _rolePermissions.Remove(grant);
    }

    /// <summary>
    /// Determines whether the role currently holds the specified permission.
    /// </summary>
    /// <param name="permissionId">The permission to check.</param>
    /// <returns><see langword="true"/> when the permission is granted; otherwise <see langword="false"/>.</returns>
    public bool HasPermission(PermissionId permissionId)
        => _rolePermissions.Any(grant => grant.PermissionId == permissionId);

    private sealed class RolePermissionDuplicateRule : IBusinessRule
    {
        public RolePermissionDuplicateRule(string message)
        {
            Message = message;
        }

        public bool IsSatisfied => false;

        public string Message { get; }
    }

    private sealed class RolePermissionMissingRule : IBusinessRule
    {
        public RolePermissionMissingRule(string message)
        {
            Message = message;
        }

        public bool IsSatisfied => false;

        public string Message { get; }
    }
}
