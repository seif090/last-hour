using LastHour.BuildingBlocks.SharedKernel.Results;
using LastHour.Modules.Notifications.Domain.Notifications;

namespace LastHour.Modules.Notifications.Application.Delivery;

/// <summary>
/// Architecture contract for a channel-specific notification delivery sender.
/// Concrete implementations (adapters) reside strictly in Infrastructure.
/// </summary>
public interface INotificationChannelSender
{
    /// <summary>
    /// Gets the delivery channel handled by this sender.
    /// </summary>
    DeliveryChannel Channel { get; }

    /// <summary>
    /// Dispatches a notification through the target channel asynchronously.
    /// </summary>
    /// <param name="notification">The parent notification aggregate.</param>
    /// <param name="delivery">The specific delivery attempt entity.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A result indicating successful dispatch or sanitized failure.</returns>
    Task<Result> SendAsync(Notification notification, NotificationDelivery delivery, CancellationToken cancellationToken = default);
}
