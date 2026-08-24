using System.Collections.ObjectModel;
using LastHour.BuildingBlocks.Application.Contracts;
using LastHour.BuildingBlocks.Infrastructure.Persistence;
using LastHour.BuildingBlocks.Infrastructure.Persistence.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace LastHour.BuildingBlocks.Infrastructure.HealthChecks;

/// <summary>
/// Evaluates the health of the outbox infrastructure based on processor liveness,
/// pending backlog size and the age of the oldest unprocessed message.
/// </summary>
public sealed class OutboxHealthCheck : IHealthCheck
{
    private readonly LastHourDbContext _dbContext;
    private readonly IOutboxProcessorTracker _tracker;
    private readonly IClock _clock;
    private readonly IOptions<HealthChecksOptions> _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="OutboxHealthCheck"/> class.
    /// </summary>
    /// <param name="dbContext">The database context used to query aggregate outbox metrics.</param>
    /// <param name="tracker">The in-memory processor tracker.</param>
    /// <param name="clock">The system clock abstraction.</param>
    /// <param name="options">The health check options.</param>
    public OutboxHealthCheck(
        LastHourDbContext dbContext,
        IOutboxProcessorTracker tracker,
        IClock clock,
        IOptions<HealthChecksOptions> options)
    {
        _dbContext = dbContext;
        _tracker = tracker;
        _clock = clock;
        _options = options;
    }

    /// <inheritdoc/>
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        OutboxHealthCheckOptions outboxOptions = _options.Value.Outbox;
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

        DateTime? oldestReadyOccurredOnUtc = null;
        if (readyPendingCount > 0)
        {
            oldestReadyOccurredOnUtc = await _dbContext.OutboxMessages
                .AsNoTracking()
                .Where(m => m.ProcessedOnUtc == null
                         && (m.NextAttemptAtUtc == null || m.NextAttemptAtUtc <= utcNow)
                         && (m.LockExpiresAtUtc == null || m.LockExpiresAtUtc < utcNow))
                .Select(m => (DateTime?)m.OccurredOnUtc)
                .MinAsync(cancellationToken)
                .ConfigureAwait(false);
        }

        TimeSpan oldestReadyAge = oldestReadyOccurredOnUtc.HasValue && oldestReadyOccurredOnUtc.Value < utcNow
            ? utcNow - oldestReadyOccurredOnUtc.Value
            : TimeSpan.Zero;

        var data = new Dictionary<string, object>
        {
            ["total_pending"] = pendingCount,
            ["pending_message_count"] = pendingCount,
            ["ready_pending_count"] = readyPendingCount,
            ["scheduled_retry_count"] = scheduledCount,
            ["active_processing_count"] = activeProcessingCount,
            ["abandoned_message_count"] = abandonedCount,
            ["oldest_pending_age_seconds"] = oldestAge.TotalSeconds,
            ["oldest_pending_message_age_seconds"] = oldestAge.TotalSeconds,
            ["oldest_ready_age_seconds"] = oldestReadyAge.TotalSeconds,
            ["processor_is_running"] = _tracker.IsRunning,
            ["last_heartbeat_utc"] = _tracker.LastHeartbeatUtc?.ToString("O") ?? "never",
            ["last_success_utc"] = _tracker.LastSuccessUtc?.ToString("O") ?? "never",
            ["last_failure_utc"] = _tracker.LastFailureUtc?.ToString("O") ?? "never",
        };

        var readOnlyData = new ReadOnlyDictionary<string, object>(data);

        // Check for stale processor heartbeat
        if (_tracker.IsRunning && _tracker.LastHeartbeatUtc.HasValue)
        {
            TimeSpan heartbeatAge = utcNow - _tracker.LastHeartbeatUtc.Value;
            if (heartbeatAge > outboxOptions.ProcessorHeartbeatTimeout)
            {
                return HealthCheckResult.Unhealthy(
                    $"Outbox processor heartbeat is stale (last seen {heartbeatAge.TotalSeconds:F0}s ago).",
                    data: readOnlyData);
            }
        }

        // Evaluate critical thresholds
        if (readyPendingCount >= outboxOptions.CriticalPendingMessageCount)
        {
            return HealthCheckResult.Unhealthy(
                $"Outbox ready backlog is critical ({readyPendingCount} messages >= threshold {outboxOptions.CriticalPendingMessageCount}).",
                data: readOnlyData);
        }

        if (oldestReadyAge >= outboxOptions.CriticalOldestMessageAge)
        {
            return HealthCheckResult.Unhealthy(
                $"Outbox oldest ready message age is critical ({oldestReadyAge.TotalMinutes:F1}m >= threshold {outboxOptions.CriticalOldestMessageAge.TotalMinutes:F1}m).",
                data: readOnlyData);
        }

        if (abandonedCount >= outboxOptions.CriticalAbandonedMessageCount)
        {
            return HealthCheckResult.Unhealthy(
                $"Outbox abandoned message count is critical ({abandonedCount} messages >= threshold {outboxOptions.CriticalAbandonedMessageCount}).",
                data: readOnlyData);
        }

        // Evaluate warning thresholds
        if (readyPendingCount >= outboxOptions.WarningPendingMessageCount)
        {
            return HealthCheckResult.Degraded(
                $"Outbox ready backlog exceeds warning threshold ({readyPendingCount} messages >= threshold {outboxOptions.WarningPendingMessageCount}).",
                data: readOnlyData);
        }

        if (oldestReadyAge >= outboxOptions.WarningOldestMessageAge)
        {
            return HealthCheckResult.Degraded(
                $"Outbox oldest ready message age exceeds warning threshold ({oldestReadyAge.TotalMinutes:F1}m >= threshold {outboxOptions.WarningOldestMessageAge.TotalMinutes:F1}m).",
                data: readOnlyData);
        }

        if (abandonedCount >= outboxOptions.WarningAbandonedMessageCount)
        {
            return HealthCheckResult.Degraded(
                $"Outbox abandoned message count exceeds warning threshold ({abandonedCount} messages >= threshold {outboxOptions.WarningAbandonedMessageCount}).",
                data: readOnlyData);
        }

        return HealthCheckResult.Healthy(
            $"Outbox is healthy ({readyPendingCount} ready messages, {abandonedCount} abandoned messages, oldest ready age {oldestReadyAge.TotalSeconds:F0}s).",
            readOnlyData);
    }
}
