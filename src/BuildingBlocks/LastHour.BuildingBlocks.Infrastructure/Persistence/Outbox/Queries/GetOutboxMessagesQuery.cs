using LastHour.BuildingBlocks.Application.Cqrs;
using LastHour.BuildingBlocks.Application.Models;

namespace LastHour.BuildingBlocks.Infrastructure.Persistence.Outbox.Queries;

/// <summary>
/// Queries outbox messages with pagination, filtering by operational status, event type, and date range.
/// </summary>
public sealed record GetOutboxMessagesQuery : IQuery<PagedResult<OutboxMessageSummaryDto>>
{
    /// <summary>
    /// The default page size.
    /// </summary>
    public const int DefaultPageSize = 20;

    /// <summary>
    /// The maximum allowed page size.
    /// </summary>
    public const int MaxPageSize = 100;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetOutboxMessagesQuery"/> class.
    /// </summary>
    /// <param name="status">Optional operational lifecycle status filter.</param>
    /// <param name="eventType">Optional event type name filter.</param>
    /// <param name="occurredFromUtc">Optional inclusive start boundary for occurred date.</param>
    /// <param name="occurredToUtc">Optional inclusive end boundary for occurred date.</param>
    /// <param name="pageNumber">The one-based page number.</param>
    /// <param name="pageSize">The number of items per page.</param>
    public GetOutboxMessagesQuery(
        OutboxMessageStatus? status = null,
        string? eventType = null,
        DateTime? occurredFromUtc = null,
        DateTime? occurredToUtc = null,
        int pageNumber = 1,
        int pageSize = DefaultPageSize)
    {
        Status = status;
        EventType = eventType;
        OccurredFromUtc = occurredFromUtc;
        OccurredToUtc = occurredToUtc;
        PageNumber = pageNumber;
        PageSize = pageSize;
    }

    /// <summary>
    /// Gets the optional operational status filter.
    /// </summary>
    public OutboxMessageStatus? Status { get; }

    /// <summary>
    /// Gets the optional event type filter.
    /// </summary>
    public string? EventType { get; }

    /// <summary>
    /// Gets the optional occurred from UTC date filter.
    /// </summary>
    public DateTime? OccurredFromUtc { get; }

    /// <summary>
    /// Gets the optional occurred to UTC date filter.
    /// </summary>
    public DateTime? OccurredToUtc { get; }

    /// <summary>
    /// Gets the one-based page number.
    /// </summary>
    public int PageNumber { get; }

    /// <summary>
    /// Gets the number of items per page.
    /// </summary>
    public int PageSize { get; }
}
