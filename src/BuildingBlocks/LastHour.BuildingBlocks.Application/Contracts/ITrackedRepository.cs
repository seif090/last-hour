using LastHour.BuildingBlocks.SharedKernel.Domain;

namespace LastHour.BuildingBlocks.Application.Contracts;

/// <summary>
/// Defines reads that place an entity into the change tracker so mutations are persisted with
/// optimistic-concurrency protection. The read-only <see cref="IReadRepository{TEntity, TId}"/>
/// deliberately detaches entities (no-tracking), which makes it unsuitable for flows that must
/// mutate an aggregate and rely on the concurrency token: attaching a mutated detached entity
/// loses the original token value. Repositories of this shape retain the original values, so
/// the update predicate matches the stored row and lost updates surface as
/// <c>DbUpdateConcurrencyException</c>. <see cref="ReloadAsync"/> refreshes a tracked entity
/// from the store, which is how a flow retries after a concurrency conflict.
/// </summary>
/// <typeparam name="TEntity">The type of the queried entity.</typeparam>
/// <typeparam name="TId">The type of the entity identifier.</typeparam>
public interface ITrackedRepository<TEntity, TId>
    where TEntity : class, IEntity<TId>
    where TId : IEquatable<TId>
{
    /// <summary>
    /// Retrieves the entity with the specified identifier and places it in the change tracker,
    /// or <see langword="null"/> when none exists.
    /// </summary>
    /// <param name="id">The entity identifier to look up.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>The matching, tracked entity, or <see langword="null"/> when not found.</returns>
    Task<TEntity?> GetByIdAsync(TId id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves the entity with the specified identifier, eagerly loading the given owned
    /// navigations, and places it in the change tracker — or <see langword="null"/> when none
    /// exists. Aggregates that mutate owned collections (for example a user's role
    /// assignments) must load those collections before mutating: the persistence context does
    /// not lazy-load, so a plain tracked load would see an empty collection and the aggregate
    /// invariant would reject the mutation. Implementations that cannot express includes fall
    /// back to a plain tracked load, which is safe when the store always hydrates the graph.
    /// </summary>
    /// <param name="id">The entity identifier to look up.</param>
    /// <param name="includePaths">The navigation property paths to eagerly load.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>The matching, tracked entity, or <see langword="null"/> when not found.</returns>
    Task<TEntity?> GetByIdIncludingAsync(
        TId id,
        IReadOnlyCollection<string> includePaths,
        CancellationToken cancellationToken = default)
        => GetByIdAsync(id, cancellationToken);

    /// <summary>
    /// Refreshes the tracked state of an entity from the store, discarding any pending changes.
    /// After a concurrency conflict, this re-establishes the original values so the entity can
    /// be mutated again and retried against the current stored state.
    /// </summary>
    /// <param name="entity">The tracked entity to refresh.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task ReloadAsync(TEntity entity, CancellationToken cancellationToken = default);
}
