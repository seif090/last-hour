namespace LastHour.Modules.Notifications.Application.Delivery;

/// <summary>
/// Centralized constants for operational notification delivery audit actions.
/// </summary>
public static class NotificationOperationsAction
{
    /// <summary>
    /// Action recorded when a single notification delivery is requeued by an administrator.
    /// </summary>
    public const string Requeued = "Requeued";

    /// <summary>
    /// Action recorded when a notification delivery is requeued as part of a batch operation.
    /// </summary>
    public const string BatchRequeued = "BatchRequeued";

    /// <summary>
    /// Action recorded when an administrative requeue attempt is rejected due to invalid state or active lease.
    /// </summary>
    public const string RequeueRejected = "RequeueRejected";
}
