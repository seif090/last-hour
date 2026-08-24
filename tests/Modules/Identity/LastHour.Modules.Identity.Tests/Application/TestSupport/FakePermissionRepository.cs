using System.Linq.Expressions;
using LastHour.BuildingBlocks.Application.Contracts;
using LastHour.Modules.Identity.Domain.Roles;

namespace LastHour.Modules.Identity.Tests.Application.TestSupport;

/// <summary>
/// Deterministic in-memory stand-in for the permission write and read repositories, keyed by
/// code exactly like the production lookup. Used by the authorization bootstrap tests.
/// </summary>
public sealed class FakePermissionRepository : IRepository<Permission>, IReadRepository<Permission, PermissionId>
{
    private readonly Dictionary<PermissionId, Permission> _permissions = new();
    private readonly Dictionary<string, Permission> _byCode = new(StringComparer.Ordinal);

    /// <summary>
    /// Gets all stored permissions, for assertions.
    /// </summary>
    public IReadOnlyCollection<Permission> All => _permissions.Values;

    /// <summary>
    /// Creates and stores a permission with the given code.
    /// </summary>
    /// <param name="code">The stable capability code.</param>
    /// <param name="description">An optional human-readable purpose.</param>
    /// <returns>The created permission.</returns>
    public Permission AddPermission(string code, string? description = null)
    {
        Permission permission = Permission.Create(code, description);
        Add(permission);
        return permission;
    }

    /// <inheritdoc/>
    public void Add(Permission aggregateRoot)
    {
        _permissions[aggregateRoot.Id] = aggregateRoot;
        _byCode[aggregateRoot.Code] = aggregateRoot;
    }

    /// <inheritdoc/>
    public void Update(Permission aggregateRoot) => Add(aggregateRoot);

    /// <inheritdoc/>
    public void Remove(Permission aggregateRoot)
    {
        _permissions.Remove(aggregateRoot.Id);
        _byCode.Remove(aggregateRoot.Code);
    }

    /// <inheritdoc/>
    public void RemoveRange(IEnumerable<Permission> aggregateRoots)
    {
        foreach (Permission permission in aggregateRoots)
        {
            Remove(permission);
        }
    }

    /// <inheritdoc/>
    public Task<Permission?> GetByIdAsync(PermissionId id, CancellationToken cancellationToken = default)
        => Task.FromResult(_permissions.GetValueOrDefault(id));

    /// <inheritdoc/>
    public Task<IReadOnlyList<Permission>> ListAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<Permission>>(_permissions.Values.ToArray());

    /// <inheritdoc/>
    public Task<IReadOnlyList<Permission>> ListAsync(Expression<Func<Permission, bool>> predicate, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<Permission>>(_permissions.Values.Where(predicate.Compile()).ToArray());

    /// <inheritdoc/>
    public Task<bool> AnyAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(_permissions.Count > 0);

    /// <inheritdoc/>
    public Task<bool> AnyAsync(Expression<Func<Permission, bool>> predicate, CancellationToken cancellationToken = default)
        => Task.FromResult(_permissions.Values.Any(predicate.Compile()));

    /// <inheritdoc/>
    public Task<Permission?> FirstOrDefaultAsync(Expression<Func<Permission, bool>> predicate, CancellationToken cancellationToken = default)
        => Task.FromResult(_permissions.Values.FirstOrDefault(predicate.Compile()));

    /// <inheritdoc/>
    public Task<bool> ExistsAsync(PermissionId id, CancellationToken cancellationToken = default)
        => Task.FromResult(_permissions.ContainsKey(id));

    /// <inheritdoc/>
    public Task<int> CountAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(_permissions.Count);
}
