using LastHour.BuildingBlocks.Application.Contracts;
using LastHour.BuildingBlocks.Application.Cqrs;
using LastHour.BuildingBlocks.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace LastHour.BuildingBlocks.Infrastructure.Persistence.Outbox.Commands;

/// <summary>
/// Handles <see cref="RequeueOutboxMessageCommand"/> by verifying the target message is abandoned,
/// resetting its retry state, persisting the change, and emitting the requeued metric.
/// </summary>
public sealed class RequeueOutboxMessageCommandHandler : ICommandHandler<RequeueOutboxMessageCommand>
{
    private readonly LastHourDbContext _dbContext;
    private readonly IClock _clock;
    private readonly OutboxMetrics _metrics;

    /// <summary>
    /// Initializes a new instance of the <see cref="RequeueOutboxMessageCommandHandler"/> class.
    /// </summary>
    /// <param name="dbContext">The database context used to access outbox messages.</param>
    /// <param name="clock">The system clock abstraction.</param>
    /// <param name="metrics">The outbox metrics collector.</param>
    public RequeueOutboxMessageCommandHandler(
        LastHourDbContext dbContext,
        IClock clock,
        OutboxMetrics metrics)
    {
        _dbContext = dbContext;
        _clock = clock;
        _metrics = metrics;
    }

    /// <inheritdoc/>
    public async Task<Result> Handle(RequeueOutboxMessageCommand request, CancellationToken cancellationToken)
    {
        OutboxMessage? message = await _dbContext.OutboxMessages
            .FirstOrDefaultAsync(m => m.Id == request.OutboxMessageId, cancellationToken)
            .ConfigureAwait(false);

        if (message is null)
        {
            return Result.Failure(Error.NotFound(
                "Outbox.MessageNotFound",
                $"Outbox message with ID '{request.OutboxMessageId}' was not found."));
        }

        if (!message.CanRequeue)
        {
            return Result.Failure(Error.Conflict(
                "Outbox.MessageNotAbandoned",
                $"Outbox message '{request.OutboxMessageId}' is not in an abandoned state and cannot be requeued."));
        }

        message.Requeue(_clock.UtcNow);
        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _metrics.RecordMessageRequeued(message.Type);
        return Result.Success();
    }
}
