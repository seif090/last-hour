using LastHour.BuildingBlocks.Application.Contracts;
using LastHour.BuildingBlocks.Application.Cqrs;
using LastHour.BuildingBlocks.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace LastHour.BuildingBlocks.Infrastructure.Persistence.Outbox.Queries;

/// <summary>
/// Handles <see cref="GetOutboxMessageByIdQuery"/> by loading operational metadata for a single
/// outbox message, projecting onto <see cref="OutboxMessageDetailDto"/> without loading message content.
/// </summary>
public sealed class GetOutboxMessageByIdQueryHandler : IQueryHandler<GetOutboxMessageByIdQuery, OutboxMessageDetailDto>
{
    private readonly LastHourDbContext _dbContext;
    private readonly IClock _clock;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetOutboxMessageByIdQueryHandler"/> class.
    /// </summary>
    /// <param name="dbContext">The database context.</param>
    /// <param name="clock">The clock provider.</param>
    public GetOutboxMessageByIdQueryHandler(LastHourDbContext dbContext, IClock clock)
    {
        _dbContext = dbContext;
        _clock = clock;
    }

    /// <inheritdoc/>
    public async Task<Result<OutboxMessageDetailDto>> Handle(
        GetOutboxMessageByIdQuery query,
        CancellationToken cancellationToken)
    {
        DateTime now = _clock.UtcNow;

        var rawMessage = await _dbContext.OutboxMessages
            .AsNoTracking()
            .Where(m => m.Id == query.Id)
            .Select(m => new
            {
                m.Id,
                m.Type,
                m.OccurredOnUtc,
                m.ProcessedOnUtc,
                m.RetryCount,
                m.NextAttemptAtUtc,
                m.ClaimToken,
                m.LockExpiresAtUtc,
                m.Error,
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (rawMessage is null)
        {
            return Error.NotFound("Outbox.MessageNotFound", $"Outbox message with ID '{query.Id}' was not found.");
        }

        var dto = new OutboxMessageDetailDto(
            rawMessage.Id,
            rawMessage.Type,
            OutboxStatusClassifier.Derive(rawMessage.ProcessedOnUtc, rawMessage.Error, rawMessage.NextAttemptAtUtc, rawMessage.LockExpiresAtUtc, now),
            rawMessage.OccurredOnUtc,
            rawMessage.ProcessedOnUtc,
            rawMessage.RetryCount,
            rawMessage.NextAttemptAtUtc,
            rawMessage.ClaimToken,
            rawMessage.LockExpiresAtUtc,
            rawMessage.Error);

        return dto;
    }
}
