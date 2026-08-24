using FluentAssertions;
using LastHour.BuildingBlocks.Application.Models;
using LastHour.BuildingBlocks.Infrastructure.Persistence;
using LastHour.BuildingBlocks.Infrastructure.Persistence.Outbox;
using LastHour.BuildingBlocks.Infrastructure.Persistence.Outbox.Queries;
using LastHour.BuildingBlocks.Infrastructure.Tests.Persistence.TestSupport;
using LastHour.BuildingBlocks.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace LastHour.BuildingBlocks.Infrastructure.Tests.Persistence.Outbox;

public class GetOutboxMessagesQueryHandlerTests
{
    private static DbContextOptions<LastHourDbContext> CreateOptions()
        => new DbContextOptionsBuilder<LastHourDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

    [Fact]
    public async Task Handle_EmptyDatabase_ReturnsEmptyPagedResult()
    {
        var options = CreateOptions();
        var clock = new FixedClock();

        using var context = new LastHourDbContext(options);
        var handler = new GetOutboxMessagesQueryHandler(context, clock);

        Result<PagedResult<OutboxMessageSummaryDto>> result = await handler.Handle(
            new GetOutboxMessagesQuery(),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().BeEmpty();
        result.Value.TotalCount.Should().Be(0);
        result.Value.TotalPages.Should().Be(0);
        result.Value.PageNumber.Should().Be(1);
        result.Value.PageSize.Should().Be(20);
    }

    [Fact]
    public async Task Handle_Pagination_ReturnsRequestedPartition()
    {
        var options = CreateOptions();
        DateTime baseDate = new DateTime(2026, 8, 17, 10, 0, 0, DateTimeKind.Utc);
        var clock = new FixedClock { UtcNow = baseDate.AddHours(5) };

        using (var context = new LastHourDbContext(options))
        {
            for (int i = 1; i <= 12; i++)
            {
                context.OutboxMessages.Add(OutboxMessage.Create(new TestMessage($"msg-{i}"), baseDate.AddMinutes(i)));
            }

            await context.SaveChangesAsync();
        }

        using (var context = new LastHourDbContext(options))
        {
            var handler = new GetOutboxMessagesQueryHandler(context, clock);
            Result<PagedResult<OutboxMessageSummaryDto>> result = await handler.Handle(
                new GetOutboxMessagesQuery(pageNumber: 2, pageSize: 5),
                CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Value.TotalCount.Should().Be(12);
            result.Value.TotalPages.Should().Be(3);
            result.Value.PageNumber.Should().Be(2);
            result.Value.PageSize.Should().Be(5);
            result.Value.Items.Should().HaveCount(5);

            // Default order is OccurredOnUtc DESC: items are 12 down to 1.
            // Page 1: 12, 11, 10, 9, 8
            // Page 2: 7, 6, 5, 4, 3
            result.Value.Items[0].OccurredOnUtc.Should().Be(baseDate.AddMinutes(7));
            result.Value.Items[4].OccurredOnUtc.Should().Be(baseDate.AddMinutes(3));
        }
    }

    [Fact]
    public async Task Handle_StatusFilters_ReturnExpectedSubsets()
    {
        var options = CreateOptions();
        DateTime now = new DateTime(2026, 8, 17, 12, 0, 0, DateTimeKind.Utc);
        var clock = new FixedClock { UtcNow = now };

        using (var context = new LastHourDbContext(options))
        {
            // 1. Pending (unclaimed, not scheduled in future)
            var pending = OutboxMessage.Create(new TestMessage("pending"), now.AddMinutes(-10));
            context.OutboxMessages.Add(pending);

            // 2. Processing (claimed with active lease)
            var processing = OutboxMessage.Create(new TestMessage("processing"), now.AddMinutes(-8));
            processing.Claim(Guid.NewGuid(), now.AddMinutes(2));
            context.OutboxMessages.Add(processing);

            // 3. Scheduled (future retry)
            var scheduled = OutboxMessage.Create(new TestMessage("scheduled"), now.AddMinutes(-6));
            scheduled.RecordFailure("temporary error", now.AddMinutes(-6), 5, TimeSpan.FromMinutes(10));
            context.OutboxMessages.Add(scheduled);

            // 4. Processed (completed without error)
            var processed = OutboxMessage.Create(new TestMessage("processed"), now.AddMinutes(-4));
            processed.MarkProcessed(now.AddMinutes(-3));
            context.OutboxMessages.Add(processed);

            // 5. Abandoned (exhausted retries with error)
            var abandoned = OutboxMessage.Create(new TestMessage("abandoned"), now.AddMinutes(-2));
            abandoned.RecordFailure("fatal error", now.AddMinutes(-2), 1, TimeSpan.Zero);
            context.OutboxMessages.Add(abandoned);

            await context.SaveChangesAsync();
        }

        using (var context = new LastHourDbContext(options))
        {
            var handler = new GetOutboxMessagesQueryHandler(context, clock);

            // Test Pending
            var pendingRes = await handler.Handle(new GetOutboxMessagesQuery(status: OutboxMessageStatus.Pending), CancellationToken.None);
            pendingRes.Value.Items.Should().ContainSingle();
            pendingRes.Value.Items[0].Status.Should().Be(OutboxMessageStatus.Pending);

            // Test Processing
            var processingRes = await handler.Handle(new GetOutboxMessagesQuery(status: OutboxMessageStatus.Processing), CancellationToken.None);
            processingRes.Value.Items.Should().ContainSingle();
            processingRes.Value.Items[0].Status.Should().Be(OutboxMessageStatus.Processing);

            // Test Scheduled
            var scheduledRes = await handler.Handle(new GetOutboxMessagesQuery(status: OutboxMessageStatus.Scheduled), CancellationToken.None);
            scheduledRes.Value.Items.Should().ContainSingle();
            scheduledRes.Value.Items[0].Status.Should().Be(OutboxMessageStatus.Scheduled);

            // Test Processed
            var processedRes = await handler.Handle(new GetOutboxMessagesQuery(status: OutboxMessageStatus.Processed), CancellationToken.None);
            processedRes.Value.Items.Should().ContainSingle();
            processedRes.Value.Items[0].Status.Should().Be(OutboxMessageStatus.Processed);

            // Test Abandoned
            var abandonedRes = await handler.Handle(new GetOutboxMessagesQuery(status: OutboxMessageStatus.Abandoned), CancellationToken.None);
            abandonedRes.Value.Items.Should().ContainSingle();
            abandonedRes.Value.Items[0].Status.Should().Be(OutboxMessageStatus.Abandoned);
        }
    }

    [Fact]
    public async Task Handle_ScheduledStatus_OrdersByNextAttemptAscending()
    {
        var options = CreateOptions();
        DateTime now = new DateTime(2026, 8, 17, 12, 0, 0, DateTimeKind.Utc);
        var clock = new FixedClock { UtcNow = now };

        using (var context = new LastHourDbContext(options))
        {
            var sched1 = OutboxMessage.Create(new TestMessage("s1"), now.AddMinutes(-5));
            sched1.RecordFailure("err", now, 5, TimeSpan.FromMinutes(20)); // NextAttempt = 12:20

            var sched2 = OutboxMessage.Create(new TestMessage("s2"), now.AddMinutes(-10));
            sched2.RecordFailure("err", now, 5, TimeSpan.FromMinutes(5)); // NextAttempt = 12:05

            var sched3 = OutboxMessage.Create(new TestMessage("s3"), now.AddMinutes(-2));
            sched3.RecordFailure("err", now, 5, TimeSpan.FromMinutes(10)); // NextAttempt = 12:10

            context.OutboxMessages.AddRange(sched1, sched2, sched3);
            await context.SaveChangesAsync();
        }

        using (var context = new LastHourDbContext(options))
        {
            var handler = new GetOutboxMessagesQueryHandler(context, clock);
            var result = await handler.Handle(new GetOutboxMessagesQuery(status: OutboxMessageStatus.Scheduled), CancellationToken.None);

            result.Value.Items.Should().HaveCount(3);
            result.Value.Items[0].NextAttemptAtUtc.Should().Be(now.AddMinutes(5));
            result.Value.Items[1].NextAttemptAtUtc.Should().Be(now.AddMinutes(10));
            result.Value.Items[2].NextAttemptAtUtc.Should().Be(now.AddMinutes(20));
        }
    }

    private sealed record AnotherTestMessage(string Text);

    [Fact]
    public async Task Handle_EventTypeAndDateRangeFilters_ApplyCorrectly()
    {
        var options = CreateOptions();
        DateTime now = new DateTime(2026, 8, 17, 12, 0, 0, DateTimeKind.Utc);
        var clock = new FixedClock { UtcNow = now };

        using (var context = new LastHourDbContext(options))
        {
            context.OutboxMessages.Add(OutboxMessage.Create(new TestMessage("m1"), now.AddHours(-3)));
            context.OutboxMessages.Add(OutboxMessage.Create(new TestMessage("m2"), now.AddHours(-2)));
            context.OutboxMessages.Add(OutboxMessage.Create(new AnotherTestMessage("r1"), now.AddHours(-2)));
            context.OutboxMessages.Add(OutboxMessage.Create(new TestMessage("m3"), now.AddHours(-1)));

            await context.SaveChangesAsync();
        }

        using (var context = new LastHourDbContext(options))
        {
            var handler = new GetOutboxMessagesQueryHandler(context, clock);

            // Filter by event type
            var typeResult = await handler.Handle(new GetOutboxMessagesQuery(eventType: "AnotherTestMessage"), CancellationToken.None);
            typeResult.Value.Items.Should().ContainSingle();
            typeResult.Value.Items[0].EventType.Should().Contain("AnotherTestMessage");

            // Filter by date range
            var dateResult = await handler.Handle(
                new GetOutboxMessagesQuery(
                    occurredFromUtc: now.AddHours(-2).AddMinutes(-10),
                    occurredToUtc: now.AddHours(-1).AddMinutes(-30)),
                CancellationToken.None);

            dateResult.Value.Items.Should().HaveCount(2);
        }
    }

    [Fact]
    public void OutboxMessageSummaryDto_DoesNotContainContentProperty()
    {
        var property = typeof(OutboxMessageSummaryDto).GetProperty("Content");
        property.Should().BeNull();
    }
}
