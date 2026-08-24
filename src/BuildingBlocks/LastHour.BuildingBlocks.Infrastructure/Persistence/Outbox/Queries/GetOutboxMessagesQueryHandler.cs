using LastHour.BuildingBlocks.Application.Contracts;
using LastHour.BuildingBlocks.Application.Cqrs;
using LastHour.BuildingBlocks.Application.Models;
using LastHour.BuildingBlocks.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace LastHour.BuildingBlocks.Infrastructure.Persistence.Outbox.Queries;

/// <summary>
/// Handles <see cref="GetOutboxMessagesQuery"/> by applying provider-neutral filters and pagination,
/// projecting onto safe <see cref="OutboxMessageSummaryDto"/> items without materializing message content.
/// </summary>
public sealed class GetOutboxMessagesQueryHandler : IQueryHandler<GetOutboxMessagesQuery, PagedResult<OutboxMessageSummaryDto>>
{
    private readonly LastHourDbContext _dbContext;
    private readonly IClock _clock;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetOutboxMessagesQueryHandler"/> class.
    /// </summary>
    /// <param name="dbContext">The database context.</param>
    /// <param name="clock">The system clock provider.</param>
    public GetOutboxMessagesQueryHandler(LastHourDbContext dbContext, IClock clock)
    {
        _dbContext = dbContext;
        _clock = clock;
    }

    /// <inheritdoc/>
    public async Task<Result<PagedResult<OutboxMessageSummaryDto>>> Handle(
        GetOutboxMessagesQuery query,
        CancellationToken cancellationToken)
    {
        DateTime now = _clock.UtcNow;
        IQueryable<OutboxMessage> queryable = _dbContext.OutboxMessages.AsNoTracking();

        if (query.Status.HasValue)
        {
            switch (query.Status.Value)
            {
                case OutboxMessageStatus.Pending:
                    queryable = queryable.Where(m => m.ProcessedOnUtc == null
                        && (m.NextAttemptAtUtc == null || m.NextAttemptAtUtc <= now)
                        && (m.LockExpiresAtUtc == null || m.LockExpiresAtUtc < now));
                    break;

                case OutboxMessageStatus.Processing:
                    queryable = queryable.Where(m => m.ProcessedOnUtc == null
                        && m.LockExpiresAtUtc != null
                        && m.LockExpiresAtUtc >= now);
                    break;

                case OutboxMessageStatus.Scheduled:
                    queryable = queryable.Where(m => m.ProcessedOnUtc == null
                        && m.NextAttemptAtUtc != null
                        && m.NextAttemptAtUtc > now
                        && (m.LockExpiresAtUtc == null || m.LockExpiresAtUtc < now));
                    break;

                case OutboxMessageStatus.Processed:
                    queryable = queryable.Where(m => m.ProcessedOnUtc != null && m.Error == null);
                    break;

                case OutboxMessageStatus.Abandoned:
                    queryable = queryable.Where(m => m.ProcessedOnUtc != null && m.Error != null);
                    break;
            }
        }

        if (!string.IsNullOrWhiteSpace(query.EventType))
        {
            string filter = query.EventType.Trim();
            queryable = queryable.Where(m => m.Type.Contains(filter));
        }

        if (query.OccurredFromUtc.HasValue)
        {
            queryable = queryable.Where(m => m.OccurredOnUtc >= query.OccurredFromUtc.Value);
        }

        if (query.OccurredToUtc.HasValue)
        {
            queryable = queryable.Where(m => m.OccurredOnUtc <= query.OccurredToUtc.Value);
        }

        int totalCount = await queryable.CountAsync(cancellationToken);

        IOrderedQueryable<OutboxMessage> orderedQueryable = query.Status == OutboxMessageStatus.Scheduled
            ? queryable.OrderBy(m => m.NextAttemptAtUtc).ThenBy(m => m.OccurredOnUtc)
            : queryable.OrderByDescending(m => m.OccurredOnUtc);

        var rawItems = await orderedQueryable
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(m => new
            {
                m.Id,
                m.Type,
                m.OccurredOnUtc,
                m.ProcessedOnUtc,
                m.RetryCount,
                m.NextAttemptAtUtc,
                m.LockExpiresAtUtc,
                m.Error,
            })
            .ToListAsync(cancellationToken);

        var items = rawItems.Select(r => new OutboxMessageSummaryDto(
            r.Id,
            r.Type,
            OutboxStatusClassifier.Derive(r.ProcessedOnUtc, r.Error, r.NextAttemptAtUtc, r.LockExpiresAtUtc, now),
            r.OccurredOnUtc,
            r.ProcessedOnUtc,
            r.RetryCount,
            r.NextAttemptAtUtc,
            r.LockExpiresAtUtc)).ToList();

        var pagedResult = new PagedResult<OutboxMessageSummaryDto>(
            items,
            query.PageNumber,
            query.PageSize,
            totalCount);

        return pagedResult;
    }
}
