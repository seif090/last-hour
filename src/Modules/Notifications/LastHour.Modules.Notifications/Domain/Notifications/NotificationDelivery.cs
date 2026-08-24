using LastHour.BuildingBlocks.SharedKernel.Domain;

namespace LastHour.Modules.Notifications.Domain.Notifications;

/// <summary>
/// Represents a channel-specific delivery attempt and lifecycle state machine for a <see cref="Notification"/>.
/// </summary>
public sealed class NotificationDelivery : AggregateRoot<Guid>
{
    private NotificationDelivery()
    {
    }

    private NotificationDelivery(
        Guid id,
        Guid notificationId,
        DeliveryChannel channel,
        DateTime createdAtUtc,
        int maximumRetryCount)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Identifier cannot be empty.", nameof(id));
        }

        Id = id;
        if (notificationId == Guid.Empty)
        {
            throw new ArgumentException("Notification identifier cannot be empty.", nameof(notificationId));
        }

        if (maximumRetryCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maximumRetryCount), "Maximum retry count cannot be negative.");
        }

        NotificationId = notificationId;
        Channel = channel;
        Status = DeliveryStatus.Pending;
        RetryCount = 0;
        MaximumRetryCount = maximumRetryCount;
        NextAttemptAtUtc = createdAtUtc;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = createdAtUtc;
    }

    /// <summary>
    /// Gets the parent notification identifier.
    /// </summary>
    public Guid NotificationId { get; private set; }

    /// <summary>
    /// Gets the delivery channel (InApp, Email, Sms, Push).
    /// </summary>
    public DeliveryChannel Channel { get; private set; }

    /// <summary>
    /// Gets the current delivery status.
    /// </summary>
    public DeliveryStatus Status { get; private set; }

    /// <summary>
    /// Gets the number of previous failure attempts.
    /// </summary>
    public int RetryCount { get; private set; }

    /// <summary>
    /// Gets the maximum allowed retry attempts before permanent failure.
    /// </summary>
    public int MaximumRetryCount { get; private set; }

    /// <summary>
    /// Gets the timestamp when the next delivery attempt is scheduled.
    /// </summary>
    public DateTime? NextAttemptAtUtc { get; private set; }

    /// <summary>
    /// Gets the unique lease token claiming this delivery.
    /// </summary>
    public Guid? ClaimToken { get; private set; }

    /// <summary>
    /// Gets the timestamp when the active processing lease expires.
    /// </summary>
    public DateTime? LockExpiresAtUtc { get; private set; }

    /// <summary>
    /// Gets the timestamp when delivery completed successfully.
    /// </summary>
    public DateTime? DeliveredAtUtc { get; private set; }

    /// <summary>
    /// Gets the timestamp of the last recorded failure.
    /// </summary>
    public DateTime? FailedAtUtc { get; private set; }

    /// <summary>
    /// Gets the sanitized error description from the last failure attempt.
    /// </summary>
    public string? LastError { get; private set; }

    /// <summary>
    /// Gets the UTC creation timestamp.
    /// </summary>
    public DateTime CreatedAtUtc { get; private set; }

    /// <summary>
    /// Gets the UTC timestamp of the last state modification.
    /// </summary>
    public DateTime UpdatedAtUtc { get; private set; }

    /// <summary>
    /// Factory method to create a new pending notification delivery.
    /// </summary>
    public static NotificationDelivery Create(
        Guid notificationId,
        DeliveryChannel channel,
        DateTime createdAtUtc,
        int maximumRetryCount = 3)
    {
        return new NotificationDelivery(Guid.NewGuid(), notificationId, channel, createdAtUtc, maximumRetryCount);
    }

    /// <summary>
    /// Claims the delivery for processing by a worker node.
    /// </summary>
    public void Claim(Guid claimToken, DateTime leaseExpiresAtUtc, DateTime utcNow)
    {
        if (Status == DeliveryStatus.Delivered)
        {
            throw new InvalidOperationException("Cannot claim an already delivered notification delivery.");
        }

        Status = DeliveryStatus.Processing;
        ClaimToken = claimToken;
        LockExpiresAtUtc = leaseExpiresAtUtc;
        UpdatedAtUtc = utcNow;
    }

    /// <summary>
    /// Marks the delivery as successfully completed (terminal state).
    /// </summary>
    public void MarkDelivered(DateTime utcNow)
    {
        Status = DeliveryStatus.Delivered;
        DeliveredAtUtc = utcNow;
        LockExpiresAtUtc = null;
        ClaimToken = null;
        NextAttemptAtUtc = null;
        UpdatedAtUtc = utcNow;
    }

    /// <summary>
    /// Records a delivery failure, scheduling a retry or transitioning to permanent failure.
    /// </summary>
    public void RecordFailure(string sanitizedError, DateTime utcNow, int maxRetryCount, TimeSpan retryDelay)
    {
        ArgumentNullException.ThrowIfNull(sanitizedError);

        if (Status == DeliveryStatus.Delivered || Status == DeliveryStatus.FailedPermanent)
        {
            throw new InvalidOperationException($"Cannot record failure for delivery in terminal status '{Status}'.");
        }

        RetryCount++;
        LastError = sanitizedError.Length > 1000 ? sanitizedError[..1000] : sanitizedError;
        FailedAtUtc = utcNow;
        LockExpiresAtUtc = null;
        ClaimToken = null;
        UpdatedAtUtc = utcNow;

        if (RetryCount >= maxRetryCount)
        {
            Status = DeliveryStatus.FailedPermanent;
            NextAttemptAtUtc = null;
        }
        else
        {
            Status = DeliveryStatus.RetryScheduled;
            NextAttemptAtUtc = utcNow.Add(retryDelay < TimeSpan.Zero ? TimeSpan.Zero : retryDelay);
        }
    }

    /// <summary>
    /// Requeues a failed delivery for recovery.
    /// </summary>
    public void Requeue(DateTime utcNow)
    {
        RequeueByAdmin(Guid.NewGuid(), "System recovery requeue", utcNow);
    }

    /// <summary>
    /// Safely requeues a delivery for administrative recovery, validating status and lease ownership.
    /// </summary>
    /// <param name="actorId">The unique identifier of the authenticated administrative actor.</param>
    /// <param name="reason">The administrative justification for requeuing.</param>
    /// <param name="utcNow">The current UTC timestamp.</param>
    public void RequeueByAdmin(Guid actorId, string reason, DateTime utcNow)
    {
        if (actorId == Guid.Empty)
        {
            throw new ArgumentException("Actor identifier cannot be empty.", nameof(actorId));
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new ArgumentException("Reason for requeuing cannot be empty.", nameof(reason));
        }

        if (Status == DeliveryStatus.Delivered)
        {
            throw new InvalidOperationException("Delivered notifications cannot be requeued.");
        }

        if (Status == DeliveryStatus.Processing && LockExpiresAtUtc.HasValue && LockExpiresAtUtc.Value > utcNow)
        {
            throw new InvalidOperationException("Cannot requeue a delivery with an active worker lease.");
        }

        Status = DeliveryStatus.Pending;
        NextAttemptAtUtc = utcNow;
        LockExpiresAtUtc = null;
        ClaimToken = null;
        RetryCount = 0;
        LastError = null;
        UpdatedAtUtc = utcNow;
    }
}
