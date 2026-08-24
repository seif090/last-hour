using LastHour.Modules.Notifications.Domain.Notifications;

namespace LastHour.Modules.Notifications.Application.Models;

/// <summary>
/// DTO representing an execution attempt for a notification delivery.
/// </summary>
public sealed record NotificationDeliveryAttemptDto
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NotificationDeliveryAttemptDto"/> class.
    /// </summary>
    public NotificationDeliveryAttemptDto(
        Guid id,
        Guid deliveryId,
        int attemptNumber,
        DeliveryChannel channel,
        DateTime startedAtUtc,
        DateTime? completedAtUtc,
        long? durationMilliseconds,
        DeliveryStatus status,
        string? failureType,
        DateTime createdAtUtc)
    {
        Id = id;
        DeliveryId = deliveryId;
        AttemptNumber = attemptNumber;
        Channel = channel;
        StartedAtUtc = startedAtUtc;
        CompletedAtUtc = completedAtUtc;
        DurationMilliseconds = durationMilliseconds;
        Status = status;
        FailureType = failureType;
        CreatedAtUtc = createdAtUtc;
    }

    /// <summary>
    /// Gets the unique attempt identifier.
    /// </summary>
    public Guid Id { get; }

    /// <summary>
    /// Gets the parent delivery identifier.
    /// </summary>
    public Guid DeliveryId { get; }

    /// <summary>
    /// Gets the monotonic 1-based attempt sequence number.
    /// </summary>
    public int AttemptNumber { get; }

    /// <summary>
    /// Gets the delivery channel.
    /// </summary>
    public DeliveryChannel Channel { get; }

    /// <summary>
    /// Gets the UTC start timestamp.
    /// </summary>
    public DateTime StartedAtUtc { get; }

    /// <summary>
    /// Gets the UTC completion timestamp, or <see langword="null"/> if in progress.
    /// </summary>
    public DateTime? CompletedAtUtc { get; }

    /// <summary>
    /// Gets the total execution duration in milliseconds.
    /// </summary>
    public long? DurationMilliseconds { get; }

    /// <summary>
    /// Gets the resulting delivery status.
    /// </summary>
    public DeliveryStatus Status { get; }

    /// <summary>
    /// Gets the low-cardinality failure classification category.
    /// </summary>
    public string? FailureType { get; }

    /// <summary>
    /// Gets the UTC creation timestamp.
    /// </summary>
    public DateTime CreatedAtUtc { get; }
}
