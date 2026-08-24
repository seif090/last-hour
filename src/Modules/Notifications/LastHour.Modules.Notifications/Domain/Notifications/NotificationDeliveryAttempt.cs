using LastHour.BuildingBlocks.SharedKernel.Domain;

namespace LastHour.Modules.Notifications.Domain.Notifications;

/// <summary>
/// Entity representing one immutable execution attempt for a <see cref="NotificationDelivery"/>.
/// </summary>
public sealed class NotificationDeliveryAttempt : AggregateRoot<Guid>
{
    private NotificationDeliveryAttempt()
    {
    }

    private NotificationDeliveryAttempt(
        Guid id,
        Guid deliveryId,
        int attemptNumber,
        DeliveryChannel channel,
        DateTime startedAtUtc)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Identifier cannot be empty.", nameof(id));
        }

        if (deliveryId == Guid.Empty)
        {
            throw new ArgumentException("Delivery identifier cannot be empty.", nameof(deliveryId));
        }

        if (attemptNumber < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(attemptNumber), "Attempt number must be greater than or equal to 1.");
        }

        if (!Enum.IsDefined(channel))
        {
            throw new ArgumentException($"Invalid delivery channel '{channel}'.", nameof(channel));
        }

        Id = id;
        DeliveryId = deliveryId;
        AttemptNumber = attemptNumber;
        Channel = channel;
        StartedAtUtc = startedAtUtc;
        Status = DeliveryStatus.Processing;
        CreatedAtUtc = startedAtUtc;
    }

    /// <summary>
    /// Gets the parent delivery identifier.
    /// </summary>
    public Guid DeliveryId { get; private set; }

    /// <summary>
    /// Gets the monotonic 1-based attempt sequence number.
    /// </summary>
    public int AttemptNumber { get; private set; }

    /// <summary>
    /// Gets the delivery channel (InApp, Email, Sms, Push).
    /// </summary>
    public DeliveryChannel Channel { get; private set; }

    /// <summary>
    /// Gets the UTC timestamp when the attempt started.
    /// </summary>
    public DateTime StartedAtUtc { get; private set; }

    /// <summary>
    /// Gets the UTC timestamp when the attempt completed, or <see langword="null"/> if in progress.
    /// </summary>
    public DateTime? CompletedAtUtc { get; private set; }

    /// <summary>
    /// Gets the total attempt execution duration in milliseconds, or <see langword="null"/> if in progress.
    /// </summary>
    public long? DurationMilliseconds { get; private set; }

    /// <summary>
    /// Gets the status resulting from this attempt.
    /// </summary>
    public DeliveryStatus Status { get; private set; }

    /// <summary>
    /// Gets the low-cardinality failure classification category, or <see langword="null"/> if successful.
    /// </summary>
    public string? FailureType { get; private set; }

    /// <summary>
    /// Gets the UTC creation timestamp.
    /// </summary>
    public DateTime CreatedAtUtc { get; private set; }

    /// <summary>
    /// Factory method to create a new delivery execution attempt.
    /// </summary>
    public static NotificationDeliveryAttempt Create(
        Guid deliveryId,
        int attemptNumber,
        DeliveryChannel channel,
        DateTime startedAtUtc)
    {
        return new NotificationDeliveryAttempt(Guid.NewGuid(), deliveryId, attemptNumber, channel, startedAtUtc);
    }

    /// <summary>
    /// Completes the delivery execution attempt with final status and metrics.
    /// </summary>
    public void Complete(DeliveryStatus finalStatus, DateTime completedAtUtc, string? failureType = null)
    {
        if (CompletedAtUtc.HasValue)
        {
            throw new InvalidOperationException("Execution attempt has already been completed and cannot be mutated.");
        }

        if (completedAtUtc < StartedAtUtc)
        {
            throw new ArgumentException("Completion timestamp cannot be earlier than start timestamp.", nameof(completedAtUtc));
        }

        CompletedAtUtc = completedAtUtc;
        DurationMilliseconds = (long)Math.Max(0, (completedAtUtc - StartedAtUtc).TotalMilliseconds);
        Status = finalStatus;
        FailureType = string.IsNullOrWhiteSpace(failureType) ? null : failureType.Trim();
    }
}
