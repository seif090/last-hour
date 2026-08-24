using LastHour.BuildingBlocks.Application.Models;
using LastHour.Modules.Notifications.Domain.Notifications;

namespace LastHour.Modules.Notifications.Application.Persistence;

/// <summary>
/// Repository abstraction for accessing and persisting <see cref="Notification"/> records.
/// </summary>
public interface INotificationRepository
{
    /// <summary>
    /// Adds a new notification to the persistence store.
    /// </summary>
    /// <param name="notification">The notification entity to add.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task AddAsync(Notification notification, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a notification by its unique identifier (untracked read-only).
    /// </summary>
    /// <param name="id">The notification unique identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The matching notification if found; otherwise <see langword="null"/>.</returns>
    Task<Notification?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a notification by its unique identifier and attaches it to the change tracker for mutation.
    /// </summary>
    /// <param name="id">The notification unique identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The matching tracked notification if found; otherwise <see langword="null"/>.</returns>
    Task<Notification?> GetByIdTrackedAsync(Guid id, CancellationToken cancellationToken = default)
        => GetByIdAsync(id, cancellationToken);

    /// <summary>
    /// Retrieves a notification by its originating domain event identifier.
    /// </summary>
    /// <param name="eventId">The domain event identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The matching notification if found; otherwise <see langword="null"/>.</returns>
    Task<Notification?> GetByEventIdAsync(Guid eventId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a notification by its originating domain event identifier and notification type.
    /// </summary>
    /// <param name="eventId">The domain event identifier.</param>
    /// <param name="type">The notification type.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The matching notification if found; otherwise <see langword="null"/>.</returns>
    Task<Notification?> GetByEventIdAndTypeAsync(Guid eventId, string type, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks whether a notification already exists for the specified domain event identifier and notification type.
    /// </summary>
    /// <param name="eventId">The domain event identifier.</param>
    /// <param name="type">The notification type.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns><see langword="true"/> if a matching notification exists; otherwise <see langword="false"/>.</returns>
    Task<bool> ExistsByEventIdAndTypeAsync(Guid eventId, string type, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all notifications for a specific recipient.
    /// </summary>
    /// <param name="recipientId">The recipient identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A read-only list of notifications for the recipient.</returns>
    Task<IReadOnlyList<Notification>> GetByRecipientIdAsync(Guid recipientId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves an ordered, filtered page of notifications for a specific recipient.
    /// </summary>
    /// <param name="recipientId">The recipient identifier.</param>
    /// <param name="isRead">The optional read state filter.</param>
    /// <param name="type">The optional notification type filter.</param>
    /// <param name="pageNumber">The one-based page number.</param>
    /// <param name="pageSize">The number of items per page.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A paged result of notifications.</returns>
    Task<PagedResult<Notification>> GetPagedByRecipientIdAsync(
        Guid recipientId,
        bool? isRead,
        string? type,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
        => Task.FromResult(new PagedResult<Notification>(Array.Empty<Notification>(), pageNumber, pageSize, 0));

    /// <summary>
    /// Marks all unread notifications for a specific recipient as read.
    /// </summary>
    /// <param name="recipientId">The recipient identifier.</param>
    /// <param name="readAtUtc">The current UTC timestamp.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The number of notifications updated.</returns>
    Task<int> MarkAllAsReadAsync(Guid recipientId, DateTime readAtUtc, CancellationToken cancellationToken = default)
        => Task.FromResult(0);
}
