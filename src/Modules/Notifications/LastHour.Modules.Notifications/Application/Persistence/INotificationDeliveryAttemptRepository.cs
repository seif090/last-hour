using LastHour.Modules.Notifications.Domain.Notifications;

namespace LastHour.Modules.Notifications.Application.Persistence;

/// <summary>
/// Repository interface for persisting and querying notification delivery execution attempt history.
/// </summary>
public interface INotificationDeliveryAttemptRepository
{
    /// <summary>
    /// Adds a new attempt entry to the repository.
    /// </summary>
    Task AddAsync(NotificationDeliveryAttempt attempt, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all attempt entries for a specific delivery identifier, ordered descending by attempt number.
    /// </summary>
    Task<IReadOnlyList<NotificationDeliveryAttempt>> GetByDeliveryIdAsync(Guid deliveryId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a specific attempt entry by delivery identifier and 1-based attempt sequence number.
    /// </summary>
    Task<NotificationDeliveryAttempt?> GetByDeliveryIdAndAttemptNumberAsync(Guid deliveryId, int attemptNumber, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes historical attempt records created prior to specified cutoff in bounded batches.
    /// </summary>
    Task<int> DeleteOlderThanAsync(DateTime cutoffUtc, int batchSize, CancellationToken cancellationToken = default);
}
