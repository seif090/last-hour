namespace LastHour.BuildingBlocks.Infrastructure.Persistence.Outbox.Queries;

/// <summary>
/// Payload-safe summary DTO representing an outbox message in administrative list views.
/// Excludes serialized message content to prevent sensitive data or PII exposure.
/// </summary>
public sealed record OutboxMessageSummaryDto
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OutboxMessageSummaryDto"/> class.
    /// </summary>
    /// <param name="id">The unique identifier of the outbox message.</param>
    /// <param name="eventType">The CLR type name of the serialized event.</param>
    /// <param name="status">The derived operational lifecycle status.</param>
    /// <param name="occurredOnUtc">The timestamp when the event occurred in UTC.</param>
    /// <param name="processedOnUtc">The timestamp when the message was processed, if any.</param>
    /// <param name="retryCount">The count of previous delivery attempts.</param>
    /// <param name="nextAttemptAtUtc">The scheduled next retry attempt timestamp, if any.</param>
    /// <param name="lockExpiresAtUtc">The lease expiration timestamp, if any.</param>
    public OutboxMessageSummaryDto(
        Guid id,
        string eventType,
        OutboxMessageStatus status,
        DateTime occurredOnUtc,
        DateTime? processedOnUtc,
        int retryCount,
        DateTime? nextAttemptAtUtc,
        DateTime? lockExpiresAtUtc)
    {
        Id = id;
        EventType = eventType;
        Status = status;
        OccurredOnUtc = occurredOnUtc;
        ProcessedOnUtc = processedOnUtc;
        RetryCount = retryCount;
        NextAttemptAtUtc = nextAttemptAtUtc;
        LockExpiresAtUtc = lockExpiresAtUtc;
    }

    /// <summary>
    /// Gets the unique identifier of the outbox message.
    /// </summary>
    public Guid Id { get; }

    /// <summary>
    /// Gets the CLR type name of the serialized event.
    /// </summary>
    public string EventType { get; }

    /// <summary>
    /// Gets the derived operational lifecycle status.
    /// </summary>
    public OutboxMessageStatus Status { get; }

    /// <summary>
    /// Gets the timestamp when the event occurred in UTC.
    /// </summary>
    public DateTime OccurredOnUtc { get; }

    /// <summary>
    /// Gets the timestamp when the message was processed, if any.
    /// </summary>
    public DateTime? ProcessedOnUtc { get; }

    /// <summary>
    /// Gets the count of previous delivery attempts.
    /// </summary>
    public int RetryCount { get; }

    /// <summary>
    /// Gets the scheduled next retry attempt timestamp, if any.
    /// </summary>
    public DateTime? NextAttemptAtUtc { get; }

    /// <summary>
    /// Gets the lease expiration timestamp, if any.
    /// </summary>
    public DateTime? LockExpiresAtUtc { get; }
}
