using LastHour.BuildingBlocks.Application.Contracts;
using LastHour.BuildingBlocks.SharedKernel.Domain;
using Microsoft.EntityFrameworkCore;

namespace LastHour.BuildingBlocks.Infrastructure.Persistence.Repositories;

/// <summary>
/// EF Core-backed implementation of <see cref="ITrackedRepository{TEntity, TId}"/>. Queries
/// run with change tracking enabled (the EF default), so loaded entities retain their original
/// values — including optimistic-concurrency tokens — and mutations are persisted with the
/// correct update predicate. <see cref="ReloadAsync"/> refreshes a tracked entity from the
/// store, discarding pending changes, which flows use to retry after a concurrency conflict.
/// </summary>
/// <typeparam name="TEntity">The type of the queried entity.</typeparam>
/// <typeparam name="TId">The type of the entity identifier.</typeparam>
public sealed class EfTrackedRepository<TEntity, TId> : ITrackedRepository<TEntity, TId>
    where TEntity : class, IEntity<TId>
    where TId : IEquatable<TId>
{
    private readonly DbSet<TEntity> _dbSet;

    /// <summary>
    /// Initializes a new instance of the <see cref="EfTrackedRepository{TEntity, TId}"/> class.
    /// </summary>
    /// <param name="dbContext">The database context that tracks the entities.</param>
    public EfTrackedRepository(LastHourDbContext dbContext)
    {
        _dbSet = dbContext.Set<TEntity>();
    }

    /// <inheritdoc/>
    public Task<TEntity?> GetByIdAsync(TId id, CancellationToken cancellationToken = default)
        => _dbSet.SingleOrDefaultAsync(entity => entity.Id.Equals(id), cancellationToken);

    /// <inheritdoc/>
    public Task<TEntity?> GetByIdIncludingAsync(
        TId id,
        IReadOnlyCollection<string> includePaths,
        CancellationToken cancellationToken = default)
    {
        IQueryable<TEntity> query = _dbSet.AsQueryable();
        foreach (string includePath in includePaths)
        {
            query = query.Include(includePath);
        }

        return query.SingleOrDefaultAsync(entity => entity.Id.Equals(id), cancellationToken);
    }

    /// <inheritdoc/>
    public Task ReloadAsync(TEntity entity, CancellationToken cancellationToken = default)
        => _dbSet.Entry(entity).ReloadAsync(cancellationToken);
}
