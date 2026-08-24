using LastHour.Modules.Identity.Domain.Roles;
using LastHour.Modules.Identity.Domain.Users;

namespace LastHour.Api.Tests.Security.Authorization;

/// <summary>
/// Minimal in-memory stand-in for the role repository used by the authorization-handler unit
/// tests. Only the permission-resolution query is implemented; the write surface throws
/// because the handler never touches it. The permission set is keyed by user, exactly like the
/// single-projection resolution the production repository performs.
/// </summary>
public sealed class StubRoleRepository : IRoleRepository
{
    private readonly Dictionary<UserId, IReadOnlyList<Permission>> _permissionsByUser = new();

    /// <summary>
    /// Configures the effective permission set of a user.
    /// </summary>
    /// <param name="userId">The user whose permissions are configured.</param>
    /// <param name="codes">The permission codes the user holds. Duplicates are preserved to
    /// exercise the handler's handling of overlapping grants.</param>
    public void SetPermissions(UserId userId, params string[] codes)
    {
        _permissionsByUser[userId] = codes
            .Select(code => Permission.Create(code, description: null))
            .ToArray();
    }

    /// <inheritdoc/>
    public Task<IReadOnlyList<Permission>> GetPermissionsForUserAsync(
        UserId userId,
        CancellationToken cancellationToken = default)
        => Task.FromResult(
            _permissionsByUser.TryGetValue(userId, out IReadOnlyList<Permission>? permissions)
                ? permissions
                : Array.Empty<Permission>());

    /// <inheritdoc/>
    public Task<Role?> GetByIdAsync(RoleId id, CancellationToken cancellationToken = default)
        => throw new NotSupportedException("Not used by the authorization handler.");

    /// <inheritdoc/>
    public Task<Role?> GetByNormalizedNameAsync(string normalizedName, CancellationToken cancellationToken = default)
        => throw new NotSupportedException("Not used by the authorization handler.");

    /// <inheritdoc/>
    public Task<bool> ExistsByIdAsync(RoleId id, CancellationToken cancellationToken = default)
        => throw new NotSupportedException("Not used by the authorization handler.");

    /// <inheritdoc/>
    public Task<bool> ExistsByNormalizedNameAsync(string normalizedName, CancellationToken cancellationToken = default)
        => throw new NotSupportedException("Not used by the authorization handler.");

    /// <inheritdoc/>
    public Task<Role?> GetByIdTrackedAsync(RoleId id, CancellationToken cancellationToken = default)
        => throw new NotSupportedException("Not used by the authorization handler.");

    /// <inheritdoc/>
    public Task ReloadTrackedAsync(Role role, CancellationToken cancellationToken = default)
        => throw new NotSupportedException("Not used by the authorization handler.");

    /// <inheritdoc/>
    public Task<IReadOnlyList<Role>> GetRolesForUserAsync(UserId userId, CancellationToken cancellationToken = default)
        => throw new NotSupportedException("Not used by the authorization handler.");

    /// <inheritdoc/>
    public Task<IReadOnlyList<Permission>> GetPermissionsForRoleAsync(RoleId roleId, CancellationToken cancellationToken = default)
        => throw new NotSupportedException("Not used by the authorization handler.");

    /// <inheritdoc/>
    public void Add(Role aggregateRoot) => throw new NotSupportedException("Not used by the authorization handler.");

    /// <inheritdoc/>
    public void Update(Role aggregateRoot) => throw new NotSupportedException("Not used by the authorization handler.");

    /// <inheritdoc/>
    public void Remove(Role aggregateRoot) => throw new NotSupportedException("Not used by the authorization handler.");

    /// <inheritdoc/>
    public void RemoveRange(IEnumerable<Role> aggregateRoots) => throw new NotSupportedException("Not used by the authorization handler.");
}
