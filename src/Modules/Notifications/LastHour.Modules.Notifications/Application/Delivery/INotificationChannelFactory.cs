using LastHour.Modules.Notifications.Domain.Notifications;

namespace LastHour.Modules.Notifications.Application.Delivery;

/// <summary>
/// Factory contract for resolving channel senders dynamically by <see cref="DeliveryChannel"/>.
/// </summary>
public interface INotificationChannelFactory
{
    /// <summary>
    /// Gets the channel sender registered for the specified delivery channel.
    /// </summary>
    /// <param name="channel">The target delivery channel.</param>
    /// <returns>The registered <see cref="INotificationChannelSender"/> instance.</returns>
    INotificationChannelSender GetSender(DeliveryChannel channel);
}
