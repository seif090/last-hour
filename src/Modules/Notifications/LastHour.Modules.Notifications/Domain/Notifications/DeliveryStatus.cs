namespace LastHour.Modules.Notifications.Domain.Notifications;

/// <summary>
/// Represents the lifecycle status of a notification delivery attempt.
/// </summary>
public enum DeliveryStatus
{
    /// <summary>
    /// Delivery is queued and pending processing.
    /// </summary>
    Pending = 1,

    /// <summary>
    /// Delivery is currently claimed and being processed by a worker.
    /// </summary>
    Processing = 2,

    /// <summary>
    /// Delivery completed successfully.
    /// </summary>
    Delivered = 3,

    /// <summary>
    /// Delivery attempt failed and a retry has been scheduled.
    /// </summary>
    RetryScheduled = 4,

    /// <summary>
    /// Delivery attempt failed permanently after retry limit exhaustion or unrecoverable error.
    /// </summary>
    FailedPermanent = 5,
}
