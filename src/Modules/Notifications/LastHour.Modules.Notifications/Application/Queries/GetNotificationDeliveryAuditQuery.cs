using LastHour.BuildingBlocks.Application.Cqrs;
using LastHour.Modules.Notifications.Application.Models;

namespace LastHour.Modules.Notifications.Application.Queries;

/// <summary>
/// Query for retrieving administrative audit history for a specific notification delivery.
/// </summary>
public sealed record GetNotificationDeliveryAuditQuery : IQuery<IReadOnlyCollection<NotificationDeliveryAuditDto>>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GetNotificationDeliveryAuditQuery"/> class.
    /// </summary>
    /// <param name="deliveryId">The target delivery identifier.</param>
    public GetNotificationDeliveryAuditQuery(Guid deliveryId)
    {
        DeliveryId = deliveryId;
    }

    /// <summary>
    /// Gets the target delivery identifier.
    /// </summary>
    public Guid DeliveryId { get; }
}
