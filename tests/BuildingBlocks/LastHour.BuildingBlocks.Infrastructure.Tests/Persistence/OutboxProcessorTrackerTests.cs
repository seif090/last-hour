using LastHour.BuildingBlocks.Infrastructure.Persistence.Outbox;

namespace LastHour.BuildingBlocks.Infrastructure.Tests.Persistence;

public class OutboxProcessorTrackerTests
{
    [Fact]
    public void Tracker_InitialState_AllPropertiesNullOrFalse()
    {
        var tracker = new OutboxProcessorTracker();

        Assert.Null(tracker.LastHeartbeatUtc);
        Assert.Null(tracker.LastSuccessUtc);
        Assert.Null(tracker.LastFailureUtc);
        Assert.False(tracker.IsRunning);
    }

    [Fact]
    public void RecordHeartbeat_UpdatesLastHeartbeatUtc()
    {
        var tracker = new OutboxProcessorTracker();
        var now = new DateTime(2026, 8, 16, 20, 0, 0, DateTimeKind.Utc);

        tracker.RecordHeartbeat(now);

        Assert.Equal(now, tracker.LastHeartbeatUtc);
    }

    [Fact]
    public void RecordSuccess_UpdatesLastSuccessUtc()
    {
        var tracker = new OutboxProcessorTracker();
        var now = new DateTime(2026, 8, 16, 20, 0, 0, DateTimeKind.Utc);

        tracker.RecordSuccess(now);

        Assert.Equal(now, tracker.LastSuccessUtc);
    }

    [Fact]
    public void RecordFailure_UpdatesLastFailureUtc()
    {
        var tracker = new OutboxProcessorTracker();
        var now = new DateTime(2026, 8, 16, 20, 0, 0, DateTimeKind.Utc);

        tracker.RecordFailure(now);

        Assert.Equal(now, tracker.LastFailureUtc);
    }

    [Fact]
    public void SetRunning_UpdatesIsRunning()
    {
        var tracker = new OutboxProcessorTracker();

        tracker.SetRunning(true);
        Assert.True(tracker.IsRunning);

        tracker.SetRunning(false);
        Assert.False(tracker.IsRunning);
    }

    [Fact]
    public async Task Tracker_ThreadSafety_ConcurrentUpdatesDoNotThrow()
    {
        var tracker = new OutboxProcessorTracker();
        var tasks = new List<Task>();

        for (int i = 0; i < 20; i++)
        {
            int index = i;
            tasks.Add(Task.Run(() =>
            {
                var time = new DateTime(2026, 8, 16, 20, index, 0, DateTimeKind.Utc);
                tracker.RecordHeartbeat(time);
                tracker.RecordSuccess(time);
                tracker.RecordFailure(time);
                tracker.SetRunning(index % 2 == 0);
            }));
        }

        await Task.WhenAll(tasks);

        Assert.NotNull(tracker.LastHeartbeatUtc);
        Assert.NotNull(tracker.LastSuccessUtc);
        Assert.NotNull(tracker.LastFailureUtc);
    }
}
