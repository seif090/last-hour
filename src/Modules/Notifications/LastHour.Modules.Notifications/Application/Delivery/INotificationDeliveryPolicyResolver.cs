using LastHour.Modules.Notifications.Domain.Notifications;

namespace LastHour.Modules.Notifications.Application.Delivery;

/// <summary>
/// Domain policy contract for resolving applicable delivery channels for a given notification.
/// </summary>
public interface INotificationDeliveryPolicyResolver
{
    /// <summary>
    /// Synchronously resolves candidate candidate delivery channels.
    /// </summary>
    IReadOnlyList<DeliveryChannel> ResolveChannels(Notification notification);

    /// <summary>
    /// Asynchronously resolves candidate channels and applies user preferences and security invariants.
    /// </summary>
    Task<IReadOnlyList<DeliveryChannel>> ResolveChannelsAsync(Notification notification, CancellationToken cancellationToken = default);
}
