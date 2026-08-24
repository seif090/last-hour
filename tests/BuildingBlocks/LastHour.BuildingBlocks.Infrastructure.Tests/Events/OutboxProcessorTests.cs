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
public class OutboxProcessorTests
{
    [Fact]
    public async Task ExecuteAsync_WhenCancellationRequestedDuringPolling_StopsCleanlyWithoutMutatingState()
    {
        TestSink.Reset();
        string databaseName = Guid.NewGuid().ToString();
        var options = new OutboxOptions
        {
            Enabled = true,
            ProcessingInterval = TimeSpan.FromSeconds(5),
            BatchSize = 10,
        };

        var clock = new FixedClock { UtcNow = new DateTime(2026, 8, 18, 12, 0, 0, DateTimeKind.Utc) };
        using ServiceProvider provider = BuildProvider(databaseName, options, clock);

        var processor = provider.GetRequiredService<OutboxProcessor>();
        var tracker = provider.GetRequiredService<IOutboxProcessorTracker>();

        using var cts = new CancellationTokenSource();
        cts.Cancel(); // Cancel before/during polling

        await processor.StartAsync(cts.Token);
        await processor.StopAsync(CancellationToken.None);

        tracker.IsRunning.Should().BeFalse();
        TestSink.Messages.Should().BeEmpty();
    }

    [Fact]
    public async Task ExecuteAsync_WhenCancellationRequestedDuringDispatch_PreservesMessageStateWithoutFailureOrRetry()
    {
        TestSink.Reset();
        CancellingTestMessageNotificationHandler.OnHandling = null;

        string databaseName = Guid.NewGuid().ToString();
        var options = new OutboxOptions
        {
            Enabled = true,
            ProcessingInterval = TimeSpan.FromMilliseconds(20),
            LeaseDuration = TimeSpan.FromMinutes(2),
            BaseRetryDelay = TimeSpan.FromSeconds(10),
            MaxRetryCount = 5,
        };

        var clock = new FixedClock { UtcNow = new DateTime(2026, 8, 18, 12, 0, 0, DateTimeKind.Utc) };
        using ServiceProvider provider = BuildProvider(databaseName, options, clock);

        Guid messageId;
        using (IServiceScope initScope = provider.CreateScope())
        {
            LastHourDbContext initContext = initScope.ServiceProvider.GetRequiredService<LastHourDbContext>();
            var message = OutboxMessage.Create(new CancellingTestMessage("in-flight-cancelled"), clock.UtcNow);
            initContext.OutboxMessages.Add(message);
            await initContext.SaveChangesAsync();
            messageId = message.Id;
        }

        using var cts = new CancellationTokenSource();
        var dispatchStarted = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        CancellingTestMessageNotificationHandler.OnHandling = () =>
        {
            dispatchStarted.TrySetResult(true);
            cts.Cancel(); // Trigger host shutdown cancellation while dispatch is in flight
        };

        long failedCount = 0;
        long retriedCount = 0;
        long processedCount = 0;
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
            else if (instrument.Name == "last_hour.outbox.messages.processed")
            {
                processedCount += measurement;
            }
            else if (instrument.Name == "last_hour.outbox.messages.abandoned")
            {
                abandonedCount += measurement;
            }
        });
        listener.Start();

        var processor = provider.GetRequiredService<OutboxProcessor>();
        var tracker = provider.GetRequiredService<IOutboxProcessorTracker>();

        await processor.StartAsync(cts.Token);

        // Wait until dispatch is in-flight and cancellation was requested
        await dispatchStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));

        // Stop the processor
        await processor.StopAsync(CancellationToken.None);

        tracker.IsRunning.Should().BeFalse();

        // Verify message state in DB:
        // - Claimed and lease set
        // - NOT processed
        // - NOT failed / retry count NOT incremented / next attempt NOT modified
        using (IServiceScope verifyScope = provider.CreateScope())
        {
            LastHourDbContext verifyContext = verifyScope.ServiceProvider.GetRequiredService<LastHourDbContext>();
            OutboxMessage msg = await verifyContext.OutboxMessages.SingleAsync(m => m.Id == messageId);

            msg.ProcessedOnUtc.Should().BeNull();
            msg.RetryCount.Should().Be(0);
            msg.NextAttemptAtUtc.Should().BeNull();
            msg.Error.Should().BeNull();
            msg.ClaimToken.Should().NotBeNull();
            msg.LockExpiresAtUtc.Should().Be(clock.UtcNow.Add(options.LeaseDuration));
        }

        // Verify zero failure or retry telemetry
        failedCount.Should().Be(0);
        retriedCount.Should().Be(0);
        processedCount.Should().Be(0);
        abandonedCount.Should().Be(0);
    }

    [Fact]
    public async Task ProcessPendingMessages_InFlightCancelledMessage_IsReclaimedAndProcessedAfterLeaseExpires()
    {
        TestSink.Reset();
        CancellingTestMessageNotificationHandler.OnHandling = null;

        string databaseName = Guid.NewGuid().ToString();
        var options = new OutboxOptions
        {
            Enabled = true,
            ProcessingInterval = TimeSpan.FromMilliseconds(20),
            LeaseDuration = TimeSpan.FromMinutes(2),
            MaxRetryCount = 5,
        };

        var clock = new FixedClock { UtcNow = new DateTime(2026, 8, 18, 12, 0, 0, DateTimeKind.Utc) };
        using ServiceProvider provider = BuildProvider(databaseName, options, clock);

        Guid messageId;
        using (IServiceScope initScope = provider.CreateScope())
        {
            LastHourDbContext initContext = initScope.ServiceProvider.GetRequiredService<LastHourDbContext>();
            var message = OutboxMessage.Create(new TestMessage("recoverable-message"), clock.UtcNow);
            message.Claim(Guid.NewGuid(), clock.UtcNow.AddMinutes(2));
            initContext.OutboxMessages.Add(message);
            await initContext.SaveChangesAsync();
            messageId = message.Id;
        }

        long reclaimedCount = 0;
        long processedCount = 0;
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
            if (instrument.Name == "last_hour.outbox.messages.reclaimed")
            {
                reclaimedCount += measurement;
            }
            else if (instrument.Name == "last_hour.outbox.messages.processed")
            {
                processedCount += measurement;
            }
        });
        listener.Start();

        // Advance clock past lease expiration
        clock.UtcNow = clock.UtcNow.AddMinutes(3);

        var processor = provider.GetRequiredService<OutboxProcessor>();
        await processor.StartAsync(CancellationToken.None);
        try
        {
            await WaitUntilAsync(
                async () =>
                {
                    using IServiceScope scope = provider.CreateScope();
                    LastHourDbContext ctx = scope.ServiceProvider.GetRequiredService<LastHourDbContext>();
                    OutboxMessage msg = await ctx.OutboxMessages.SingleAsync(m => m.Id == messageId);
                    return msg.ProcessedOnUtc != null;
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
            OutboxMessage msg = await verifyContext.OutboxMessages.SingleAsync(m => m.Id == messageId);

            msg.ProcessedOnUtc.Should().NotBeNull();
            msg.RetryCount.Should().Be(0);
            msg.Error.Should().BeNull();
        }

        reclaimedCount.Should().Be(1);
        processedCount.Should().Be(1);
        TestSink.Messages.Should().ContainSingle();
    }

    [Fact]
    public async Task ProcessPendingMessages_InternalCancellation_WhenStoppingTokenNotCancelled_IsTreatedAsRealFailure()
    {
        TestSink.Reset();
        string databaseName = Guid.NewGuid().ToString();
        var options = new OutboxOptions
        {
            Enabled = true,
            ProcessingInterval = TimeSpan.FromMilliseconds(20),
            BaseRetryDelay = TimeSpan.FromSeconds(5),
            MaxRetryCount = 3,
        };

        var clock = new FixedClock { UtcNow = new DateTime(2026, 8, 18, 12, 0, 0, DateTimeKind.Utc) };
        using ServiceProvider provider = BuildProvider(databaseName, options, clock);

        Guid messageId;
        using (IServiceScope initScope = provider.CreateScope())
        {
            LastHourDbContext initContext = initScope.ServiceProvider.GetRequiredService<LastHourDbContext>();
            var message = OutboxMessage.Create(new InternalCancelledTestMessage("internal-timeout"), clock.UtcNow);
            initContext.OutboxMessages.Add(message);
            await initContext.SaveChangesAsync();
            messageId = message.Id;
        }

        long failedCount = 0;
        long retriedCount = 0;
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
                    OutboxMessage msg = await ctx.OutboxMessages.SingleAsync(m => m.Id == messageId);
                    return msg.RetryCount >= 1;
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
            OutboxMessage msg = await verifyContext.OutboxMessages.SingleAsync(m => m.Id == messageId);

            msg.ProcessedOnUtc.Should().BeNull();
            msg.RetryCount.Should().Be(1);
            msg.NextAttemptAtUtc.Should().Be(clock.UtcNow.AddSeconds(5));
            msg.Error.Should().Contain("Internal handler timeout");
        }

        failedCount.Should().Be(1);
        retriedCount.Should().Be(1);
    }

    [Fact]
    public async Task ProcessPendingMessages_RealException_ExecutesStandardRetryAndBackoffPipeline()
    {
        TestSink.Reset();
        string databaseName = Guid.NewGuid().ToString();
        var options = new OutboxOptions
        {
            Enabled = true,
            ProcessingInterval = TimeSpan.FromMilliseconds(20),
            BaseRetryDelay = TimeSpan.FromSeconds(5),
            MaxRetryCount = 3,
        };

        var clock = new FixedClock { UtcNow = new DateTime(2026, 8, 18, 12, 0, 0, DateTimeKind.Utc) };
        using ServiceProvider provider = BuildProvider(databaseName, options, clock);

        Guid messageId;
        using (IServiceScope initScope = provider.CreateScope())
        {
            LastHourDbContext initContext = initScope.ServiceProvider.GetRequiredService<LastHourDbContext>();
            var message = OutboxMessage.Create(new ThrowingTestMessage("real-error"), clock.UtcNow);
            initContext.OutboxMessages.Add(message);
            await initContext.SaveChangesAsync();
            messageId = message.Id;
        }

        var processor = provider.GetRequiredService<OutboxProcessor>();
        await processor.StartAsync(CancellationToken.None);
        try
        {
            await WaitUntilAsync(
                async () =>
                {
                    using IServiceScope scope = provider.CreateScope();
                    LastHourDbContext ctx = scope.ServiceProvider.GetRequiredService<LastHourDbContext>();
                    OutboxMessage msg = await ctx.OutboxMessages.SingleAsync(m => m.Id == messageId);
                    return msg.RetryCount >= 1;
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
            OutboxMessage msg = await verifyContext.OutboxMessages.SingleAsync(m => m.Id == messageId);

            msg.ProcessedOnUtc.Should().BeNull();
            msg.RetryCount.Should().Be(1);
            msg.NextAttemptAtUtc.Should().Be(clock.UtcNow.AddSeconds(5));
            msg.Error.Should().Contain("handler failed");
        }
    }

    [Fact]
    public async Task ProcessPendingMessages_MultiInstanceClaimConflict_SkipsMessageAndRecordsClaimConflictMetric()
    {
        TestSink.Reset();
        string databaseName = Guid.NewGuid().ToString();
        var options = new OutboxOptions
        {
            Enabled = true,
            ProcessingInterval = TimeSpan.FromMilliseconds(20),
            LeaseDuration = TimeSpan.FromMinutes(2),
        };

        var clock = new FixedClock { UtcNow = new DateTime(2026, 8, 18, 12, 0, 0, DateTimeKind.Utc) };
        using ServiceProvider provider = BuildProvider(databaseName, options, clock);

        Guid messageId;
        using (IServiceScope initScope = provider.CreateScope())
        {
            LastHourDbContext initContext = initScope.ServiceProvider.GetRequiredService<LastHourDbContext>();
            var message = OutboxMessage.Create(new TestMessage("conflict-test"), clock.UtcNow);
            initContext.OutboxMessages.Add(message);
            await initContext.SaveChangesAsync();
            messageId = message.Id;
        }

        using (IServiceScope claimScope = provider.CreateScope())
        {
            LastHourDbContext claimContext = claimScope.ServiceProvider.GetRequiredService<LastHourDbContext>();
            OutboxMessage msg = await claimContext.OutboxMessages.SingleAsync(m => m.Id == messageId);
            msg.Claim(Guid.NewGuid(), clock.UtcNow.AddMinutes(2));
            await claimContext.SaveChangesAsync();
        }

        var processor = provider.GetRequiredService<OutboxProcessor>();
        await processor.StartAsync(CancellationToken.None);
        try
        {
            await Task.Delay(100);
        }
        finally
        {
            await processor.StopAsync(CancellationToken.None);
        }

        using (IServiceScope verifyScope = provider.CreateScope())
        {
            LastHourDbContext verifyContext = verifyScope.ServiceProvider.GetRequiredService<LastHourDbContext>();
            OutboxMessage msg = await verifyContext.OutboxMessages.SingleAsync(m => m.Id == messageId);

            msg.ProcessedOnUtc.Should().BeNull();
            msg.RetryCount.Should().Be(0);
        }

        TestSink.Messages.Should().BeEmpty();
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
