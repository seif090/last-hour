using LastHour.BuildingBlocks.SharedKernel.Domain.Events;

namespace LastHour.BuildingBlocks.Application.Contracts;

/// <summary>
/// Provides consumer-scoped idempotency controls to suppress duplicate event deliveries
/// under at-least-once delivery guarantees.
/// </summary>
public interface IConsumerIdempotencyService
{
    /// <summary>
    /// Executes the specified handler action idempotently for the given event identifier and consumer.
    /// If the event has already been completed by this consumer, execution is skipped and <see langword="false"/> is returned.
    /// If handler execution succeeds, the record is marked completed and <see langword="true"/> is returned.
    /// If handler execution fails or is cancelled, the in-progress record is released so the event remains retryable.
    /// </summary>
    /// <param name="eventId">The unique identifier of the event.</param>
    /// <param name="consumer">The deterministic identifier or name of the consumer.</param>
    /// <param name="handler">The asynchronous handler action to execute.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns><see langword="true"/> if the handler was executed; <see langword="false"/> if it was skipped as a duplicate.</returns>
    Task<bool> ExecuteAsync(
        Guid eventId,
        string consumer,
        Func<CancellationToken, Task> handler,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes the specified handler action idempotently for the given domain event and consumer.
    /// </summary>
    /// <param name="domainEvent">The domain event containing the canonical event identifier.</param>
    /// <param name="consumer">The deterministic identifier or name of the consumer.</param>
    /// <param name="handler">The asynchronous handler action to execute.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns><see langword="true"/> if the handler was executed; <see langword="false"/> if it was skipped as a duplicate.</returns>
    Task<bool> ExecuteAsync(
        IDomainEvent domainEvent,
        string consumer,
        Func<CancellationToken, Task> handler,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Attempts to acquire ownership to process an event for the specified consumer.
    /// </summary>
    /// <param name="eventId">The unique identifier of the event.</param>
    /// <param name="consumer">The deterministic identifier or name of the consumer.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns><see langword="true"/> if acquisition succeeded (first delivery or expired lease); otherwise <see langword="false"/>.</returns>
    Task<bool> TryAcquireAsync(
        Guid eventId,
        string consumer,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks the event processing as successfully completed for the specified consumer.
    /// </summary>
    /// <param name="eventId">The unique identifier of the event.</param>
    /// <param name="consumer">The deterministic identifier or name of the consumer.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task CompleteAsync(
        Guid eventId,
        string consumer,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Releases an in-progress idempotency record (e.g. after a handler failure or cancellation)
    /// so that subsequent retries can immediately re-acquire ownership.
    /// </summary>
    /// <param name="eventId">The unique identifier of the event.</param>
    /// <param name="consumer">The deterministic identifier or name of the consumer.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task ReleaseAsync(
        Guid eventId,
        string consumer,
        CancellationToken cancellationToken = default);
}
