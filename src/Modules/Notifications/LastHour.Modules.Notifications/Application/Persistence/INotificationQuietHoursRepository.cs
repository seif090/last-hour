using LastHour.Modules.Notifications.Domain.Notifications;

namespace LastHour.Modules.Notifications.Application.Persistence;

/// <summary>
/// Repository interface for managing user <see cref="NotificationQuietHours"/> persistence.
/// </summary>
public interface INotificationQuietHoursRepository
{
    /// <summary>
    /// Adds a new quiet hours configuration to persistence.
    /// </summary>
    Task AddAsync(NotificationQuietHours quietHours, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing quiet hours configuration in persistence.
    /// </summary>
    Task UpdateAsync(NotificationQuietHours quietHours, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the quiet hours configuration for a user.
    /// </summary>
    Task<NotificationQuietHours?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
}
