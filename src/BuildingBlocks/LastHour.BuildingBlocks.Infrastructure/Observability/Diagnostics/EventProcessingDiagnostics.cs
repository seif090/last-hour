using LastHour.BuildingBlocks.Application.Contracts;
using LastHour.BuildingBlocks.Infrastructure.Persistence;
using LastHour.BuildingBlocks.Infrastructure.Persistence.Outbox;
using Microsoft.EntityFrameworkCore;

namespace LastHour.BuildingBlocks.Infrastructure.Observability.Diagnostics;

/// <summary>
/// Implements operational diagnostic queries for outbox and consumer inbox subsystems.
/// </summary>
public sealed class EventProcessingDiagnostics : IEventProcessingDiagnostics
{
    private readonly LastHourDbContext _dbContext;
    private readonly IOutboxProcessorTracker _tracker;
    private readonly IClock _clock;

    /// <summary>
    /// Initializes a new instance of the <see cref="EventProcessingDiagnostics"/> class.
    /// </summary>
    /// <param name="dbContext">The database context.</param>
    /// <param name="tracker">The outbox processor tracker.</param>
    /// <param name="clock">The system clock.</param>
    public EventProcessingDiagnostics(
        LastHourDbContext dbContext,
        IOutboxProcessorTracker tracker,
        IClock clock)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _tracker = tracker ?? throw new ArgumentNullException(nameof(tracker));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
    }

    /// <inheritdoc/>
    public async Task<OutboxDiagnosticsSnapshot> GetOutboxSnapshotAsync(CancellationToken cancellationToken = default)
    {
        DateTime utcNow = _clock.UtcNow;

        int pendingCount = await _dbContext.OutboxMessages
            .AsNoTracking()
            .CountAsync(m => m.ProcessedOnUtc == null, cancellationToken)
            .ConfigureAwait(false);

        int abandonedCount = await _dbContext.OutboxMessages
            .AsNoTracking()
            .CountAsync(m => m.ProcessedOnUtc != null && m.Error != null, cancellationToken)
            .ConfigureAwait(false);

        int scheduledCount = await _dbContext.OutboxMessages
            .AsNoTracking()
            .Where(m => m.ProcessedOnUtc == null
                     && m.NextAttemptAtUtc != null
                     && m.NextAttemptAtUtc > utcNow
                     && (m.LockExpiresAtUtc == null || m.LockExpiresAtUtc < utcNow))
            .CountAsync(cancellationToken)
            .ConfigureAwait(false);

        int activeProcessingCount = await _dbContext.OutboxMessages
            .AsNoTracking()
            .Where(m => m.ProcessedOnUtc == null
                     && m.LockExpiresAtUtc != null
                     && m.LockExpiresAtUtc >= utcNow)
            .CountAsync(cancellationToken)
            .ConfigureAwait(false);

        int readyPendingCount = Math.Max(0, pendingCount - scheduledCount - activeProcessingCount);

        DateTime? oldestOccurredOnUtc = null;
        if (pendingCount > 0)
        {
            oldestOccurredOnUtc = await _dbContext.OutboxMessages
                .AsNoTracking()
                .Where(m => m.ProcessedOnUtc == null)
                .Select(m => (DateTime?)m.OccurredOnUtc)
                .MinAsync(cancellationToken)
                .ConfigureAwait(false);
        }

        TimeSpan oldestAge = oldestOccurredOnUtc.HasValue && oldestOccurredOnUtc.Value < utcNow
            ? utcNow - oldestOccurredOnUtc.Value
            : TimeSpan.Zero;

        return new OutboxDiagnosticsSnapshot(
            totalPendingCount: pendingCount,
            readyPendingCount: readyPendingCount,
            scheduledRetryCount: scheduledCount,
            activeProcessingCount: activeProcessingCount,
            abandonedCount: abandonedCount,
            oldestPendingAge: oldestAge,
            processorIsRunning: _tracker.IsRunning,
            lastHeartbeatUtc: _tracker.LastHeartbeatUtc,
            lastSuccessUtc: _tracker.LastSuccessUtc,
            lastFailureUtc: _tracker.LastFailureUtc);
    }

    /// <inheritdoc/>
    public async Task<ConsumerInboxDiagnosticsSnapshot> GetConsumerInboxSnapshotAsync(CancellationToken cancellationToken = default)
    {
        DateTime utcNow = _clock.UtcNow;

        int totalCount = await _dbContext.ConsumerInboxMessages
            .AsNoTracking()
            .CountAsync(cancellationToken)
            .ConfigureAwait(false);

        int completedCount = await _dbContext.ConsumerInboxMessages
            .AsNoTracking()
            .CountAsync(m => m.CompletedOnUtc != null, cancellationToken)
            .ConfigureAwait(false);

        int activeLeaseCount = await _dbContext.ConsumerInboxMessages
            .AsNoTracking()
            .CountAsync(m => m.CompletedOnUtc == null && m.LockExpiresAtUtc != null && m.LockExpiresAtUtc >= utcNow, cancellationToken)
            .ConfigureAwait(false);

        int expiredLeaseCount = await _dbContext.ConsumerInboxMessages
            .AsNoTracking()
            .CountAsync(m => m.CompletedOnUtc == null && m.LockExpiresAtUtc != null && m.LockExpiresAtUtc < utcNow, cancellationToken)
            .ConfigureAwait(false);

        DateTime? oldestExpiredLeaseExpiresAtUtc = null;
        if (expiredLeaseCount > 0)
        {
            oldestExpiredLeaseExpiresAtUtc = await _dbContext.ConsumerInboxMessages
                .AsNoTracking()
                .Where(m => m.CompletedOnUtc == null && m.LockExpiresAtUtc != null && m.LockExpiresAtUtc < utcNow)
                .Select(m => m.LockExpiresAtUtc)
                .MinAsync(cancellationToken)
                .ConfigureAwait(false);
        }

        TimeSpan oldestExpiredAge = oldestExpiredLeaseExpiresAtUtc.HasValue && oldestExpiredLeaseExpiresAtUtc.Value < utcNow
            ? utcNow - oldestExpiredLeaseExpiresAtUtc.Value
            : TimeSpan.Zero;

        return new ConsumerInboxDiagnosticsSnapshot(
            totalInboxCount: totalCount,
            completedCount: completedCount,
            activeLeaseCount: activeLeaseCount,
            expiredLeaseCount: expiredLeaseCount,
            oldestExpiredLeaseAge: oldestExpiredAge);
    }
}
