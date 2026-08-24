using System.Diagnostics.Metrics;
using FluentAssertions;
using LastHour.BuildingBlocks.Application.Contracts;
using LastHour.BuildingBlocks.Infrastructure.Persistence;
using LastHour.BuildingBlocks.Infrastructure.Persistence.Configuration;
using LastHour.BuildingBlocks.Infrastructure.Persistence.Outbox;
using LastHour.BuildingBlocks.Infrastructure.Tests.Persistence.TestSupport;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace LastHour.BuildingBlocks.Infrastructure.Tests.Events;

[Collection("Events")]
public class OutboxProcessorRetrySchedulingTests
{
    [Fact]
    public async Task ProcessPendingMessages_FailedMessage_SchedulesRetryWithBackoffAndEmitsRetriedMetric()
    {
        TestSink.Reset();
        string databaseName = Guid.NewGuid().ToString();
        var options = new OutboxOptions
        {
            Enabled = true,
            ProcessingInterval = TimeSpan.FromMilliseconds(50),
            BaseRetryDelay = TimeSpan.FromSeconds(5),
            MaxRetryCount = 5,
        };

        var clock = new FixedClock { UtcNow = new DateTime(2026, 8, 17, 12, 0, 0, DateTimeKind.Utc) };
        using ServiceProvider provider = BuildProvider(databaseName, options, clock);

        using (IServiceScope initScope = provider.CreateScope())
        {
            LastHourDbContext initContext = initScope.ServiceProvider.GetRequiredService<LastHourDbContext>();
            initContext.OutboxMessages.Add(OutboxMessage.Create(new ThrowingTestMessage("failing-msg"), clock.UtcNow));
            await initContext.SaveChangesAsync();
        }

        long failedCount = 0;
        long retriedCount = 0;
        long abandonedCount = 0;
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
            if (instrument.Name == "last_hour.outbox.messages.failed")
            {
                failedCount += measurement;
            }
            else if (instrument.Name == "last_hour.outbox.messages.retried")
            {
                retriedCount += measurement;
            }
            else if (instrument.Name == "last_hour.outbox.messages.abandoned")
            {
                abandonedCount += measurement;
            }
        });
        listener.Start();

        var processor = provider.GetRequiredService<OutboxProcessor>();
        await processor.StartAsync(CancellationToken.None);
        try
        {
            await WaitUntilAsync(
                async () =>
                {
                    using IServiceScope scope = provider.CreateScope();
                    LastHourDbContext ctx = scope.ServiceProvider.GetRequiredService<LastHourDbContext>();
                    OutboxMessage? msg = await ctx.OutboxMessages.FirstOrDefaultAsync();
                    return msg is not null && msg.RetryCount >= 1;
                },
                TimeSpan.FromSeconds(5));
        }
        finally
        {
            await processor.StopAsync(CancellationToken.None);
        }

        using (IServiceScope verifyScope = provider.CreateScope())
        {
            LastHourDbContext verifyContext = verifyScope.ServiceProvider.GetRequiredService<LastHourDbContext>();
            OutboxMessage msg = await verifyContext.OutboxMessages.SingleAsync();

            msg.RetryCount.Should().Be(1);
            msg.ProcessedOnUtc.Should().BeNull();
            msg.NextAttemptAtUtc.Should().Be(clock.UtcNow.AddSeconds(5));
            msg.ClaimToken.Should().BeNull();
            msg.LockExpiresAtUtc.Should().BeNull();
            msg.Error.Should().Contain("handler failed");
        }

        failedCount.Should().Be(1);
        retriedCount.Should().Be(1);
        abandonedCount.Should().Be(0);
    }

    [Fact]
    public async Task ProcessPendingMessages_FutureScheduledRetry_IsSkippedByProcessor()
    {
        TestSink.Reset();
        string databaseName = Guid.NewGuid().ToString();
        var options = new OutboxOptions
        {
            Enabled = true,
            ProcessingInterval = TimeSpan.FromMilliseconds(50),
        };

        var clock = new FixedClock { UtcNow = new DateTime(2026, 8, 17, 12, 0, 0, DateTimeKind.Utc) };
        using ServiceProvider provider = BuildProvider(databaseName, options, clock);

        using (IServiceScope initScope = provider.CreateScope())
        {
            LastHourDbContext initContext = initScope.ServiceProvider.GetRequiredService<LastHourDbContext>();
            var message = OutboxMessage.Create(new TestMessage("future-retry"), clock.UtcNow);
            message.RecordFailure("prior failure", clock.UtcNow, 5, TimeSpan.FromMinutes(10));
            initContext.OutboxMessages.Add(message);
            await initContext.SaveChangesAsync();
        }

        var processor = provider.GetRequiredService<OutboxProcessor>();
        await processor.StartAsync(CancellationToken.None);
        try
        {
            await Task.Delay(200);
        }
        finally
        {
            await processor.StopAsync(CancellationToken.None);
        }

        using (IServiceScope verifyScope = provider.CreateScope())
        {
            LastHourDbContext verifyContext = verifyScope.ServiceProvider.GetRequiredService<LastHourDbContext>();
            OutboxMessage msg = await verifyContext.OutboxMessages.SingleAsync();

            msg.ProcessedOnUtc.Should().BeNull();
            msg.RetryCount.Should().Be(1);
            msg.ClaimToken.Should().BeNull();
        }

        TestSink.Messages.Should().BeEmpty();
    }

    [Fact]
    public async Task ProcessPendingMessages_ScheduledRetryDue_IsProcessedWhenClockAdvanced()
    {
        TestSink.Reset();
        string databaseName = Guid.NewGuid().ToString();
        var options = new OutboxOptions
        {
            Enabled = true,
            ProcessingInterval = TimeSpan.FromMilliseconds(50),
        };

        var clock = new FixedClock { UtcNow = new DateTime(2026, 8, 17, 12, 0, 0, DateTimeKind.Utc) };
        using ServiceProvider provider = BuildProvider(databaseName, options, clock);

        using (IServiceScope initScope = provider.CreateScope())
        {
            LastHourDbContext initContext = initScope.ServiceProvider.GetRequiredService<LastHourDbContext>();
            var message = OutboxMessage.Create(new TestMessage("due-retry"), clock.UtcNow);
            message.RecordFailure("prior failure", clock.UtcNow, 5, TimeSpan.FromSeconds(10));
            initContext.OutboxMessages.Add(message);
            await initContext.SaveChangesAsync();
        }

        // Advance clock past NextAttemptAtUtc
        clock.UtcNow = clock.UtcNow.AddSeconds(15);

        var processor = provider.GetRequiredService<OutboxProcessor>();
        await processor.StartAsync(CancellationToken.None);
        try
        {
            await WaitUntilAsync(
                async () =>
                {
                    using IServiceScope scope = provider.CreateScope();
                    LastHourDbContext ctx = scope.ServiceProvider.GetRequiredService<LastHourDbContext>();
                    OutboxMessage? msg = await ctx.OutboxMessages.FirstOrDefaultAsync();
                    return msg is not null && msg.ProcessedOnUtc != null;
                },
                TimeSpan.FromSeconds(5));
        }
        finally
        {
            await processor.StopAsync(CancellationToken.None);
        }

        using (IServiceScope verifyScope = provider.CreateScope())
        {
            LastHourDbContext verifyContext = verifyScope.ServiceProvider.GetRequiredService<LastHourDbContext>();
            OutboxMessage msg = await verifyContext.OutboxMessages.SingleAsync();

            msg.ProcessedOnUtc.Should().NotBeNull();
            msg.NextAttemptAtUtc.Should().BeNull();
            msg.Error.Should().BeNull();
        }

        TestSink.Messages.Should().ContainSingle();
    }

    [Fact]
    public async Task ProcessPendingMessages_PoisonMessage_AbandonedAtMaxRetryCount()
    {
        TestSink.Reset();
        string databaseName = Guid.NewGuid().ToString();
        var options = new OutboxOptions
        {
            Enabled = true,
            ProcessingInterval = TimeSpan.FromMilliseconds(50),
            BaseRetryDelay = TimeSpan.FromSeconds(1),
            MaxRetryCount = 2,
        };

        var clock = new FixedClock { UtcNow = new DateTime(2026, 8, 17, 12, 0, 0, DateTimeKind.Utc) };
        using ServiceProvider provider = BuildProvider(databaseName, options, clock);

        using (IServiceScope initScope = provider.CreateScope())
        {
            LastHourDbContext initContext = initScope.ServiceProvider.GetRequiredService<LastHourDbContext>();
            // Message that already failed once and is scheduled for now
            var message = OutboxMessage.Create(new ThrowingTestMessage("poison-msg"), clock.UtcNow);
            message.RecordFailure("first failure", clock.UtcNow, 2, TimeSpan.Zero);
            initContext.OutboxMessages.Add(message);
            await initContext.SaveChangesAsync();
        }

        long abandonedCount = 0;
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
            if (instrument.Name == "last_hour.outbox.messages.abandoned")
            {
                abandonedCount += measurement;
            }
        });
        listener.Start();

        var processor = provider.GetRequiredService<OutboxProcessor>();
        await processor.StartAsync(CancellationToken.None);
        try
        {
            await WaitUntilAsync(
                async () =>
                {
                    using IServiceScope scope = provider.CreateScope();
                    LastHourDbContext ctx = scope.ServiceProvider.GetRequiredService<LastHourDbContext>();
                    OutboxMessage? msg = await ctx.OutboxMessages.FirstOrDefaultAsync();
                    return msg is not null && msg.ProcessedOnUtc != null;
                },
                TimeSpan.FromSeconds(5));
        }
        finally
        {
            await processor.StopAsync(CancellationToken.None);
        }

        using (IServiceScope verifyScope = provider.CreateScope())
        {
            LastHourDbContext verifyContext = verifyScope.ServiceProvider.GetRequiredService<LastHourDbContext>();
            OutboxMessage msg = await verifyContext.OutboxMessages.SingleAsync();

            msg.RetryCount.Should().Be(2);
            msg.ProcessedOnUtc.Should().NotBeNull();
            msg.NextAttemptAtUtc.Should().BeNull();
            msg.CanRequeue.Should().BeTrue();
            msg.Error.Should().Contain("handler failed");
        }

        abandonedCount.Should().Be(1);
    }

    private static ServiceProvider BuildProvider(string databaseName, OutboxOptions options, IClock clock)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMediatR(config => config.RegisterServicesFromAssembly(typeof(TestMessageNotificationHandler).Assembly));
        services.AddDbContext<LastHourDbContext>(builder => builder.UseInMemoryDatabase(databaseName));
        services.AddSingleton(clock);
        services.AddSingleton<OutboxMetrics>();
        services.AddSingleton<IOutboxProcessorTracker, OutboxProcessorTracker>();
        services.AddSingleton(Options.Create(options));
        services.AddSingleton<OutboxProcessor>();
        return services.BuildServiceProvider();
    }

    private static async Task<bool> WaitUntilAsync(Func<Task<bool>> condition, TimeSpan timeout)
    {
        DateTime deadline = DateTime.UtcNow.Add(timeout);
        while (DateTime.UtcNow < deadline)
        {
            if (await condition())
            {
                return true;
            }

            await Task.Delay(50);
        }

        return await condition();
    }
}
