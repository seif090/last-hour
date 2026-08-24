using LastHour.BuildingBlocks.Application.Contracts;
using LastHour.BuildingBlocks.Application.Models;
using LastHour.Modules.Notifications.Domain.Notifications;

namespace LastHour.Modules.Notifications.Application.Persistence;

/// <summary>
/// Repository implementation for managing <see cref="NotificationDelivery"/> persistence via EF Core repositories.
/// </summary>
public sealed class NotificationDeliveryRepository : INotificationDeliveryRepository
{
    private readonly IRepository<NotificationDelivery> _repository;
    private readonly IReadRepository<NotificationDelivery, Guid> _readRepository;
    private readonly ITrackedRepository<NotificationDelivery, Guid>? _trackedRepository;

    /// <summary>
    /// Initializes a new instance of the <see cref="NotificationDeliveryRepository"/> class.
    /// </summary>
    public NotificationDeliveryRepository(
        IRepository<NotificationDelivery> repository,
        IReadRepository<NotificationDelivery, Guid> readRepository,
        ITrackedRepository<NotificationDelivery, Guid>? trackedRepository = null)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _readRepository = readRepository ?? throw new ArgumentNullException(nameof(readRepository));
        _trackedRepository = trackedRepository;
    }

    /// <inheritdoc/>
    public Task AddAsync(NotificationDelivery delivery, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(delivery);
        _repository.Add(delivery);
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task<NotificationDelivery?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => _readRepository.GetByIdAsync(id, cancellationToken);

    /// <inheritdoc/>
    public Task<NotificationDelivery?> GetByIdTrackedAsync(Guid id, CancellationToken cancellationToken = default)
        => _trackedRepository is not null
            ? _trackedRepository.GetByIdAsync(id, cancellationToken)
            : _readRepository.GetByIdAsync(id, cancellationToken);

    /// <inheritdoc/>
    public Task<IReadOnlyList<NotificationDelivery>> GetByNotificationIdAsync(Guid notificationId, CancellationToken cancellationToken = default)
        => _readRepository.ListAsync(d => d.NotificationId == notificationId, cancellationToken);

    /// <inheritdoc/>
    public Task<IReadOnlyList<NotificationDelivery>> GetPendingDeliveriesAsync(int batchSize, DateTime nowUtc, CancellationToken cancellationToken = default)
    {
        return _readRepository.ListAsync(
            d => ((d.Status == DeliveryStatus.Pending || d.Status == DeliveryStatus.RetryScheduled) && (d.NextAttemptAtUtc == null || d.NextAttemptAtUtc <= nowUtc) && (d.LockExpiresAtUtc == null || d.LockExpiresAtUtc < nowUtc))
              || (d.Status == DeliveryStatus.Processing && d.LockExpiresAtUtc != null && d.LockExpiresAtUtc < nowUtc),
            d => d.NextAttemptAtUtc ?? d.CreatedAtUtc,
            sortDescending: false,
            skip: 0,
            take: batchSize,
            cancellationToken: cancellationToken);
    }

    /// <inheritdoc/>
    public Task<bool> ExistsByNotificationIdAndChannelAsync(Guid notificationId, DeliveryChannel channel, CancellationToken cancellationToken = default)
        => _readRepository.AnyAsync(d => d.NotificationId == notificationId && d.Channel == channel, cancellationToken);

    /// <inheritdoc/>
    public async Task<PagedResult<NotificationDelivery>> GetPagedDeliveriesAsync(
        DeliveryStatus? status,
        DeliveryChannel? channel,
        Guid? notificationId,
        DateTime? createdFromUtc,
        DateTime? createdToUtc,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        System.Linq.Expressions.Expression<Func<NotificationDelivery, bool>> predicate = d =>
            (!status.HasValue || d.Status == status.Value) &&
            (!channel.HasValue || d.Channel == channel.Value) &&
            (!notificationId.HasValue || d.NotificationId == notificationId.Value) &&
            (!createdFromUtc.HasValue || d.CreatedAtUtc >= createdFromUtc.Value) &&
            (!createdToUtc.HasValue || d.CreatedAtUtc <= createdToUtc.Value);

        int totalCount = await _readRepository.CountAsync(predicate, cancellationToken);
        int skip = (pageNumber - 1) * pageSize;

        IReadOnlyList<NotificationDelivery> items = await _readRepository.ListAsync(
            predicate,
            d => d.CreatedAtUtc,
            sortDescending: true,
            skip: skip,
            take: pageSize,
            cancellationToken: cancellationToken);

        return new PagedResult<NotificationDelivery>(items, pageNumber, pageSize, totalCount);
    }
}
