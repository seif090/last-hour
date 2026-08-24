using LastHour.Modules.Notifications.Application.Delivery;
using LastHour.Modules.Notifications.Domain.Notifications;

namespace LastHour.Modules.Notifications.Infrastructure.Delivery;

/// <summary>
/// Production implementation of <see cref="INotificationChannelFactory"/> resolving registered senders.
/// </summary>
public sealed class NotificationChannelFactory : INotificationChannelFactory
{
    private readonly Dictionary<DeliveryChannel, INotificationChannelSender> _senders;

    /// <summary>
    /// Initializes a new instance of the <see cref="NotificationChannelFactory"/> class.
    /// </summary>
    public NotificationChannelFactory(IEnumerable<INotificationChannelSender> senders)
    {
        ArgumentNullException.ThrowIfNull(senders);
        _senders = senders.ToDictionary(s => s.Channel, s => s);
    }

    /// <inheritdoc/>
    public INotificationChannelSender GetSender(DeliveryChannel channel)
    {
        if (_senders.TryGetValue(channel, out INotificationChannelSender? sender))
        {
            return sender;
        }

        throw new NotSupportedException($"No notification channel sender registered for channel '{channel}'.");
    }
}
