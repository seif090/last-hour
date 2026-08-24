using LastHour.Modules.Notifications.Domain.Notifications;

namespace LastHour.Modules.Notifications.Application.Delivery;

/// <summary>
/// Provider-neutral template renderer performing safe placeholder substitution.
/// </summary>
public interface INotificationTemplateRenderer
{
    /// <summary>
    /// Renders template subject and body content using values from the specified template context.
    /// </summary>
    RenderedNotificationContent Render(NotificationTemplate notificationTemplate, NotificationTemplateContext context);
}
