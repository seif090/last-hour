using LastHour.BuildingBlocks.Application.Cqrs;
using LastHour.Modules.Notifications.Application.Models;

namespace LastHour.Modules.Notifications.Application.Queries;

/// <summary>
/// Query for retrieving operational details for a specific notification delivery.
/// </summary>
public sealed record GetNotificationDeliveryDetailsQuery : IQuery<NotificationDeliveryDetailDto>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GetNotificationDeliveryDetailsQuery"/> class.
    /// </summary>
    /// <param name="deliveryId">The target delivery identifier.</param>
    public GetNotificationDeliveryDetailsQuery(Guid deliveryId)
    {
        DeliveryId = deliveryId;
    }

    /// <summary>
    /// Gets the target delivery identifier.
    /// </summary>
    public Guid DeliveryId { get; }
}
