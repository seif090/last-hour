using LastHour.BuildingBlocks.Application.Contracts;
using LastHour.Modules.Identity.Domain.Roles;
using LastHour.Modules.Identity.Domain.Users;

namespace LastHour.Modules.Identity.Application.Persistence;

/// <summary>
/// The <see cref="IRoleRepository"/> implementation, composed purely from the shared repository
/// contracts. Writes delegate to the tracking <see cref="IRepository{TAggregateRoot}"/> and
/// queries to the no-tracking <see cref="IReadRepository{TEntity, TId}"/>, so the module needs
/// no infrastructure reference. Role lookups always run against the stored normalized name,
/// computed by the domain's single normalization rule, and the authorization resolution
/// queries (roles of a user, effective permissions of a user) are expressed as single
/// projection predicates so they translate into one query each — never N+1.
/// </summary>
public sealed class RoleRepository : IRoleRepository
{
    private readonly IRepository<Role> _repository;
    private readonly IReadRepository<Role, RoleId> _readRoles;
    private readonly IReadRepository<Permission, PermissionId> _readPermissions;
    private readonly ITrackedRepository<Role, RoleId> _trackedRepository;

    /// <summary>
    /// Initializes a new instance of the <see cref="RoleRepository"/> class.
    /// </summary>
    /// <param name="repository">The shared write repository for the <see cref="Role"/> aggregate.</param>
    /// <param name="readRoles">The shared read repository for <see cref="Role"/> entities.</param>
    /// <param name="readPermissions">The shared read repository for <see cref="Permission"/> entities.</param>
    /// <param name="trackedRepository">The shared tracked repository used to load the role for
    /// mutation with optimistic-concurrency protection.</param>
    /// <exception cref="ArgumentNullException">Any argument is <see langword="null"/>.</exception>
    public RoleRepository(
        IRepository<Role> repository,
        IReadRepository<Role, RoleId> readRoles,
        IReadRepository<Permission, PermissionId> readPermissions,
        ITrackedRepository<Role, RoleId> trackedRepository)
    {
        ArgumentNullException.ThrowIfNull(repository);
        ArgumentNullException.ThrowIfNull(readRoles);
        ArgumentNullException.ThrowIfNull(readPermissions);
        ArgumentNullException.ThrowIfNull(trackedRepository);
        _repository = repository;
        _readRoles = readRoles;
        _readPermissions = readPermissions;
        _trackedRepository = trackedRepository;
    }

    /// <inheritdoc/>
    public void Add(Role aggregateRoot) => _repository.Add(aggregateRoot);

    /// <inheritdoc/>
    public void Update(Role aggregateRoot) => _repository.Update(aggregateRoot);

    /// <inheritdoc/>
    public void Remove(Role aggregateRoot) => _repository.Remove(aggregateRoot);

    /// <inheritdoc/>
    public void RemoveRange(IEnumerable<Role> aggregateRoots) => _repository.RemoveRange(aggregateRoots);

    /// <inheritdoc/>
    public Task<Role?> GetByIdAsync(RoleId id, CancellationToken cancellationToken = default)
        => _readRoles.GetByIdAsync(id, cancellationToken);

    /// <inheritdoc/>
    public Task<Role?> GetByNormalizedNameAsync(string normalizedName, CancellationToken cancellationToken = default)
        => _readRoles.FirstOrDefaultAsync(
            role => role.Name.NormalizedValue == normalizedName,
            cancellationToken);

    /// <inheritdoc/>
    public Task<bool> ExistsByIdAsync(RoleId id, CancellationToken cancellationToken = default)
        => _readRoles.ExistsAsync(id, cancellationToken);

    /// <inheritdoc/>
    public Task<bool> ExistsByNormalizedNameAsync(string normalizedName, CancellationToken cancellationToken = default)
        => _readRoles.AnyAsync(
            role => role.Name.NormalizedValue == normalizedName,
            cancellationToken);

    /// <inheritdoc/>
    public Task<Role?> GetByIdTrackedAsync(RoleId id, CancellationToken cancellationToken = default)
    {
        // Eagerly load the permission grants: flows that revoke a grant must see the owned
        // collection, because the persistence context never lazy-loads it.
        return _trackedRepository.GetByIdIncludingAsync(
            id,
            new[] { nameof(Role.RolePermissions) },
            cancellationToken);
    }

    /// <inheritdoc/>
    public Task ReloadTrackedAsync(Role role, CancellationToken cancellationToken = default)
        => _trackedRepository.ReloadAsync(role, cancellationToken);

    /// <inheritdoc/>
    public Task<IReadOnlyList<Role>> GetRolesForUserAsync(UserId userId, CancellationToken cancellationToken = default)
        => _readRoles.ListAsync(
            role => role.UserRoles.Any(assignment => assignment.UserId == userId),
            cancellationToken);

    /// <inheritdoc/>
    public Task<IReadOnlyList<Permission>> GetPermissionsForRoleAsync(RoleId roleId, CancellationToken cancellationToken = default)
        => _readPermissions.ListAsync(
            permission => permission.RolePermissions.Any(grant => grant.RoleId == roleId),
            cancellationToken);

    /// <inheritdoc/>
    public Task<IReadOnlyList<Permission>> GetPermissionsForUserAsync(UserId userId, CancellationToken cancellationToken = default)
        => _readPermissions.ListAsync(
            permission => permission.RolePermissions.Any(
                grant => grant.Role.UserRoles.Any(assignment => assignment.UserId == userId)),
            cancellationToken);
}
