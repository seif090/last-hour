using LastHour.BuildingBlocks.Application.Cqrs;
using LastHour.BuildingBlocks.Application.Models;
using LastHour.Modules.Notifications.Application.Models;

namespace LastHour.Modules.Notifications.Application.Queries;

/// <summary>
/// Query to retrieve a paged list of notifications for the authenticated user.
/// </summary>
public sealed record GetMyNotificationsQuery : IQuery<PagedResult<NotificationDto>>
{
    /// <summary>
    /// The default page size when none is specified.
    /// </summary>
    public const int DefaultPageSize = 20;

    /// <summary>
    /// The maximum allowed page size.
    /// </summary>
    public const int MaxPageSize = 100;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetMyNotificationsQuery"/> class.
    /// </summary>
    /// <param name="userId">The unique identifier of the authenticated user requesting their notifications.</param>
    /// <param name="isRead">Optional filter by read status.</param>
    /// <param name="type">Optional filter by notification type.</param>
    /// <param name="pageNumber">The one-based page number.</param>
    /// <param name="pageSize">The page size (max 100).</param>
    public GetMyNotificationsQuery(
        Guid userId,
        bool? isRead = null,
        string? type = null,
        int pageNumber = 1,
        int pageSize = DefaultPageSize)
    {
        UserId = userId;
        IsRead = isRead;
        Type = type;
        PageNumber = pageNumber;
        PageSize = pageSize;
    }

    /// <summary>
    /// Gets the unique identifier of the authenticated user.
    /// </summary>
    public Guid UserId { get; }

    /// <summary>
    /// Gets the optional filter by read status.
    /// </summary>
    public bool? IsRead { get; }

    /// <summary>
    /// Gets the optional filter by notification type.
    /// </summary>
    public string? Type { get; }

    /// <summary>
    /// Gets the one-based page number.
    /// </summary>
    public int PageNumber { get; }

    /// <summary>
    /// Gets the page size.
    /// </summary>
    public int PageSize { get; }
}
