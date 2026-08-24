using LastHour.BuildingBlocks.Application.Cqrs;
using LastHour.BuildingBlocks.Application.Models;
using LastHour.Modules.Notifications.Application.Models;
using LastHour.Modules.Notifications.Domain.Notifications;

namespace LastHour.Modules.Notifications.Application.Queries;

/// <summary>
/// Query for listing notification deliveries with operational filters and pagination.
/// </summary>
public sealed record GetNotificationDeliveriesQuery : IQuery<PagedResult<NotificationDeliverySummaryDto>>
{
    /// <summary>
    /// Default page size when unspecified.
    /// </summary>
    public const int DefaultPageSize = 20;

    /// <summary>
    /// Maximum allowed page size.
    /// </summary>
    public const int MaxPageSize = 100;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetNotificationDeliveriesQuery"/> class.
    /// </summary>
    public GetNotificationDeliveriesQuery(
        DeliveryStatus? status = null,
        DeliveryChannel? channel = null,
        Guid? notificationId = null,
        DateTime? createdFromUtc = null,
        DateTime? createdToUtc = null,
        int pageNumber = 1,
        int pageSize = DefaultPageSize)
    {
        Status = status;
        Channel = channel;
        NotificationId = notificationId;
        CreatedFromUtc = createdFromUtc;
        CreatedToUtc = createdToUtc;
        PageNumber = pageNumber;
        PageSize = pageSize;
    }

    /// <summary>
    /// Gets the optional delivery status filter.
    /// </summary>
    public DeliveryStatus? Status { get; }

    /// <summary>
    /// Gets the optional delivery channel filter.
    /// </summary>
    public DeliveryChannel? Channel { get; }

    /// <summary>
    /// Gets the optional notification identifier filter.
    /// </summary>
    public Guid? NotificationId { get; }

    /// <summary>
    /// Gets the optional start creation timestamp filter (inclusive).
    /// </summary>
    public DateTime? CreatedFromUtc { get; }

    /// <summary>
    /// Gets the optional end creation timestamp filter (inclusive).
    /// </summary>
    public DateTime? CreatedToUtc { get; }

    /// <summary>
    /// Gets the 1-based page number.
    /// </summary>
    public int PageNumber { get; }

    /// <summary>
    /// Gets the page size.
    /// </summary>
    public int PageSize { get; }
}
