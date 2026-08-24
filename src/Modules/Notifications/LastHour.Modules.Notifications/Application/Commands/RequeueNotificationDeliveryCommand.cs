using LastHour.BuildingBlocks.Application.Cqrs;

namespace LastHour.Modules.Notifications.Application.Commands;

/// <summary>
/// Command for an authenticated administrator to requeue a failed or stuck notification delivery.
/// </summary>
public sealed record RequeueNotificationDeliveryCommand : ICommand
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RequeueNotificationDeliveryCommand"/> class.
    /// </summary>
    /// <param name="deliveryId">The unique identifier of the delivery to requeue.</param>
    /// <param name="adminUserId">The unique identifier of the authenticated administrative actor.</param>
    /// <param name="reason">The administrative justification for requeuing.</param>
    /// <param name="correlationId">Optional request correlation identifier.</param>
    public RequeueNotificationDeliveryCommand(
        Guid deliveryId,
        Guid adminUserId,
        string reason,
        string? correlationId = null)
    {
        DeliveryId = deliveryId;
        AdminUserId = adminUserId;
        Reason = reason;
        CorrelationId = correlationId;
    }

    /// <summary>
    /// Gets the unique identifier of the delivery to requeue.
    /// </summary>
    public Guid DeliveryId { get; }

    /// <summary>
    /// Gets the unique identifier of the authenticated administrative actor.
    /// </summary>
    public Guid AdminUserId { get; }

    /// <summary>
    /// Gets the administrative justification for requeuing.
    /// </summary>
    public string Reason { get; }

    /// <summary>
    /// Gets the optional request correlation identifier.
    /// </summary>
    public string? CorrelationId { get; }
}
