using LastHour.Modules.Identity.Domain.Roles;
using LastHour.Modules.Identity.Domain.Users;

namespace LastHour.Modules.Identity.Tests.Application.TestSupport;

/// <summary>
/// Deterministic in-memory stand-in for the role repository. Roles are keyed by id and by
/// normalized name exactly like the production lookup, and permissions are registered as real
/// entities so the authorization queries resolve genuine codes and descriptions. The queries
/// compute the sets over the in-memory aggregates, mirroring the single-query resolution of
/// the EF implementation.
/// </summary>
public sealed class FakeRoleRepository : IRoleRepository
{
    private readonly Dictionary<RoleId, Role> _roles = new();
    private readonly Dictionary<string, Role> _byNormalizedName = new(StringComparer.Ordinal);
    private readonly Dictionary<PermissionId, Permission> _permissions = new();
    private readonly FakeUserRepository? _users;
    private readonly FakePermissionRepository? _permissionStore;

    /// <summary>
    /// Initializes a new instance of the <see cref="FakeRoleRepository"/> class.
    /// </summary>
    /// <param name="users">The user fake whose assignments the authorization queries read, or
    /// <see langword="null"/> when assignments are asserted through the role aggregates
    /// directly. In the real model the role side carries the inverse navigation; the in-memory
    /// model holds assignments on the user aggregate, so the role queries delegate to the
    /// users fake when it is provided.</param>
    /// <param name="permissionStore">The permission fake whose entities the permission
    /// resolution queries read, or <see langword="null"/> when permissions are registered
    /// through this fake directly.</param>
    public FakeRoleRepository(FakeUserRepository? users = null, FakePermissionRepository? permissionStore = null)
    {
        _users = users;
        _permissionStore = permissionStore;
    }

    /// <summary>
    /// Gets all stored roles, for assertions.
    /// </summary>
    public IReadOnlyCollection<Role> All => _roles.Values;

    /// <summary>
    /// Gets all stored permissions, for assertions.
    /// </summary>
    public IReadOnlyCollection<Permission> Permissions => _permissions.Values;

    /// <summary>
    /// Creates and stores a permission with the given code.
    /// </summary>
    /// <param name="code">The stable capability code.</param>
    /// <param name="description">An optional human-readable purpose.</param>
    /// <returns>The created permission.</returns>
    public Permission AddPermission(string code, string? description = null)
    {
        Permission permission = Permission.Create(code, description);
        _permissions[permission.Id] = permission;
        return permission;
    }

    /// <summary>
    /// Creates and stores a role with the given name, granting it the given permissions.
    /// </summary>
    /// <param name="name">The role name.</param>
    /// <param name="permissions">The permissions to grant to the role.</param>
    /// <returns>The created role.</returns>
    public Role AddRole(string name, params Permission[] permissions)
    {
        Role role = Role.Create(name);
        foreach (Permission permission in permissions)
        {
            role.AssignPermission(permission.Id);
            _permissions[permission.Id] = permission;
        }

        Add(role);
        return role;
    }

    /// <inheritdoc/>
    public void Add(Role aggregateRoot)
    {
        _roles[aggregateRoot.Id] = aggregateRoot;
        _byNormalizedName[aggregateRoot.Name.NormalizedValue] = aggregateRoot;
    }

    /// <inheritdoc/>
    public Task<Role?> GetByIdAsync(RoleId id, CancellationToken cancellationToken = default)
        => Task.FromResult(_roles.GetValueOrDefault(id));

    /// <inheritdoc/>
    public Task<Role?> GetByNormalizedNameAsync(string normalizedName, CancellationToken cancellationToken = default)
        => Task.FromResult(_byNormalizedName.GetValueOrDefault(normalizedName));

    /// <inheritdoc/>
    public Task<bool> ExistsByIdAsync(RoleId id, CancellationToken cancellationToken = default)
        => Task.FromResult(_roles.ContainsKey(id));

    /// <inheritdoc/>
    public Task<bool> ExistsByNormalizedNameAsync(string normalizedName, CancellationToken cancellationToken = default)
        => Task.FromResult(_byNormalizedName.ContainsKey(normalizedName));

    /// <inheritdoc/>
    public Task<Role?> GetByIdTrackedAsync(RoleId id, CancellationToken cancellationToken = default)
        => GetByIdAsync(id, cancellationToken);

    /// <inheritdoc/>
    public Task ReloadTrackedAsync(Role role, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    /// <inheritdoc/>
    public Task<IReadOnlyList<Role>> GetRolesForUserAsync(UserId userId, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<Role>>(_roles.Values
            .Where(role => IsAssignedTo(role.Id, userId))
            .ToArray());

    /// <inheritdoc/>
    public Task<IReadOnlyList<Permission>> GetPermissionsForRoleAsync(RoleId roleId, CancellationToken cancellationToken = default)
    {
        if (!_roles.TryGetValue(roleId, out Role? role))
        {
            return Task.FromResult<IReadOnlyList<Permission>>(Array.Empty<Permission>());
        }

        return Task.FromResult<IReadOnlyList<Permission>>(role.RolePermissions
            .Select(grant => ResolvePermission(grant.PermissionId))
            .ToArray());
    }

    /// <inheritdoc/>
    public Task<IReadOnlyList<Permission>> GetPermissionsForUserAsync(UserId userId, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<Permission>>(_roles.Values
            .Where(role => IsAssignedTo(role.Id, userId))
            .SelectMany(role => role.RolePermissions.Select(grant => ResolvePermission(grant.PermissionId)))
            .Distinct()
            .ToArray());

    private bool IsAssignedTo(RoleId roleId, UserId userId)
    {
        if (_users is not null)
        {
            return _users.All.Any(user => user.Id == userId && user.UserRoles.Any(assignment => assignment.RoleId == roleId));
        }

        return _roles.TryGetValue(roleId, out Role? role)
            && role.UserRoles.Any(assignment => assignment.UserId == userId);
    }

    private Permission ResolvePermission(PermissionId permissionId)
    {
        if (_permissionStore is not null)
        {
            return _permissionStore.All.Single(permission => permission.Id == permissionId);
        }

        return _permissions[permissionId];
    }

    /// <inheritdoc/>
    public void Update(Role aggregateRoot) => Add(aggregateRoot);

    /// <inheritdoc/>
    public void Remove(Role aggregateRoot)
    {
        _roles.Remove(aggregateRoot.Id);
        _byNormalizedName.Remove(aggregateRoot.Name.NormalizedValue);
    }

    /// <inheritdoc/>
    public void RemoveRange(IEnumerable<Role> aggregateRoots)
    {
        foreach (Role role in aggregateRoots)
        {
            Remove(role);
        }
    }
}
