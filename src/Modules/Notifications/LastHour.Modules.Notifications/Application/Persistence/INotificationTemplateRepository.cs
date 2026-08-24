using LastHour.Modules.Notifications.Domain.Notifications;

namespace LastHour.Modules.Notifications.Application.Persistence;

/// <summary>
/// Repository interface for managing versioned <see cref="NotificationTemplate"/> persistence.
/// </summary>
public interface INotificationTemplateRepository
{
    /// <summary>
    /// Adds a new notification template version to persistence.
    /// </summary>
    Task AddAsync(NotificationTemplate notificationTemplate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the active template for a given notification type and delivery channel.
    /// Throws <see cref="InvalidOperationException"/> if multiple active templates exist (data integrity failure).
    /// </summary>
    Task<NotificationTemplate?> GetActiveTemplateAsync(string notificationType, DeliveryChannel channel, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a specific template version by key, channel, and version number.
    /// </summary>
    Task<NotificationTemplate?> GetByKeyAndVersionAsync(string templateKey, DeliveryChannel channel, int version, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists all template versions for a logical template key and channel.
    /// </summary>
    Task<IReadOnlyList<NotificationTemplate>> ListVersionsAsync(string templateKey, DeliveryChannel channel, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists all active and inactive templates.
    /// </summary>
    Task<IReadOnlyList<NotificationTemplate>> ListAllAsync(CancellationToken cancellationToken = default);
}
