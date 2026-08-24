using FluentAssertions;
using LastHour.BuildingBlocks.Application.Contracts;
using LastHour.BuildingBlocks.Infrastructure.HealthChecks;
using LastHour.BuildingBlocks.Infrastructure.Persistence;
using LastHour.BuildingBlocks.Infrastructure.Persistence.Outbox;
using LastHour.BuildingBlocks.Infrastructure.Tests.Persistence.TestSupport;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace LastHour.BuildingBlocks.Infrastructure.Tests.HealthChecks;

public class OutboxHealthCheckTests
{
    private readonly HealthChecksOptions _options;
    private readonly OutboxProcessorTracker _tracker;

    public OutboxHealthCheckTests()
    {
        _options = new HealthChecksOptions
        {
            Outbox = new OutboxHealthCheckOptions
            {
                WarningPendingMessageCount = 5,
                CriticalPendingMessageCount = 10,
                WarningOldestMessageAge = TimeSpan.FromMinutes(2),
                CriticalOldestMessageAge = TimeSpan.FromMinutes(5),
                ProcessorHeartbeatTimeout = TimeSpan.FromMinutes(1),
            },
        };
        _tracker = new OutboxProcessorTracker();
    }

    [Fact]
    public async Task CheckHealthAsync_HealthyQueueAndFreshHeartbeat_ReturnsHealthy()
    {
        var now = new DateTime(2026, 8, 16, 20, 0, 0, DateTimeKind.Utc);
        var clock = new TestClock(now);
        _tracker.SetRunning(true);
        _tracker.RecordHeartbeat(now.AddSeconds(-10));

        using LastHourDbContext context = CreateDbContext();
        context.OutboxMessages.Add(CreateMessage(now.AddSeconds(-30)));
        await context.SaveChangesAsync();

        var healthCheck = new OutboxHealthCheck(context, _tracker, clock, Options.Create(_options));
        HealthCheckResult result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        result.Status.Should().Be(HealthStatus.Healthy);
        result.Data["pending_message_count"].Should().Be(1);
        result.Data["total_pending"].Should().Be(1);
        result.Data["ready_pending_count"].Should().Be(1);
        result.Data["scheduled_retry_count"].Should().Be(0);
        result.Data["active_processing_count"].Should().Be(0);
        result.Data["oldest_ready_age_seconds"].Should().Be(30.0);
    }

    [Fact]
    public async Task CheckHealthAsync_NoReadyMessages_ResultsInOldestReadyAgeZero()
    {
        var now = new DateTime(2026, 8, 16, 20, 0, 0, DateTimeKind.Utc);
        var clock = new TestClock(now);
        _tracker.SetRunning(true);
        _tracker.RecordHeartbeat(now.AddSeconds(-5));

        using LastHourDbContext context = CreateDbContext();
        for (int i = 0; i < 3; i++)
        {
            var msg = CreateMessage(now.AddMinutes(-10));
            msg.RecordFailure("temporary", now, 5, TimeSpan.FromMinutes(5)); // NextAttempt in future
            context.OutboxMessages.Add(msg);
        }
        await context.SaveChangesAsync();

        var healthCheck = new OutboxHealthCheck(context, _tracker, clock, Options.Create(_options));
        HealthCheckResult result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        result.Data["ready_pending_count"].Should().Be(0);
        result.Data["scheduled_retry_count"].Should().Be(3);
        result.Data["oldest_ready_age_seconds"].Should().Be(0.0);
        ((double)result.Data["oldest_pending_age_seconds"]).Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task CheckHealthAsync_ReadyAndScheduledCoexist_OldestReadyAgeOnlyReflectsOldestReady()
    {
        var now = new DateTime(2026, 8, 16, 20, 0, 0, DateTimeKind.Utc);
        var clock = new TestClock(now);
        _tracker.SetRunning(true);
        _tracker.RecordHeartbeat(now.AddSeconds(-5));

        using LastHourDbContext context = CreateDbContext();

        // 1 very old scheduled message (20 minutes old)
        var scheduledMsg = CreateMessage(now.AddMinutes(-20));
        scheduledMsg.RecordFailure("temporary", now, 5, TimeSpan.FromMinutes(5)); // NextAttempt = now + 5m
        context.OutboxMessages.Add(scheduledMsg);

        // 1 ready message (2 minutes old)
        var readyMsg = CreateMessage(now.AddMinutes(-2));
        context.OutboxMessages.Add(readyMsg);

        await context.SaveChangesAsync();

        var customOptions = new HealthChecksOptions
        {
            Outbox = new OutboxHealthCheckOptions
            {
                WarningOldestMessageAge = TimeSpan.FromMinutes(6),
                CriticalOldestMessageAge = TimeSpan.FromMinutes(15),
            },
        };

        var healthCheck = new OutboxHealthCheck(context, _tracker, clock, Options.Create(customOptions));
        HealthCheckResult result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        result.Data["ready_pending_count"].Should().Be(1);
        result.Data["scheduled_retry_count"].Should().Be(1);
        result.Data["oldest_ready_age_seconds"].Should().Be(120.0); // Exactly 2 minutes (120s)
        result.Data["oldest_pending_age_seconds"].Should().Be(1200.0); // Exactly 20 minutes (1200s)
        result.Status.Should().Be(HealthStatus.Healthy); // Ready age (2m) is within WarningOldestMessageAge (6m)
    }

    [Fact]
    public async Task CheckHealthAsync_100ScheduledRetryMessagesWith0Ready_ReturnsHealthy()
    {
        var now = new DateTime(2026, 8, 16, 20, 0, 0, DateTimeKind.Utc);
        var clock = new TestClock(now);
        _tracker.SetRunning(true);
        _tracker.RecordHeartbeat(now.AddSeconds(-5));

        using LastHourDbContext context = CreateDbContext();
        for (int i = 0; i < 100; i++)
        {
            var msg = CreateMessage(now.AddMinutes(-10)); // Occurred 10 min ago
            msg.RecordFailure("transient downstream error", now, 5, TimeSpan.FromMinutes(4)); // NextAttempt = now + 4m
            context.OutboxMessages.Add(msg);
        }
        await context.SaveChangesAsync();

        var healthCheck = new OutboxHealthCheck(context, _tracker, clock, Options.Create(_options));
        HealthCheckResult result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        result.Status.Should().Be(HealthStatus.Healthy);
        result.Data["total_pending"].Should().Be(100);
        result.Data["ready_pending_count"].Should().Be(0);
        result.Data["scheduled_retry_count"].Should().Be(100);
        result.Data["active_processing_count"].Should().Be(0);
        result.Data["abandoned_message_count"].Should().Be(0);
        result.Data["oldest_ready_age_seconds"].Should().Be(0.0);
    }

    [Fact]
    public async Task CheckHealthAsync_ReadyPendingCountAtOrAboveWarning_ReturnsDegraded()
    {
        var now = new DateTime(2026, 8, 16, 20, 0, 0, DateTimeKind.Utc);
        var clock = new TestClock(now);
        _tracker.SetRunning(true);
        _tracker.RecordHeartbeat(now.AddSeconds(-5));

        using LastHourDbContext context = CreateDbContext();
        for (int i = 0; i < 6; i++) // WarningPendingMessageCount = 5
        {
            context.OutboxMessages.Add(CreateMessage(now.AddSeconds(-10)));
        }
        await context.SaveChangesAsync();

        var healthCheck = new OutboxHealthCheck(context, _tracker, clock, Options.Create(_options));
        HealthCheckResult result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        result.Status.Should().Be(HealthStatus.Degraded);
        result.Data["ready_pending_count"].Should().Be(6);
    }

    [Fact]
    public async Task CheckHealthAsync_ReadyPendingCountAtOrAboveCritical_ReturnsUnhealthy()
    {
        var now = new DateTime(2026, 8, 16, 20, 0, 0, DateTimeKind.Utc);
        var clock = new TestClock(now);
        _tracker.SetRunning(true);
        _tracker.RecordHeartbeat(now.AddSeconds(-5));

        using LastHourDbContext context = CreateDbContext();
        for (int i = 0; i < 11; i++) // CriticalPendingMessageCount = 10
        {
            context.OutboxMessages.Add(CreateMessage(now.AddSeconds(-10)));
        }
        await context.SaveChangesAsync();

        var healthCheck = new OutboxHealthCheck(context, _tracker, clock, Options.Create(_options));
        HealthCheckResult result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        result.Status.Should().Be(HealthStatus.Unhealthy);
        result.Data["ready_pending_count"].Should().Be(11);
    }

    [Fact]
    public async Task CheckHealthAsync_OldestReadyAgeAtOrAboveWarning_ReturnsDegraded()
    {
        var now = new DateTime(2026, 8, 16, 20, 0, 0, DateTimeKind.Utc);
        var clock = new TestClock(now);
        _tracker.SetRunning(true);
        _tracker.RecordHeartbeat(now.AddSeconds(-5));

        using LastHourDbContext context = CreateDbContext();
        context.OutboxMessages.Add(CreateMessage(now.AddMinutes(-3))); // WarningOldestMessageAge = 2m
        await context.SaveChangesAsync();

        var healthCheck = new OutboxHealthCheck(context, _tracker, clock, Options.Create(_options));
        HealthCheckResult result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        result.Status.Should().Be(HealthStatus.Degraded);
        result.Data["oldest_ready_age_seconds"].Should().Be(180.0);
    }

    [Fact]
    public async Task CheckHealthAsync_OldestReadyAgeAtOrAboveCritical_ReturnsUnhealthy()
    {
        var now = new DateTime(2026, 8, 16, 20, 0, 0, DateTimeKind.Utc);
        var clock = new TestClock(now);
        _tracker.SetRunning(true);
        _tracker.RecordHeartbeat(now.AddSeconds(-5));

        using LastHourDbContext context = CreateDbContext();
        context.OutboxMessages.Add(CreateMessage(now.AddMinutes(-6))); // CriticalOldestMessageAge = 5m
        await context.SaveChangesAsync();

        var healthCheck = new OutboxHealthCheck(context, _tracker, clock, Options.Create(_options));
        HealthCheckResult result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        result.Status.Should().Be(HealthStatus.Unhealthy);
        result.Data["oldest_ready_age_seconds"].Should().Be(360.0);
    }

    [Fact]
    public async Task CheckHealthAsync_ExpiredLeaseWithNextAttemptInPast_IsClassifiedAsReady()
    {
        var now = new DateTime(2026, 8, 16, 20, 0, 0, DateTimeKind.Utc);
        var clock = new TestClock(now);
        _tracker.SetRunning(true);
        _tracker.RecordHeartbeat(now.AddSeconds(-5));

        using LastHourDbContext context = CreateDbContext();
        var msg = CreateMessage(now.AddMinutes(-10));
        msg.RecordFailure("past error", now.AddMinutes(-2), 5, TimeSpan.Zero); // NextAttempt = now - 2m <= now, RetryCount = 1 < 5
        msg.Claim(Guid.NewGuid(), now.AddMinutes(-1)); // Lease expired
        context.OutboxMessages.Add(msg);
        await context.SaveChangesAsync();

        var healthCheck = new OutboxHealthCheck(context, _tracker, clock, Options.Create(_options));
        HealthCheckResult result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        result.Data["ready_pending_count"].Should().Be(1);
        result.Data["scheduled_retry_count"].Should().Be(0);
        result.Data["active_processing_count"].Should().Be(0);
    }

    [Fact]
    public async Task CheckHealthAsync_ExpiredLeaseWithNextAttemptInFuture_IsClassifiedAsScheduled()
    {
        var now = new DateTime(2026, 8, 16, 20, 0, 0, DateTimeKind.Utc);
        var clock = new TestClock(now);
        _tracker.SetRunning(true);
        _tracker.RecordHeartbeat(now.AddSeconds(-5));

        using LastHourDbContext context = CreateDbContext();
        var msg = CreateMessage(now.AddMinutes(-10));
        msg.RecordFailure("past error", now, 5, TimeSpan.FromMinutes(5)); // NextAttempt = now + 5m > now, RetryCount = 1 < 5
        msg.Claim(Guid.NewGuid(), now.AddMinutes(-1)); // Lease expired
        context.OutboxMessages.Add(msg);
        await context.SaveChangesAsync();

        var healthCheck = new OutboxHealthCheck(context, _tracker, clock, Options.Create(_options));
        HealthCheckResult result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        result.Data["ready_pending_count"].Should().Be(0);
        result.Data["scheduled_retry_count"].Should().Be(1);
        result.Data["active_processing_count"].Should().Be(0);
    }

    [Fact]
    public async Task CheckHealthAsync_DetailedQueueBreakdown_IsInternallyConsistent()
    {
        var now = new DateTime(2026, 8, 16, 20, 0, 0, DateTimeKind.Utc);
        var clock = new TestClock(now);
        _tracker.SetRunning(true);
        _tracker.RecordHeartbeat(now.AddSeconds(-5));

        using LastHourDbContext context = CreateDbContext();

        // 2 Ready Pending
        context.OutboxMessages.Add(CreateMessage(now.AddSeconds(-30)));
        context.OutboxMessages.Add(CreateMessage(now.AddSeconds(-20)));

        // 3 Scheduled Retry
        for (int i = 0; i < 3; i++)
        {
            var msg = CreateMessage(now.AddSeconds(-10));
            msg.RecordFailure("temporary", now, 5, TimeSpan.FromMinutes(5)); // NextAttempt = now + 5 min
            context.OutboxMessages.Add(msg);
        }

        // 4 Active Processing (lock in future)
        for (int i = 0; i < 4; i++)
        {
            var msg = CreateMessage(now.AddSeconds(-10));
            msg.Claim(Guid.NewGuid(), now.AddMinutes(2));
            context.OutboxMessages.Add(msg);
        }

        // 1 Expired Lease (lock in past -> ready pending)
        var expiredMsg = CreateMessage(now.AddSeconds(-15));
        expiredMsg.Claim(Guid.NewGuid(), now.AddMinutes(-1));
        context.OutboxMessages.Add(expiredMsg);

        // 2 Abandoned
        for (int i = 0; i < 2; i++)
        {
            var msg = CreateMessage(now.AddSeconds(-10));
            msg.RecordFailure("fatal", now, 1, TimeSpan.Zero);
            context.OutboxMessages.Add(msg);
        }

        await context.SaveChangesAsync();

        var customOptions = new HealthChecksOptions
        {
            Outbox = new OutboxHealthCheckOptions
            {
                WarningPendingMessageCount = 50,
                CriticalPendingMessageCount = 100,
            },
        };

        var healthCheck = new OutboxHealthCheck(context, _tracker, clock, Options.Create(customOptions));
        HealthCheckResult result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        int totalPending = (int)result.Data["total_pending"];
        int readyPending = (int)result.Data["ready_pending_count"];
        int scheduledRetry = (int)result.Data["scheduled_retry_count"];
        int activeProcessing = (int)result.Data["active_processing_count"];
        int abandoned = (int)result.Data["abandoned_message_count"];

        totalPending.Should().Be(10);
        readyPending.Should().Be(3); // 2 fresh + 1 expired lease
        scheduledRetry.Should().Be(3);
        activeProcessing.Should().Be(4);
        abandoned.Should().Be(2);

        // Verify mathematical consistency
        totalPending.Should().Be(readyPending + scheduledRetry + activeProcessing);
    }

    [Fact]
    public async Task CheckHealthAsync_StaleHeartbeat_ReturnsUnhealthy()
    {
        var now = new DateTime(2026, 8, 16, 20, 0, 0, DateTimeKind.Utc);
        var clock = new TestClock(now);
        _tracker.SetRunning(true);
        _tracker.RecordHeartbeat(now.AddMinutes(-2)); // Stale > 1 min timeout

        using LastHourDbContext context = CreateDbContext();
        var healthCheck = new OutboxHealthCheck(context, _tracker, clock, Options.Create(_options));
        HealthCheckResult result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        result.Status.Should().Be(HealthStatus.Unhealthy);
        result.Description.Should().Contain("stale");
    }

    [Fact]
    public async Task CheckHealthAsync_InitialStartupBeforeHeartbeat_ReturnsHealthy()
    {
        var now = new DateTime(2026, 8, 16, 20, 0, 0, DateTimeKind.Utc);
        var clock = new TestClock(now);
        // Tracker is not yet running or no heartbeat recorded yet
        using LastHourDbContext context = CreateDbContext();
        var healthCheck = new OutboxHealthCheck(context, _tracker, clock, Options.Create(_options));
        HealthCheckResult result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        result.Status.Should().Be(HealthStatus.Healthy);
    }

    [Fact]
    public async Task CheckHealthAsync_AbandonedCountAtWarningThreshold_ReturnsDegraded()
    {
        var now = new DateTime(2026, 8, 16, 20, 0, 0, DateTimeKind.Utc);
        var clock = new TestClock(now);
        _tracker.SetRunning(true);
        _tracker.RecordHeartbeat(now.AddSeconds(-5));

        using LastHourDbContext context = CreateDbContext();
        for (int i = 0; i < _options.Outbox.WarningAbandonedMessageCount; i++)
        {
            var msg = CreateMessage(now.AddMinutes(-1));
            msg.RecordFailure("poison", now.AddMinutes(-1), 1, TimeSpan.Zero);
            context.OutboxMessages.Add(msg);
        }

        await context.SaveChangesAsync();

        var healthCheck = new OutboxHealthCheck(context, _tracker, clock, Options.Create(_options));
        HealthCheckResult result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        result.Status.Should().Be(HealthStatus.Degraded);
        result.Data["abandoned_message_count"].Should().Be(_options.Outbox.WarningAbandonedMessageCount);
    }

    [Fact]
    public async Task CheckHealthAsync_AbandonedCountAtCriticalThreshold_ReturnsUnhealthy()
    {
        var now = new DateTime(2026, 8, 16, 20, 0, 0, DateTimeKind.Utc);
        var clock = new TestClock(now);
        _tracker.SetRunning(true);
        _tracker.RecordHeartbeat(now.AddSeconds(-5));

        using LastHourDbContext context = CreateDbContext();
        for (int i = 0; i < _options.Outbox.CriticalAbandonedMessageCount; i++)
        {
            var msg = CreateMessage(now.AddMinutes(-1));
            msg.RecordFailure("poison", now.AddMinutes(-1), 1, TimeSpan.Zero);
            context.OutboxMessages.Add(msg);
        }

        await context.SaveChangesAsync();

        var healthCheck = new OutboxHealthCheck(context, _tracker, clock, Options.Create(_options));
        HealthCheckResult result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        result.Status.Should().Be(HealthStatus.Unhealthy);
        result.Data["abandoned_message_count"].Should().Be(_options.Outbox.CriticalAbandonedMessageCount);
    }

    private static LastHourDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<LastHourDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new LastHourDbContext(options);
    }

    private static OutboxMessage CreateMessage(DateTime occurredOnUtc)
    {
        return OutboxMessage.Create(new TestMessage("content"), occurredOnUtc);
    }

    private sealed class TestClock : IClock
    {
        public TestClock(DateTime utcNow) => UtcNow = utcNow;
        public DateTime UtcNow { get; }
    }
}
