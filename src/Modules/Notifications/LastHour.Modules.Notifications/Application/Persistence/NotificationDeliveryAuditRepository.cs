using LastHour.BuildingBlocks.Application.Contracts;
using LastHour.Modules.Notifications.Domain.Notifications;

namespace LastHour.Modules.Notifications.Application.Persistence;

/// <summary>
/// EF Core implementation of <see cref="INotificationDeliveryAuditRepository"/>.
/// </summary>
public sealed class NotificationDeliveryAuditRepository : INotificationDeliveryAuditRepository
{
    private readonly IRepository<NotificationDeliveryAuditEntry> _repository;
    private readonly IReadRepository<NotificationDeliveryAuditEntry, Guid> _readRepository;

    /// <summary>
    /// Initializes a new instance of the <see cref="NotificationDeliveryAuditRepository"/> class.
    /// </summary>
    public NotificationDeliveryAuditRepository(
        IRepository<NotificationDeliveryAuditEntry> repository,
        IReadRepository<NotificationDeliveryAuditEntry, Guid> readRepository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _readRepository = readRepository ?? throw new ArgumentNullException(nameof(readRepository));
    }

    /// <inheritdoc/>
    public Task AddAsync(NotificationDeliveryAuditEntry entry, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entry);
        _repository.Add(entry);
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task<IReadOnlyList<NotificationDeliveryAuditEntry>> GetByDeliveryIdAsync(Guid deliveryId, CancellationToken cancellationToken = default)
    {
        return _readRepository.ListAsync(
            e => e.DeliveryId == deliveryId,
            e => e.OccurredAtUtc,
            sortDescending: true,
            skip: 0,
            take: 1000,
            cancellationToken: cancellationToken);
    }

    /// <inheritdoc/>
    public Task<IReadOnlyList<NotificationDeliveryAuditEntry>> GetByActorIdAsync(Guid actorId, CancellationToken cancellationToken = default)
    {
        return _readRepository.ListAsync(
            e => e.ActorId == actorId,
            e => e.OccurredAtUtc,
            sortDescending: true,
            skip: 0,
            take: 1000,
            cancellationToken: cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<int> DeleteOlderThanAsync(DateTime cutoffUtc, int batchSize, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<NotificationDeliveryAuditEntry> itemsToDelete = await _readRepository.ListAsync(
            e => e.OccurredAtUtc < cutoffUtc,
            e => e.OccurredAtUtc,
            sortDescending: false,
            skip: 0,
            take: batchSize,
            cancellationToken: cancellationToken);

        if (itemsToDelete.Count == 0)
        {
            return 0;
        }

        _repository.RemoveRange(itemsToDelete);
        return itemsToDelete.Count;
    }
}
