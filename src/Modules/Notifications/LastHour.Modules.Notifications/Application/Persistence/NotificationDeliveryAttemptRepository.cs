using LastHour.BuildingBlocks.Application.Contracts;
using LastHour.Modules.Notifications.Domain.Notifications;

namespace LastHour.Modules.Notifications.Application.Persistence;

/// <summary>
/// EF Core implementation of <see cref="INotificationDeliveryAttemptRepository"/>.
/// </summary>
public sealed class NotificationDeliveryAttemptRepository : INotificationDeliveryAttemptRepository
{
    private readonly IRepository<NotificationDeliveryAttempt> _repository;
    private readonly IReadRepository<NotificationDeliveryAttempt, Guid> _readRepository;

    /// <summary>
    /// Initializes a new instance of the <see cref="NotificationDeliveryAttemptRepository"/> class.
    /// </summary>
    public NotificationDeliveryAttemptRepository(
        IRepository<NotificationDeliveryAttempt> repository,
        IReadRepository<NotificationDeliveryAttempt, Guid> readRepository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _readRepository = readRepository ?? throw new ArgumentNullException(nameof(readRepository));
    }

    /// <inheritdoc/>
    public Task AddAsync(NotificationDeliveryAttempt attempt, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(attempt);
        _repository.Add(attempt);
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task<IReadOnlyList<NotificationDeliveryAttempt>> GetByDeliveryIdAsync(Guid deliveryId, CancellationToken cancellationToken = default)
    {
        return _readRepository.ListAsync(
            a => a.DeliveryId == deliveryId,
            a => a.AttemptNumber,
            sortDescending: true,
            skip: 0,
            take: 1000,
            cancellationToken: cancellationToken);
    }

    /// <inheritdoc/>
    public Task<NotificationDeliveryAttempt?> GetByDeliveryIdAndAttemptNumberAsync(Guid deliveryId, int attemptNumber, CancellationToken cancellationToken = default)
    {
        return _readRepository.FirstOrDefaultAsync(
            a => a.DeliveryId == deliveryId && a.AttemptNumber == attemptNumber,
            cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<int> DeleteOlderThanAsync(DateTime cutoffUtc, int batchSize, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<NotificationDeliveryAttempt> itemsToDelete = await _readRepository.ListAsync(
            a => a.CreatedAtUtc < cutoffUtc,
            a => a.CreatedAtUtc,
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
