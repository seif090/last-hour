using LastHour.Modules.Notifications.Domain.Notifications;

namespace LastHour.Modules.Notifications.Application.Models;

/// <summary>
/// DTO representing operational summary metadata for a notification delivery.
/// </summary>
public sealed record NotificationDeliverySummaryDto
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NotificationDeliverySummaryDto"/> class.
    /// </summary>
    public NotificationDeliverySummaryDto(
        Guid id,
        Guid notificationId,
        DeliveryChannel channel,
        DeliveryStatus status,
        int retryCount,
        int maxRetryCount,
        DateTime? nextAttemptAtUtc,
        DateTime? lockExpiresAtUtc,
        DateTime createdAtUtc,
        DateTime? updatedAtUtc)
    {
        Id = id;
        NotificationId = notificationId;
        Channel = channel;
        Status = status;
        RetryCount = retryCount;
        MaxRetryCount = maxRetryCount;
        NextAttemptAtUtc = nextAttemptAtUtc;
        LockExpiresAtUtc = lockExpiresAtUtc;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = updatedAtUtc;
    }

    /// <summary>
    /// Gets the unique delivery identifier.
    /// </summary>
    public Guid Id { get; }

    /// <summary>
    /// Gets the parent notification identifier.
    /// </summary>
    public Guid NotificationId { get; }

    /// <summary>
    /// Gets the delivery channel (InApp, Email, Sms, Push).
    /// </summary>
    public DeliveryChannel Channel { get; }

    /// <summary>
    /// Gets the current delivery status.
    /// </summary>
    public DeliveryStatus Status { get; }

    /// <summary>
    /// Gets the number of previous failed execution attempts.
    /// </summary>
    public int RetryCount { get; }

    /// <summary>
    /// Gets the maximum allowed retry attempts.
    /// </summary>
    public int MaxRetryCount { get; }

    /// <summary>
    /// Gets the timestamp when the next attempt is scheduled.
    /// </summary>
    public DateTime? NextAttemptAtUtc { get; }

    /// <summary>
    /// Gets the timestamp when the active worker lease expires.
    /// </summary>
    public DateTime? LockExpiresAtUtc { get; }

    /// <summary>
    /// Gets the UTC creation timestamp.
    /// </summary>
    public DateTime CreatedAtUtc { get; }

    /// <summary>
    /// Gets the UTC timestamp of the last state modification.
    /// </summary>
    public DateTime? UpdatedAtUtc { get; }
}
