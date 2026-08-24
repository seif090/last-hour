using System.Diagnostics.Metrics;
using FluentAssertions;
using LastHour.BuildingBlocks.Infrastructure.Persistence;
using LastHour.BuildingBlocks.Infrastructure.Persistence.Outbox;
using LastHour.BuildingBlocks.Infrastructure.Persistence.Outbox.Commands;
using LastHour.BuildingBlocks.Infrastructure.Tests.Persistence.TestSupport;
using LastHour.BuildingBlocks.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace LastHour.BuildingBlocks.Infrastructure.Tests.Persistence.Outbox;

[Collection("Events")]
public class RequeueOutboxMessageCommandHandlerTests
{
    [Fact]
    public async Task Handle_AbandonedMessage_ResetsStateAndEmitsRequeuedMetric()
    {
        string databaseName = Guid.NewGuid().ToString();
        var options = new DbContextOptionsBuilder<LastHourDbContext>()
            .UseInMemoryDatabase(databaseName)
            .Options;

        var clock = new FixedClock { UtcNow = new DateTime(2026, 8, 17, 14, 0, 0, DateTimeKind.Utc) };
        using var metrics = new OutboxMetrics();

        Guid messageId;
        using (var context = new LastHourDbContext(options))
        {
            var message = OutboxMessage.Create(new TestMessage("abandoned"));
            message.RecordFailure("fatal error", clock.UtcNow.AddMinutes(-10), 1, TimeSpan.Zero);
            context.OutboxMessages.Add(message);
            await context.SaveChangesAsync();
            messageId = message.Id;
        }

        long requeuedCount = 0;
        using var listener = new MeterListener();
        listener.InstrumentPublished = (instrument, meterListener) =>
        {
            if (instrument.Meter.Name == OutboxMetrics.MeterName)
            {
                meterListener.EnableMeasurementEvents(instrument);
            }
        };
        listener.SetMeasurementEventCallback<long>((instrument, measurement, tags, state) =>
        {
            if (instrument.Name == "last_hour.outbox.messages.requeued")
            {
                requeuedCount += measurement;
            }
        });
        listener.Start();

        using (var context = new LastHourDbContext(options))
        {
            var handler = new RequeueOutboxMessageCommandHandler(context, clock, metrics);
            Result result = await handler.Handle(new RequeueOutboxMessageCommand(messageId), CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
        }

        using (var context = new LastHourDbContext(options))
        {
            OutboxMessage msg = await context.OutboxMessages.SingleAsync(m => m.Id == messageId);
            msg.ProcessedOnUtc.Should().BeNull();
            msg.RetryCount.Should().Be(0);
            msg.Error.Should().BeNull();
            msg.ClaimToken.Should().BeNull();
            msg.LockExpiresAtUtc.Should().BeNull();
            msg.NextAttemptAtUtc.Should().Be(clock.UtcNow);
        }

        requeuedCount.Should().Be(1);
    }

    [Fact]
    public async Task Handle_NonExistentMessage_ReturnsNotFound()
    {
        string databaseName = Guid.NewGuid().ToString();
        var options = new DbContextOptionsBuilder<LastHourDbContext>()
            .UseInMemoryDatabase(databaseName)
            .Options;

        var clock = new FixedClock();
        using var metrics = new OutboxMetrics();

        using var context = new LastHourDbContext(options);
        var handler = new RequeueOutboxMessageCommandHandler(context, clock, metrics);
        Result result = await handler.Handle(new RequeueOutboxMessageCommand(Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.FirstError!.Type.Should().Be(ErrorType.NotFound);
        result.FirstError.Code.Should().Be("Outbox.MessageNotFound");
    }

    [Fact]
    public async Task Handle_PendingMessage_ReturnsConflict()
    {
        string databaseName = Guid.NewGuid().ToString();
        var options = new DbContextOptionsBuilder<LastHourDbContext>()
            .UseInMemoryDatabase(databaseName)
            .Options;

        var clock = new FixedClock();
        using var metrics = new OutboxMetrics();

        Guid messageId;
        using (var context = new LastHourDbContext(options))
        {
            var message = OutboxMessage.Create(new TestMessage("pending"));
            context.OutboxMessages.Add(message);
            await context.SaveChangesAsync();
            messageId = message.Id;
        }

        using (var context = new LastHourDbContext(options))
        {
            var handler = new RequeueOutboxMessageCommandHandler(context, clock, metrics);
            Result result = await handler.Handle(new RequeueOutboxMessageCommand(messageId), CancellationToken.None);

            result.IsFailure.Should().BeTrue();
            result.FirstError!.Type.Should().Be(ErrorType.Conflict);
            result.FirstError.Code.Should().Be("Outbox.MessageNotAbandoned");
        }
    }

    [Fact]
    public async Task Handle_SuccessfullyProcessedMessage_ReturnsConflict()
    {
        string databaseName = Guid.NewGuid().ToString();
        var options = new DbContextOptionsBuilder<LastHourDbContext>()
            .UseInMemoryDatabase(databaseName)
            .Options;

        var clock = new FixedClock();
        using var metrics = new OutboxMetrics();

        Guid messageId;
        using (var context = new LastHourDbContext(options))
        {
            var message = OutboxMessage.Create(new TestMessage("processed"));
            message.MarkProcessed(clock.UtcNow);
            context.OutboxMessages.Add(message);
            await context.SaveChangesAsync();
            messageId = message.Id;
        }

        using (var context = new LastHourDbContext(options))
        {
            var handler = new RequeueOutboxMessageCommandHandler(context, clock, metrics);
            Result result = await handler.Handle(new RequeueOutboxMessageCommand(messageId), CancellationToken.None);

            result.IsFailure.Should().BeTrue();
            result.FirstError!.Type.Should().Be(ErrorType.Conflict);
            result.FirstError.Code.Should().Be("Outbox.MessageNotAbandoned");
        }
    }
}
