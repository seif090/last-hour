using LastHour.BuildingBlocks.Application.Cqrs;
using LastHour.Modules.Notifications.Application.Models;

namespace LastHour.Modules.Notifications.Application.Queries;

/// <summary>
/// Query for retrieving execution attempt history for a specific notification delivery.
/// </summary>
public sealed record GetNotificationDeliveryAttemptsQuery : IQuery<IReadOnlyCollection<NotificationDeliveryAttemptDto>>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GetNotificationDeliveryAttemptsQuery"/> class.
    /// </summary>
    /// <param name="deliveryId">The target delivery identifier.</param>
    public GetNotificationDeliveryAttemptsQuery(Guid deliveryId)
    {
        DeliveryId = deliveryId;
    }

    /// <summary>
    /// Gets the target delivery identifier.
    /// </summary>
    public Guid DeliveryId { get; }
}
