using System.Text.Json;
using LastHour.BuildingBlocks.SharedKernel.Domain.Events;

namespace LastHour.BuildingBlocks.Infrastructure.Persistence.Outbox;

/// <summary>
/// A durable record of a message published through the outbox. Messages are serialized to
/// JSON when written and rehydrated by the outbox processor, which dispatches them to
/// in-process handlers. Processing is at-least-once: a message is retried on subsequent
/// processor cycles until it either succeeds or exhausts the configured retry limit.
/// </summary>
public sealed class OutboxMessage
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OutboxMessage"/> class.
    /// </summary>
    /// <param name="id">The message identifier.</param>
    /// <param name="type">The assembly-qualified CLR type name of the serialized message.</param>
    /// <param name="content">The JSON-serialized message payload.</param>
    /// <param name="occurredOnUtc">The UTC timestamp at which the message was created.</param>
    private OutboxMessage(Guid id, string type, string content, DateTime occurredOnUtc)
    {
        Id = id;
        Type = type;
        Content = content;
        OccurredOnUtc = occurredOnUtc;
    }

    /// <summary>
    /// Gets the message identifier.
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// Gets the assembly-qualified CLR type name of the serialized message.
    /// </summary>
    public string Type { get; private set; }

    /// <summary>
    /// Gets the JSON-serialized message payload.
    /// </summary>
    public string Content { get; private set; }

    /// <summary>
    /// Gets the UTC timestamp at which the message was created.
    /// </summary>
    public DateTime OccurredOnUtc { get; private set; }

    /// <summary>
    /// Gets the UTC timestamp at which the message was processed, or <see langword="null"/>
    /// when it is still pending or has exhausted its retries.
    /// </summary>
    public DateTime? ProcessedOnUtc { get; private set; }

    /// <summary>
    /// Gets the UTC timestamp at which the next processing attempt is scheduled, or
    /// <see langword="null"/> when the message is immediately eligible or processed.
    /// </summary>
    public DateTime? NextAttemptAtUtc { get; private set; }

    /// <summary>
    /// Gets the token representing the active processor lease, or <see langword="null"/>
    /// when the message is not currently claimed.
    /// </summary>
    public Guid? ClaimToken { get; private set; }

    /// <summary>
    /// Gets the UTC timestamp at which the active processing lease expires, or <see langword="null"/>
    /// when the message is not currently claimed.
    /// </summary>
    public DateTime? LockExpiresAtUtc { get; private set; }

    /// <summary>
    /// Gets the last error captured while processing the message, or <see langword="null"/>
    /// when no failure has occurred.
    /// </summary>
    public string? Error { get; private set; }

    /// <summary>
    /// Gets the number of times the message was processed without success.
    /// </summary>
    public int RetryCount { get; private set; }

    /// <summary>
    /// Gets a value indicating whether the message is in an abandoned state and can be re-queued.
    /// </summary>
    public bool CanRequeue => ProcessedOnUtc != null && Error != null;

    /// <summary>
    /// Creates an outbox message from a domain event.
    /// </summary>
    /// <param name="domainEvent">The domain event to persist.</param>
    /// <returns>A new outbox message ready to be added to the data store.</returns>
    public static OutboxMessage FromDomainEvent(IDomainEvent domainEvent)
        => Create(domainEvent.GetType(), domainEvent);

    /// <summary>
    /// Creates an outbox message from an arbitrary message payload.
    /// </summary>
    /// <param name="message">The message to persist.</param>
    /// <returns>A new outbox message ready to be added to the data store.</returns>
    public static OutboxMessage Create(object message)
        => Create(message.GetType(), message);

    /// <summary>
    /// Creates an outbox message with an explicit creation timestamp.
    /// </summary>
    /// <param name="message">The message to persist.</param>
    /// <param name="occurredOnUtc">The UTC timestamp at which the message was created.</param>
    /// <returns>A new outbox message ready to be added to the data store.</returns>
    public static OutboxMessage Create(object message, DateTime occurredOnUtc)
        => Create(message.GetType(), message, occurredOnUtc);

    /// <summary>
    /// Claims the message for processing until the specified lease expiration instant.
    /// </summary>
    /// <param name="claimToken">The unique token identifying the claiming processor cycle.</param>
    /// <param name="leaseExpiresAtUtc">The UTC timestamp at which the lease expires.</param>
    public void Claim(Guid claimToken, DateTime leaseExpiresAtUtc)
    {
        ClaimToken = claimToken;
        LockExpiresAtUtc = leaseExpiresAtUtc;
    }

    /// <summary>
    /// Marks the message as successfully processed and clears the active lease and scheduled retry.
    /// </summary>
    /// <param name="processedOnUtc">The UTC timestamp at which processing completed.</param>
    public void MarkProcessed(DateTime processedOnUtc)
    {
        ProcessedOnUtc = processedOnUtc;
        ClaimToken = null;
        LockExpiresAtUtc = null;
        NextAttemptAtUtc = null;
        Error = null;
    }

    /// <summary>
    /// Records a failed processing attempt, schedules the next retry attempt according to
    /// <paramref name="retryDelay"/>, and clears the active lease. When <paramref name="maxRetryCount"/>
    /// is reached, the message is abandoned and flagged as processed with its last error preserved.
    /// </summary>
    /// <param name="error">A description of the failure.</param>
    /// <param name="failedOnUtc">The UTC timestamp at which the attempt failed.</param>
    /// <param name="maxRetryCount">The maximum number of retry attempts allowed.</param>
    /// <param name="retryDelay">The scheduled backoff delay before the next attempt.</param>
    public void RecordFailure(string error, DateTime failedOnUtc, int maxRetryCount, TimeSpan retryDelay)
    {
        Error = error;
        ClaimToken = null;
        LockExpiresAtUtc = null;
        RetryCount++;

        if (RetryCount >= maxRetryCount)
        {
            ProcessedOnUtc = failedOnUtc;
            NextAttemptAtUtc = null;
        }
        else
        {
            NextAttemptAtUtc = failedOnUtc.Add(retryDelay);
        }
    }

    /// <summary>
    /// Records a failed processing attempt without retry delay and clears the active lease.
    /// </summary>
    /// <param name="error">A description of the failure.</param>
    /// <param name="failedOnUtc">The UTC timestamp at which the attempt failed.</param>
    /// <param name="maxRetryCount">The maximum number of retry attempts allowed.</param>
    public void RecordFailure(string error, DateTime failedOnUtc, int maxRetryCount)
        => RecordFailure(error, failedOnUtc, maxRetryCount, TimeSpan.Zero);

    /// <summary>
    /// Resets an abandoned outbox message so that it is re-queued for processing immediately.
    /// </summary>
    /// <param name="currentUtc">The current UTC timestamp.</param>
    /// <exception cref="InvalidOperationException">The message is not in an abandoned state.</exception>
    public void Requeue(DateTime currentUtc)
    {
        if (!CanRequeue)
        {
            throw new InvalidOperationException("Only abandoned outbox messages can be requeued.");
        }

        ProcessedOnUtc = null;
        RetryCount = 0;
        Error = null;
        ClaimToken = null;
        LockExpiresAtUtc = null;
        NextAttemptAtUtc = currentUtc;
    }

    private static OutboxMessage Create(Type messageType, object message, DateTime occurredOnUtc)
    {
        string content = JsonSerializer.Serialize(message, messageType, OutboxJson.SerializerOptions);
        return new OutboxMessage(Guid.NewGuid(), messageType.AssemblyQualifiedName ?? messageType.FullName ?? messageType.Name, content, occurredOnUtc);
    }

    private static OutboxMessage Create(Type messageType, object message)
        => Create(messageType, message, DateTime.UtcNow);
}
