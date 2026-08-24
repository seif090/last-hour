namespace LastHour.BuildingBlocks.Infrastructure.Persistence.Outbox.Queries;

/// <summary>
/// Represents the derived operational lifecycle state of an outbox message.
/// </summary>
public enum OutboxMessageStatus
{
    /// <summary>
    /// The message is pending and ready for immediate claiming and dispatch on the next cycle.
    /// </summary>
    Pending,

    /// <summary>
    /// The message is currently claimed and held under an active processing lease.
    /// </summary>
    Processing,

    /// <summary>
    /// The message previously failed and is awaiting scheduled retry under exponential backoff.
    /// </summary>
    Scheduled,

    /// <summary>
    /// The message was successfully processed and dispatched without error.
    /// </summary>
    Processed,

    /// <summary>
    /// The message reached maximum retry attempts, was abandoned, and is eligible for administrative requeue.
    /// </summary>
    Abandoned,
}
