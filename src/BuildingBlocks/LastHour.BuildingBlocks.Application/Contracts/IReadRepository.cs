using System.Linq.Expressions;
using LastHour.BuildingBlocks.SharedKernel.Domain;

namespace LastHour.BuildingBlocks.Application.Contracts;

/// <summary>
/// Defines read-only queries over an entity type. Implementations perform queries against
/// the underlying data store without tracking or modifying state.
/// </summary>
/// <typeparam name="TEntity">The type of the queried entity.</typeparam>
/// <typeparam name="TId">The type of the entity identifier.</typeparam>
public interface IReadRepository<TEntity, TId>
    where TEntity : IEntity<TId>
    where TId : IEquatable<TId>
{
    /// <summary>
    /// Retrieves the entity with the specified identifier, or <see langword="null"/> when none exists.
    /// </summary>
    /// <param name="id">The entity identifier to look up.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>The matching entity, or <see langword="null"/> when not found.</returns>
    Task<TEntity?> GetByIdAsync(TId id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all entities of the type.
    /// </summary>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>A read-only list of all entities.</returns>
    Task<IReadOnlyList<TEntity>> ListAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all entities matching <paramref name="predicate"/>.
    /// </summary>
    /// <param name="predicate">The condition an entity must satisfy.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>A read-only list of the matching entities.</returns>
    Task<IReadOnlyList<TEntity>> ListAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Determines whether any entity of the type exists.
    /// </summary>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns><see langword="true"/> when at least one entity exists; otherwise <see langword="false"/>.</returns>
    Task<bool> AnyAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Determines whether any entity matching <paramref name="predicate"/> exists.
    /// </summary>
    /// <param name="predicate">The condition an entity must satisfy.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns><see langword="true"/> when at least one matching entity exists; otherwise <see langword="false"/>.</returns>
    Task<bool> AnyAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves the first entity matching <paramref name="predicate"/>, or <see langword="null"/> when none exists.
    /// </summary>
    /// <param name="predicate">The condition an entity must satisfy.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>The first matching entity, or <see langword="null"/> when none exists.</returns>
    Task<TEntity?> FirstOrDefaultAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Determines whether an entity with the specified identifier exists.
    /// </summary>
    /// <param name="id">The entity identifier to look up.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns><see langword="true"/> when an entity with the identifier exists; otherwise <see langword="false"/>.</returns>
    Task<bool> ExistsAsync(TId id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Counts the number of entities of the type.
    /// </summary>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>The number of entities.</returns>
    Task<int> CountAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Counts the number of entities matching <paramref name="predicate"/>.
    /// </summary>
    /// <param name="predicate">The condition an entity must satisfy.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>The number of matching entities.</returns>
    async Task<int> CountAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default)
    {
        // In-memory fallback used by implementations that cannot apply predicates in the store.
        IReadOnlyList<TEntity> all = await ListAsync(predicate, cancellationToken).ConfigureAwait(false);
        return all.Count;
    }

    /// <summary>
    /// Retrieves an ordered page of the entities matching <paramref name="predicate"/>.
    /// Implementations translate the ordering and paging into the store where possible; the
    /// default applies them in memory over <see cref="ListAsync(System.Linq.Expressions.Expression{Func{TEntity, bool}}, System.Threading.CancellationToken)"/>.
    /// </summary>
    /// <typeparam name="TKey">The type of the sort key.</typeparam>
    /// <param name="predicate">The condition an entity must satisfy.</param>
    /// <param name="sortSelector">The sort key of an entity.</param>
    /// <param name="sortDescending">When <see langword="true"/>, order descending; otherwise ascending.</param>
    /// <param name="skip">The number of leading matches to skip.</param>
    /// <param name="take">The maximum number of matches to return.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>A read-only list of the matching, ordered page.</returns>
    async Task<IReadOnlyList<TEntity>> ListAsync<TKey>(
        Expression<Func<TEntity, bool>> predicate,
        Expression<Func<TEntity, TKey>> sortSelector,
        bool sortDescending,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        // In-memory fallback used by implementations that cannot apply ordering in the store.
        IReadOnlyList<TEntity> all = await ListAsync(predicate, cancellationToken).ConfigureAwait(false);
        IOrderedEnumerable<TEntity> ordered = sortDescending
            ? all.OrderByDescending(sortSelector.Compile())
            : all.OrderBy(sortSelector.Compile());
        return ordered.Skip(skip).Take(take).ToArray();
    }
}
