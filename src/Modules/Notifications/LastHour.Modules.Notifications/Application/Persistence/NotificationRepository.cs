using System.Linq.Expressions;
using LastHour.BuildingBlocks.Application.Contracts;
using LastHour.BuildingBlocks.Application.Models;
using LastHour.Modules.Notifications.Domain.Notifications;

namespace LastHour.Modules.Notifications.Application.Persistence;

/// <summary>
/// The <see cref="INotificationRepository"/> implementation, composed purely from shared repository contracts.
/// </summary>
public sealed class NotificationRepository : INotificationRepository
{
    private readonly IRepository<Notification> _repository;
    private readonly IReadRepository<Notification, Guid> _readRepository;
    private readonly ITrackedRepository<Notification, Guid>? _trackedRepository;

    /// <summary>
    /// Initializes a new instance of the <see cref="NotificationRepository"/> class with untracked and tracked repositories.
    /// </summary>
    /// <param name="repository">The shared write repository for the <see cref="Notification"/> aggregate.</param>
    /// <param name="readRepository">The shared read repository for <see cref="Notification"/> entities.</param>
    /// <param name="trackedRepository">The shared tracked repository for <see cref="Notification"/> entities.</param>
    public NotificationRepository(
        IRepository<Notification> repository,
        IReadRepository<Notification, Guid> readRepository,
        ITrackedRepository<Notification, Guid>? trackedRepository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _readRepository = readRepository ?? throw new ArgumentNullException(nameof(readRepository));
        _trackedRepository = trackedRepository;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="NotificationRepository"/> class with read and write repositories.
    /// </summary>
    /// <param name="repository">The shared write repository for the <see cref="Notification"/> aggregate.</param>
    /// <param name="readRepository">The shared read repository for <see cref="Notification"/> entities.</param>
    public NotificationRepository(
        IRepository<Notification> repository,
        IReadRepository<Notification, Guid> readRepository)
        : this(repository, readRepository, null)
    {
    }

    /// <inheritdoc/>
    public Task AddAsync(Notification notification, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(notification);
        _repository.Add(notification);
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task<Notification?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => _readRepository.GetByIdAsync(id, cancellationToken);

    /// <inheritdoc/>
    public Task<Notification?> GetByIdTrackedAsync(Guid id, CancellationToken cancellationToken = default)
        => _trackedRepository is not null
            ? _trackedRepository.GetByIdAsync(id, cancellationToken)
            : _readRepository.GetByIdAsync(id, cancellationToken);

    /// <inheritdoc/>
    public Task<Notification?> GetByEventIdAsync(Guid eventId, CancellationToken cancellationToken = default)
        => _readRepository.FirstOrDefaultAsync(n => n.EventId == eventId, cancellationToken);

    /// <inheritdoc/>
    public Task<Notification?> GetByEventIdAndTypeAsync(Guid eventId, string type, CancellationToken cancellationToken = default)
        => _readRepository.FirstOrDefaultAsync(n => n.EventId == eventId && n.Type == type, cancellationToken);

    /// <inheritdoc/>
    public Task<bool> ExistsByEventIdAndTypeAsync(Guid eventId, string type, CancellationToken cancellationToken = default)
        => _readRepository.AnyAsync(n => n.EventId == eventId && n.Type == type, cancellationToken);

    /// <inheritdoc/>
    public Task<IReadOnlyList<Notification>> GetByRecipientIdAsync(Guid recipientId, CancellationToken cancellationToken = default)
        => _readRepository.ListAsync(
            n => n.RecipientId == recipientId,
            n => n.CreatedAtUtc,
            true,
            0,
            100,
            cancellationToken);

    /// <inheritdoc/>
    public async Task<PagedResult<Notification>> GetPagedByRecipientIdAsync(
        Guid recipientId,
        bool? isRead,
        string? type,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        Expression<Func<Notification, bool>> predicate = BuildPredicate(recipientId, isRead, type);
        int totalCount = await _readRepository.CountAsync(predicate, cancellationToken);

        if (totalCount == 0)
        {
            return new PagedResult<Notification>(Array.Empty<Notification>(), pageNumber, pageSize, 0);
        }

        int skip = (pageNumber - 1) * pageSize;
        IReadOnlyList<Notification> items = await _readRepository.ListAsync(
            predicate,
            n => n.CreatedAtUtc,
            sortDescending: true,
            skip,
            pageSize,
            cancellationToken);

        return new PagedResult<Notification>(items, pageNumber, pageSize, totalCount);
    }

    /// <inheritdoc/>
    public async Task<int> MarkAllAsReadAsync(Guid recipientId, DateTime readAtUtc, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<Notification> unread = await _readRepository.ListAsync(
            n => n.RecipientId == recipientId && !n.IsRead,
            cancellationToken);

        if (unread.Count == 0)
        {
            return 0;
        }

        foreach (Notification item in unread)
        {
            if (_trackedRepository is not null)
            {
                Notification? tracked = await _trackedRepository.GetByIdAsync(item.Id, cancellationToken);
                tracked?.MarkAsRead(readAtUtc);
            }
            else
            {
                item.MarkAsRead(readAtUtc);
            }
        }

        return unread.Count;
    }

    private static Expression<Func<Notification, bool>> BuildPredicate(Guid recipientId, bool? isRead, string? type)
    {
        string? trimmedType = string.IsNullOrWhiteSpace(type) ? null : type.Trim();

        if (isRead.HasValue && trimmedType is not null)
        {
            bool read = isRead.Value;
            return n => n.RecipientId == recipientId && n.IsRead == read && n.Type == trimmedType;
        }

        if (isRead.HasValue)
        {
            bool read = isRead.Value;
            return n => n.RecipientId == recipientId && n.IsRead == read;
        }

        if (trimmedType is not null)
        {
            return n => n.RecipientId == recipientId && n.Type == trimmedType;
        }

        return n => n.RecipientId == recipientId;
    }
}
