using FluentAssertions;
using LastHour.BuildingBlocks.Application.Contracts;
using LastHour.BuildingBlocks.Infrastructure.Observability.Diagnostics;
using LastHour.BuildingBlocks.Infrastructure.Persistence;
using LastHour.BuildingBlocks.Infrastructure.Persistence.Idempotency;
using LastHour.BuildingBlocks.Infrastructure.Persistence.Outbox;
using LastHour.BuildingBlocks.Infrastructure.Tests.Persistence.TestSupport;
using Microsoft.EntityFrameworkCore;

namespace LastHour.BuildingBlocks.Infrastructure.Tests.Observability;

public class EventProcessingDiagnosticsTests
{
    private readonly FixedClock _clock = new() { UtcNow = new DateTime(2026, 8, 19, 12, 0, 0, DateTimeKind.Utc) };

    [Fact]
    public async Task GetOutboxSnapshotAsync_ReturnsAccurateMetrics()
    {
        using LastHourDbContext dbContext = CreateDbContext();
        var tracker = new OutboxProcessorTracker();
        tracker.SetRunning(true);
        tracker.RecordHeartbeat(_clock.UtcNow);
        tracker.RecordSuccess(_clock.UtcNow);

        // 1 pending ready message
        dbContext.OutboxMessages.Add(OutboxMessage.Create(new TestMessage("ready-1")));

        // 1 active processing message (lease in future)
        var activeMessage = OutboxMessage.Create(new TestMessage("active-1"));
        activeMessage.Claim(Guid.NewGuid(), _clock.UtcNow.AddMinutes(2));
        dbContext.OutboxMessages.Add(activeMessage);

        // 1 abandoned message
        var abandonedMessage = OutboxMessage.Create(new TestMessage("abandoned-1"));
        abandonedMessage.RecordFailure("poison", _clock.UtcNow, 1, TimeSpan.Zero);
        dbContext.OutboxMessages.Add(abandonedMessage);

        await dbContext.SaveChangesAsync();

        var diagnostics = new EventProcessingDiagnostics(dbContext, tracker, _clock);

        OutboxDiagnosticsSnapshot snapshot = await diagnostics.GetOutboxSnapshotAsync();

        snapshot.TotalPendingCount.Should().Be(2);
        snapshot.ReadyPendingCount.Should().Be(1);
        snapshot.ActiveProcessingCount.Should().Be(1);
        snapshot.AbandonedCount.Should().Be(1);
        snapshot.ProcessorIsRunning.Should().BeTrue();
        snapshot.LastHeartbeatUtc.Should().Be(_clock.UtcNow);
    }

    [Fact]
    public async Task GetConsumerInboxSnapshotAsync_ReturnsAccurateMetrics()
    {
        using LastHourDbContext dbContext = CreateDbContext();
        var tracker = new OutboxProcessorTracker();

        // 1 completed message
        var completed = ConsumerInboxMessage.Create(Guid.NewGuid(), "Consumer.Done", _clock.UtcNow.AddMinutes(-10), TimeSpan.FromMinutes(2));
        completed.Complete(_clock.UtcNow.AddMinutes(-9));
        dbContext.ConsumerInboxMessages.Add(completed);

        // 1 active lease
        var active = ConsumerInboxMessage.Create(Guid.NewGuid(), "Consumer.Active", _clock.UtcNow, TimeSpan.FromMinutes(2));
        dbContext.ConsumerInboxMessages.Add(active);

        // 1 expired lease
        var expired = ConsumerInboxMessage.Create(Guid.NewGuid(), "Consumer.Expired", _clock.UtcNow.AddMinutes(-10), TimeSpan.FromMinutes(2));
        dbContext.ConsumerInboxMessages.Add(expired);

        await dbContext.SaveChangesAsync();

        var diagnostics = new EventProcessingDiagnostics(dbContext, tracker, _clock);

        ConsumerInboxDiagnosticsSnapshot snapshot = await diagnostics.GetConsumerInboxSnapshotAsync();

        snapshot.TotalInboxCount.Should().Be(3);
        snapshot.CompletedCount.Should().Be(1);
        snapshot.ActiveLeaseCount.Should().Be(1);
        snapshot.ExpiredLeaseCount.Should().Be(1);
    }

    private static LastHourDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<LastHourDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new LastHourDbContext(options);
    }

    private sealed class FixedClock : IClock
    {
        public DateTime UtcNow { get; set; }
    }
}
