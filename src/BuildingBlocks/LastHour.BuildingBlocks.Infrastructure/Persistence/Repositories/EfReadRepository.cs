using System.Linq.Expressions;
using LastHour.BuildingBlocks.Application.Contracts;
using LastHour.BuildingBlocks.SharedKernel.Domain;
using Microsoft.EntityFrameworkCore;

namespace LastHour.BuildingBlocks.Infrastructure.Persistence.Repositories;

/// <summary>
/// EF Core-backed implementation of <see cref="IReadRepository{TEntity, TId}"/>. All queries
/// are executed as no-tracking reads that never mutate the change tracker, and they respect
/// the global query filters (for example soft-delete) configured on the model.
/// </summary>
/// <typeparam name="TEntity">The type of the queried entity.</typeparam>
/// <typeparam name="TId">The type of the entity identifier.</typeparam>
public sealed class EfReadRepository<TEntity, TId> : IReadRepository<TEntity, TId>
    where TEntity : class, IEntity<TId>
    where TId : IEquatable<TId>
{
    private readonly DbSet<TEntity> _dbSet;

    /// <summary>
    /// Initializes a new instance of the <see cref="EfReadRepository{TEntity, TId}"/> class.
    /// </summary>
    /// <param name="dbContext">The database context used to execute queries.</param>
    public EfReadRepository(LastHourDbContext dbContext)
    {
        _dbSet = dbContext.Set<TEntity>();
    }

    /// <inheritdoc/>
    public Task<TEntity?> GetByIdAsync(TId id, CancellationToken cancellationToken = default)
        => _dbSet.AsNoTracking().SingleOrDefaultAsync(entity => entity.Id.Equals(id), cancellationToken);

    /// <inheritdoc/>
    public async Task<IReadOnlyList<TEntity>> ListAsync(CancellationToken cancellationToken = default)
        => await _dbSet.AsNoTracking().ToListAsync(cancellationToken).ConfigureAwait(false);

    /// <inheritdoc/>
    public async Task<IReadOnlyList<TEntity>> ListAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default)
        => await _dbSet.AsNoTracking().Where(predicate).ToListAsync(cancellationToken).ConfigureAwait(false);

    /// <inheritdoc/>
    public Task<bool> AnyAsync(CancellationToken cancellationToken = default)
        => _dbSet.AsNoTracking().AnyAsync(cancellationToken);

    /// <inheritdoc/>
    public Task<bool> AnyAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default)
        => _dbSet.AsNoTracking().AnyAsync(predicate, cancellationToken);

    /// <inheritdoc/>
    public Task<TEntity?> FirstOrDefaultAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default)
        => _dbSet.AsNoTracking().FirstOrDefaultAsync(predicate, cancellationToken);

    /// <inheritdoc/>
    public Task<bool> ExistsAsync(TId id, CancellationToken cancellationToken = default)
        => _dbSet.AsNoTracking().AnyAsync(entity => entity.Id.Equals(id), cancellationToken);

    /// <inheritdoc/>
    public Task<int> CountAsync(CancellationToken cancellationToken = default)
        => _dbSet.AsNoTracking().CountAsync(cancellationToken);

    /// <inheritdoc/>
    public Task<int> CountAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default)
        => _dbSet.AsNoTracking().CountAsync(predicate, cancellationToken);

    /// <inheritdoc/>
    public async Task<IReadOnlyList<TEntity>> ListAsync<TKey>(
        Expression<Func<TEntity, bool>> predicate,
        Expression<Func<TEntity, TKey>> sortSelector,
        bool sortDescending,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        IQueryable<TEntity> query = _dbSet.AsNoTracking().Where(predicate);
        query = sortDescending ? query.OrderByDescending(sortSelector) : query.OrderBy(sortSelector);
        return await query.Skip(skip).Take(take).ToListAsync(cancellationToken).ConfigureAwait(false);
    }
}
