using LastHour.Modules.Notifications.Domain.Notifications;

namespace LastHour.Modules.Notifications.Application.Delivery;

/// <summary>
/// Domain service interface for resolving active notification templates for a notification type and channel.
/// </summary>
public interface INotificationTemplateResolver
{
    /// <summary>
    /// Resolves the active template for a given notification type and delivery channel, falling back to defaults if unconfigured.
    /// </summary>
    Task<NotificationTemplate> ResolveTemplateAsync(string notificationType, DeliveryChannel channel, CancellationToken cancellationToken = default);
}
