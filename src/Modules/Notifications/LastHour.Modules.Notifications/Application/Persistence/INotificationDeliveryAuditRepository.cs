using LastHour.Modules.Notifications.Domain.Notifications;

namespace LastHour.Modules.Notifications.Application.Persistence;

/// <summary>
/// Repository interface for persisting and querying administrative notification delivery audit trail entries.
/// </summary>
public interface INotificationDeliveryAuditRepository
{
    /// <summary>
    /// Adds a new operational audit entry to the repository.
    /// </summary>
    Task AddAsync(NotificationDeliveryAuditEntry entry, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all operational audit entries for a specific delivery, ordered deterministically.
    /// </summary>
    Task<IReadOnlyList<NotificationDeliveryAuditEntry>> GetByDeliveryIdAsync(Guid deliveryId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all operational audit entries performed by an administrative actor, ordered deterministically.
    /// </summary>
    Task<IReadOnlyList<NotificationDeliveryAuditEntry>> GetByActorIdAsync(Guid actorId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes historical audit entries recorded prior to specified cutoff in bounded batches.
    /// </summary>
    Task<int> DeleteOlderThanAsync(DateTime cutoffUtc, int batchSize, CancellationToken cancellationToken = default);
}
