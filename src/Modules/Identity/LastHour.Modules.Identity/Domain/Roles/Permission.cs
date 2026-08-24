using LastHour.BuildingBlocks.SharedKernel.Domain;
using LastHour.BuildingBlocks.SharedKernel.Domain.Exceptions;

namespace LastHour.Modules.Identity.Domain.Roles;

/// <summary>
/// A single, stable application capability (for example <c>Users.Read</c>). Permissions are
/// defined by the centralized <c>PermissionCatalog</c> in the application layer and seeded into
/// the database; they are never created from client input. The <see cref="Code"/> is the
/// deterministic identity of the capability and is protected by a database unique constraint,
/// so authorization checks and role grants never depend on a mutable display label. The
/// aggregate deliberately carries no business behavior beyond the code invariant: which roles
/// hold a permission is state owned by the <see cref="Role"/> aggregate.
/// </summary>
public sealed class Permission : AggregateRoot<PermissionId>, IAuditableEntity
{
    /// <summary>
    /// The maximum length of a permission code.
    /// </summary>
    public const int MaxCodeLength = 100;

    private readonly List<RolePermissionGrant> _rolePermissions = new List<RolePermissionGrant>();

    private Permission()
    {
        // EF Core materialization only.
    }

    private Permission(PermissionId id, string code, string? description)
    {
        Id = id;
        Code = code;
        Description = description;
    }

    /// <summary>
    /// Gets the stable capability code using the <c>Resource.Action</c> convention (for
    /// example <c>Users.Read</c>). Unique across the catalog and the database.
    /// </summary>
    public string Code { get; private set; } = null!;

    /// <summary>
    /// Gets the human-readable purpose of the permission, when one is defined.
    /// </summary>
    public string? Description { get; private set; }

    /// <summary>
    /// Gets the roles that hold this permission. The inverse of the grants owned by
    /// <see cref="Role"/>, used by authorization resolution queries.
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
    /// Creates a permission with a stable code. The code format (Resource.Action) is the
    /// responsibility of the application-layer catalog; this aggregate enforces only the
    /// presence and length invariant.
    /// </summary>
    /// <param name="code">The stable capability code.</param>
    /// <param name="description">An optional human-readable purpose.</param>
    /// <returns>A new <see cref="Permission"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="code"/> is <see langword="null"/>.</exception>
    /// <exception cref="BusinessRuleValidationException"><paramref name="code"/> is empty or
    /// exceeds <see cref="MaxCodeLength"/> characters.</exception>
    public static Permission Create(string code, string? description = null)
    {
        ArgumentNullException.ThrowIfNull(code);

        string trimmed = code.Trim();
        if (trimmed.Length == 0)
        {
            throw new BusinessRuleValidationException(
                new PermissionCodeRule("A permission code is required."));
        }

        if (trimmed.Length > MaxCodeLength)
        {
            throw new BusinessRuleValidationException(
                new PermissionCodeRule($"A permission code cannot exceed {MaxCodeLength} characters."));
        }

        return new Permission(PermissionId.New(), trimmed, description?.Trim());
    }

    private sealed class PermissionCodeRule : IBusinessRule
    {
        public PermissionCodeRule(string message)
        {
            Message = message;
        }

        public bool IsSatisfied => false;

        public string Message { get; }
    }
}
