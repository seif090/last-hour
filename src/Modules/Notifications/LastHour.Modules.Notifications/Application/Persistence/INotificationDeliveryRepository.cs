using LastHour.BuildingBlocks.Application.Models;
using LastHour.Modules.Notifications.Domain.Notifications;

namespace LastHour.Modules.Notifications.Application.Persistence;

/// <summary>
/// Repository contract for accessing and persisting <see cref="NotificationDelivery"/> records.
/// </summary>
public interface INotificationDeliveryRepository
{
    /// <summary>
    /// Adds a new notification delivery to persistence.
    /// </summary>
    Task AddAsync(NotificationDelivery delivery, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a notification delivery by its unique identifier (untracked).
    /// </summary>
    Task<NotificationDelivery?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a notification delivery by its unique identifier, attached to change tracker.
    /// </summary>
    Task<NotificationDelivery?> GetByIdTrackedAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all deliveries for a specific notification.
    /// </summary>
    Task<IReadOnlyList<NotificationDelivery>> GetByNotificationIdAsync(Guid notificationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves eligible pending or expired processing deliveries up to specified batch size.
    /// </summary>
    Task<IReadOnlyList<NotificationDelivery>> GetPendingDeliveriesAsync(int batchSize, DateTime nowUtc, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a delivery already exists for a given notification and channel.
    /// </summary>
    Task<bool> ExistsByNotificationIdAndChannelAsync(Guid notificationId, DeliveryChannel channel, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a paginated list of notification deliveries based on optional operational filters.
    /// </summary>
    Task<PagedResult<NotificationDelivery>> GetPagedDeliveriesAsync(
        DeliveryStatus? status,
        DeliveryChannel? channel,
        Guid? notificationId,
        DateTime? createdFromUtc,
        DateTime? createdToUtc,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);
}
