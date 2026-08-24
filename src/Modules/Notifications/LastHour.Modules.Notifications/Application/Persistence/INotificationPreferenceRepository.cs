using LastHour.Modules.Notifications.Domain.Notifications;

namespace LastHour.Modules.Notifications.Application.Persistence;

/// <summary>
/// Repository interface for managing user <see cref="NotificationPreference"/> persistence.
/// </summary>
public interface INotificationPreferenceRepository
{
    /// <summary>
    /// Adds a new notification preference to persistence.
    /// </summary>
    Task AddAsync(NotificationPreference preference, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing notification preference in persistence.
    /// </summary>
    Task UpdateAsync(NotificationPreference preference, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a specific user preference by user ID, notification type, and channel.
    /// </summary>
    Task<NotificationPreference?> GetByUserAndTypeAndChannelAsync(Guid userId, string notificationType, DeliveryChannel channel, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all preferences for a user for a specific notification type across all channels.
    /// </summary>
    Task<IReadOnlyList<NotificationPreference>> GetByUserAndTypeAsync(Guid userId, string notificationType, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all notification preferences configured for a user.
    /// </summary>
    Task<IReadOnlyList<NotificationPreference>> GetByUserAsync(Guid userId, CancellationToken cancellationToken = default);
}
