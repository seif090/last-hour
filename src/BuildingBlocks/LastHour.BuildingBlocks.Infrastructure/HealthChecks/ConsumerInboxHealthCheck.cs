using System.Collections.ObjectModel;
using LastHour.BuildingBlocks.Application.Contracts;
using LastHour.BuildingBlocks.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace LastHour.BuildingBlocks.Infrastructure.HealthChecks;

/// <summary>
/// Evaluates the operational health of the consumer idempotency inbox by inspecting active and expired leases.
/// </summary>
public sealed class ConsumerInboxHealthCheck : IHealthCheck
{
    private readonly LastHourDbContext _dbContext;
    private readonly IClock _clock;
    private readonly IOptions<HealthChecksOptions> _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConsumerInboxHealthCheck"/> class.
    /// </summary>
    /// <param name="dbContext">The database context used to query inbox metrics.</param>
    /// <param name="clock">The system clock abstraction.</param>
    /// <param name="options">The health check options.</param>
    public ConsumerInboxHealthCheck(
        LastHourDbContext dbContext,
        IClock clock,
        IOptions<HealthChecksOptions> options)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    /// <inheritdoc/>
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        ConsumerInboxHealthCheckOptions inboxOptions = _options.Value.ConsumerInbox;
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

        int stalledExpiredCount = await _dbContext.ConsumerInboxMessages
            .AsNoTracking()
            .CountAsync(m => m.CompletedOnUtc == null && m.LockExpiresAtUtc != null && m.LockExpiresAtUtc < utcNow, cancellationToken)
            .ConfigureAwait(false);

        DateTime? oldestStalledLeaseExpiresAtUtc = null;
        if (stalledExpiredCount > 0)
        {
            oldestStalledLeaseExpiresAtUtc = await _dbContext.ConsumerInboxMessages
                .AsNoTracking()
                .Where(m => m.CompletedOnUtc == null && m.LockExpiresAtUtc != null && m.LockExpiresAtUtc < utcNow)
                .Select(m => m.LockExpiresAtUtc)
                .MinAsync(cancellationToken)
                .ConfigureAwait(false);
        }

        TimeSpan oldestStalledAge = oldestStalledLeaseExpiresAtUtc.HasValue && oldestStalledLeaseExpiresAtUtc.Value < utcNow
            ? utcNow - oldestStalledLeaseExpiresAtUtc.Value
            : TimeSpan.Zero;

        var data = new Dictionary<string, object>
        {
            ["total_inbox_count"] = totalCount,
            ["completed_count"] = completedCount,
            ["active_lease_count"] = activeLeaseCount,
            ["stalled_expired_lease_count"] = stalledExpiredCount,
            ["oldest_stalled_lease_age_seconds"] = oldestStalledAge.TotalSeconds,
        };

        var readOnlyData = new ReadOnlyDictionary<string, object>(data);

        if (stalledExpiredCount >= inboxOptions.CriticalStalledLeaseCount)
        {
            return HealthCheckResult.Unhealthy(
                $"Consumer inbox stalled expired lease count is critical ({stalledExpiredCount} >= threshold {inboxOptions.CriticalStalledLeaseCount}).",
                data: readOnlyData);
        }

        if (oldestStalledAge >= inboxOptions.CriticalOldestStalledAge)
        {
            return HealthCheckResult.Unhealthy(
                $"Consumer inbox oldest stalled lease age is critical ({oldestStalledAge.TotalMinutes:F1}m >= threshold {inboxOptions.CriticalOldestStalledAge.TotalMinutes:F1}m).",
                data: readOnlyData);
        }

        if (stalledExpiredCount >= inboxOptions.WarningStalledLeaseCount)
        {
            return HealthCheckResult.Degraded(
                $"Consumer inbox stalled expired lease count exceeds warning threshold ({stalledExpiredCount} >= threshold {inboxOptions.WarningStalledLeaseCount}).",
                data: readOnlyData);
        }

        if (oldestStalledAge >= inboxOptions.WarningOldestStalledAge)
        {
            return HealthCheckResult.Degraded(
                $"Consumer inbox oldest stalled lease age exceeds warning threshold ({oldestStalledAge.TotalMinutes:F1}m >= threshold {inboxOptions.WarningOldestStalledAge.TotalMinutes:F1}m).",
                data: readOnlyData);
        }

        return HealthCheckResult.Healthy(
            $"Consumer inbox is healthy ({activeLeaseCount} active leases, {stalledExpiredCount} stalled expired leases).",
            readOnlyData);
    }
}
