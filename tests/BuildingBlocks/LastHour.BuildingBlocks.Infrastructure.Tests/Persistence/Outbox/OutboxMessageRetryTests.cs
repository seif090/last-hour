using LastHour.BuildingBlocks.Infrastructure.Persistence.Outbox;
using LastHour.BuildingBlocks.Infrastructure.Tests.Persistence.TestSupport;

namespace LastHour.BuildingBlocks.Infrastructure.Tests.Persistence.Outbox;

public class OutboxMessageRetryTests
{
    [Fact]
    public void InitialState_NextAttemptAtUtcIsNull_AndCanRequeueIsFalse()
    {
        var message = OutboxMessage.Create(new TestMessage("hello"));

        Assert.Null(message.NextAttemptAtUtc);
        Assert.False(message.CanRequeue);
        Assert.Equal(0, message.RetryCount);
        Assert.Null(message.ProcessedOnUtc);
        Assert.Null(message.Error);
        Assert.Null(message.ClaimToken);
        Assert.Null(message.LockExpiresAtUtc);
    }

    [Fact]
    public void RecordFailure_BelowMaxRetryCount_SchedulesNextAttemptAndClearsClaim()
    {
        var message = OutboxMessage.Create(new TestMessage("hello"));
        message.Claim(Guid.NewGuid(), DateTime.UtcNow.AddMinutes(1));
        DateTime failedOnUtc = new DateTime(2026, 8, 17, 10, 0, 0, DateTimeKind.Utc);
        TimeSpan retryDelay = TimeSpan.FromSeconds(5);

        message.RecordFailure("Network timeout", failedOnUtc, 5, retryDelay);

        Assert.Equal(1, message.RetryCount);
        Assert.Equal("Network timeout", message.Error);
        Assert.Equal(failedOnUtc.Add(retryDelay), message.NextAttemptAtUtc);
        Assert.Null(message.ProcessedOnUtc);
        Assert.Null(message.ClaimToken);
        Assert.Null(message.LockExpiresAtUtc);
        Assert.False(message.CanRequeue);
    }

    [Fact]
    public void RecordFailure_ExhaustingMaxRetries_MarksProcessedAndClearsNextAttempt()
    {
        var message = OutboxMessage.Create(new TestMessage("hello"));
        DateTime failedOnUtc = new DateTime(2026, 8, 17, 10, 0, 0, DateTimeKind.Utc);

        message.RecordFailure("Fail 1", failedOnUtc, 2, TimeSpan.FromSeconds(5));
        Assert.Null(message.ProcessedOnUtc);
        Assert.NotNull(message.NextAttemptAtUtc);
        Assert.False(message.CanRequeue);

        message.RecordFailure("Fail 2", failedOnUtc.AddSeconds(10), 2, TimeSpan.FromSeconds(10));
        Assert.Equal(2, message.RetryCount);
        Assert.Equal("Fail 2", message.Error);
        Assert.Equal(failedOnUtc.AddSeconds(10), message.ProcessedOnUtc);
        Assert.Null(message.NextAttemptAtUtc);
        Assert.Null(message.ClaimToken);
        Assert.Null(message.LockExpiresAtUtc);
        Assert.True(message.CanRequeue);
    }

    [Fact]
    public void MarkProcessed_ClearsNextAttemptAtUtcAndError()
    {
        var message = OutboxMessage.Create(new TestMessage("hello"));
        DateTime failedOnUtc = new DateTime(2026, 8, 17, 10, 0, 0, DateTimeKind.Utc);
        message.RecordFailure("Temporary error", failedOnUtc, 5, TimeSpan.FromSeconds(5));

        DateTime completedOnUtc = new DateTime(2026, 8, 17, 10, 0, 10, DateTimeKind.Utc);
        message.MarkProcessed(completedOnUtc);

        Assert.Equal(completedOnUtc, message.ProcessedOnUtc);
        Assert.Null(message.NextAttemptAtUtc);
        Assert.Null(message.Error);
        Assert.Null(message.ClaimToken);
        Assert.Null(message.LockExpiresAtUtc);
        Assert.False(message.CanRequeue);
    }

    [Fact]
    public void Requeue_NotAbandoned_ThrowsInvalidOperationException()
    {
        var pendingMessage = OutboxMessage.Create(new TestMessage("hello"));
        Assert.Throws<InvalidOperationException>(() => pendingMessage.Requeue(DateTime.UtcNow));

        var processedMessage = OutboxMessage.Create(new TestMessage("hello"));
        processedMessage.MarkProcessed(DateTime.UtcNow);
        Assert.Throws<InvalidOperationException>(() => processedMessage.Requeue(DateTime.UtcNow));
    }

    [Fact]
    public void Requeue_AbandonedMessage_ResetsStateAndPreservesIdentity()
    {
        var message = OutboxMessage.Create(new TestMessage("payload"));
        Guid originalId = message.Id;
        string originalType = message.Type;
        string originalContent = message.Content;
        DateTime originalOccurredOnUtc = message.OccurredOnUtc;

        DateTime failedOnUtc = new DateTime(2026, 8, 17, 10, 0, 0, DateTimeKind.Utc);
        message.RecordFailure("Fatal error", failedOnUtc, 1, TimeSpan.FromSeconds(5));
        Assert.True(message.CanRequeue);

        DateTime requeuedOnUtc = new DateTime(2026, 8, 17, 11, 0, 0, DateTimeKind.Utc);
        message.Requeue(requeuedOnUtc);

        Assert.Equal(originalId, message.Id);
        Assert.Equal(originalType, message.Type);
        Assert.Equal(originalContent, message.Content);
        Assert.Equal(originalOccurredOnUtc, message.OccurredOnUtc);
        Assert.Null(message.ProcessedOnUtc);
        Assert.Equal(0, message.RetryCount);
        Assert.Null(message.Error);
        Assert.Null(message.ClaimToken);
        Assert.Null(message.LockExpiresAtUtc);
        Assert.Equal(requeuedOnUtc, message.NextAttemptAtUtc);
        Assert.False(message.CanRequeue);
    }
}
