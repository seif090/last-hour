namespace LastHour.BuildingBlocks.Infrastructure.Persistence.Idempotency;

/// <summary>
/// A persistent record tracking the consumer-scoped processing lifecycle of an event to suppress
/// duplicate deliveries under at-least-once delivery semantics.
/// </summary>
public sealed class ConsumerInboxMessage
{
    /// <summary>
    /// The maximum allowed length for the consumer identifier.
    /// </summary>
    public const int MaxConsumerLength = 256;

    private ConsumerInboxMessage()
    {
        // EF Core materialization only.
    }

    private ConsumerInboxMessage(
        Guid id,
        Guid eventId,
        string consumer,
        DateTime startedOnUtc,
        DateTime? lockExpiresAtUtc)
    {
        Id = id;
        EventId = eventId;
        Consumer = consumer;
        StartedOnUtc = startedOnUtc;
        LockExpiresAtUtc = lockExpiresAtUtc;
    }

    /// <summary>
    /// Gets the unique surrogate identifier of the inbox record.
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// Gets the unique identifier of the event being processed.
    /// </summary>
    public Guid EventId { get; private set; }

    /// <summary>
    /// Gets the unique name or identifier of the consumer processing this event.
    /// </summary>
    public string Consumer { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the UTC timestamp at which processing of this attempt was started.
    /// </summary>
    public DateTime StartedOnUtc { get; private set; }

    /// <summary>
    /// Gets the UTC timestamp at which processing completed successfully, or <see langword="null"/>
    /// while processing is still in-flight or was uncompleted.
    /// </summary>
    public DateTime? CompletedOnUtc { get; private set; }

    /// <summary>
    /// Gets the UTC timestamp at which the active processing lease expires, or <see langword="null"/>
    /// when the message has completed.
    /// </summary>
    public DateTime? LockExpiresAtUtc { get; private set; }

    /// <summary>
    /// Gets a value indicating whether the event has been successfully processed by this consumer.
    /// </summary>
    public bool IsCompleted => CompletedOnUtc.HasValue;

    /// <summary>
    /// Creates a new in-progress consumer inbox record.
    /// </summary>
    /// <param name="eventId">The event identifier.</param>
    /// <param name="consumer">The consumer identifier.</param>
    /// <param name="startedOnUtc">The UTC timestamp at which processing starts.</param>
    /// <param name="leaseDuration">The processing lease duration.</param>
    /// <returns>A new <see cref="ConsumerInboxMessage"/> instance.</returns>
    /// <exception cref="ArgumentException"><paramref name="eventId"/> is empty or <paramref name="consumer"/> is invalid.</exception>
    public static ConsumerInboxMessage Create(
        Guid eventId,
        string consumer,
        DateTime startedOnUtc,
        TimeSpan? leaseDuration = null)
    {
        if (eventId == Guid.Empty)
        {
            throw new ArgumentException("The event identifier cannot be empty.", nameof(eventId));
        }

        if (string.IsNullOrWhiteSpace(consumer))
        {
            throw new ArgumentException("The consumer identifier cannot be null or whitespace.", nameof(consumer));
        }

        if (consumer.Length > MaxConsumerLength)
        {
            throw new ArgumentException($"The consumer identifier cannot exceed {MaxConsumerLength} characters.", nameof(consumer));
        }

        DateTime? lockExpiresAtUtc = leaseDuration.HasValue ? startedOnUtc.Add(leaseDuration.Value) : null;
        return new ConsumerInboxMessage(Guid.NewGuid(), eventId, consumer.Trim(), startedOnUtc, lockExpiresAtUtc);
    }

    /// <summary>
    /// Marks the event processing as successfully completed.
    /// </summary>
    /// <param name="completedOnUtc">The UTC timestamp at which processing completed.</param>
    public void Complete(DateTime completedOnUtc)
    {
        CompletedOnUtc = completedOnUtc;
        LockExpiresAtUtc = null;
    }

    /// <summary>
    /// Renews the processing lease after a previous crashed or timed-out worker attempt.
    /// </summary>
    /// <param name="startedOnUtc">The new start timestamp in UTC.</param>
    /// <param name="leaseDuration">The new lease duration.</param>
    public void RenewLease(DateTime startedOnUtc, TimeSpan leaseDuration)
    {
        StartedOnUtc = startedOnUtc;
        LockExpiresAtUtc = startedOnUtc.Add(leaseDuration);
    }
}
