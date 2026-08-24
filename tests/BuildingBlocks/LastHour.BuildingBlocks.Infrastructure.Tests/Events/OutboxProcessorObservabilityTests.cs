using System.Diagnostics;
using System.Diagnostics.Metrics;
using FluentAssertions;
using LastHour.BuildingBlocks.Application.Contracts;
using LastHour.BuildingBlocks.Infrastructure.Persistence;
using LastHour.BuildingBlocks.Infrastructure.Persistence.Configuration;
using LastHour.BuildingBlocks.Infrastructure.Persistence.Outbox;
using LastHour.BuildingBlocks.Infrastructure.Tests.Persistence.TestSupport;
using LastHour.BuildingBlocks.Infrastructure.Time;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace LastHour.BuildingBlocks.Infrastructure.Tests.Events;

[Collection("Events")]
public class OutboxProcessorObservabilityTests
{
    [Fact]
    public async Task ProcessPendingMessages_SuccessfulMessage_UpdatesTrackerAndEmitsProcessedMetric()
    {
        TestSink.Reset();
        string databaseName = Guid.NewGuid().ToString();
        var options = new OutboxOptions { Enabled = true, ProcessingInterval = TimeSpan.FromSeconds(1) };
        using ServiceProvider provider = BuildProvider(databaseName, options);

        using (IServiceScope initScope = provider.CreateScope())
        {
            LastHourDbContext initContext = initScope.ServiceProvider.GetRequiredService<LastHourDbContext>();
            initContext.OutboxMessages.Add(OutboxMessage.Create(new TestMessage("observability-test")));
            await initContext.SaveChangesAsync();
        }

        long processedCount = 0;
        double durationRecorded = 0;
        double deliveryLatencyRecorded = 0;
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
            if (instrument.Name == "last_hour.outbox.messages.processed")
            {
                processedCount += measurement;
            }
        });
        listener.SetMeasurementEventCallback<double>((instrument, measurement, tags, state) =>
        {
            if (instrument.Name == "last_hour.outbox.processing.duration")
            {
                durationRecorded = measurement;
            }
            else if (instrument.Name == "last_hour.outbox.delivery.latency")
            {
                deliveryLatencyRecorded = measurement;
            }
        });
        listener.Start();

        Activity? capturedActivity = null;
        using var activityListener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == OutboxMetrics.MeterName,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStopped = act => capturedActivity = act,
        };
        ActivitySource.AddActivityListener(activityListener);

        var processor = provider.GetRequiredService<OutboxProcessor>();
        var tracker = provider.GetRequiredService<IOutboxProcessorTracker>();

        await processor.StartAsync(CancellationToken.None);
        try
        {
            await WaitUntilAsync(
                async () =>
                {
                    using IServiceScope scope = provider.CreateScope();
                    LastHourDbContext ctx = scope.ServiceProvider.GetRequiredService<LastHourDbContext>();
                    return await ctx.OutboxMessages.AllAsync(m => m.ProcessedOnUtc != null);
                },
                TimeSpan.FromSeconds(5));
        }
        finally
        {
            await processor.StopAsync(CancellationToken.None);
        }

        tracker.LastHeartbeatUtc.Should().NotBeNull();
        tracker.LastSuccessUtc.Should().NotBeNull();
        processedCount.Should().Be(1);
        durationRecorded.Should().BeGreaterThanOrEqualTo(0);
        deliveryLatencyRecorded.Should().BeGreaterThanOrEqualTo(0);

        capturedActivity.Should().NotBeNull();
        capturedActivity!.OperationName.Should().Be("Outbox.Dispatch");
        capturedActivity.Kind.Should().Be(ActivityKind.Consumer);
        capturedActivity.Status.Should().Be(ActivityStatusCode.Ok);
        capturedActivity.GetTagItem("outbox.message_id").Should().NotBeNull();
        capturedActivity.GetTagItem("outbox.event_type").Should().NotBeNull();
        capturedActivity.GetTagItem("outbox.retry_count").Should().Be(0);
    }

    [Fact]
    public async Task ProcessPendingMessages_FailedMessage_EmitsFailureMetricAndUpdatesFailureTimestamp()
    {
        TestSink.Reset();
        string databaseName = Guid.NewGuid().ToString();
        var options = new OutboxOptions { Enabled = true, ProcessingInterval = TimeSpan.FromSeconds(1), MaxRetryCount = 5 };
        using ServiceProvider provider = BuildProvider(databaseName, options);

        using (IServiceScope initScope = provider.CreateScope())
        {
            LastHourDbContext initContext = initScope.ServiceProvider.GetRequiredService<LastHourDbContext>();
            initContext.OutboxMessages.Add(OutboxMessage.Create(new ThrowingTestMessage("failed-msg")));
            await initContext.SaveChangesAsync();
        }

        long failedCount = 0;
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
                    OutboxMessage msg = await ctx.OutboxMessages.SingleAsync();
                    return msg.RetryCount >= 1;
                },
                TimeSpan.FromSeconds(5));
        }
        finally
        {
            await processor.StopAsync(CancellationToken.None);
        }

        failedCount.Should().BeGreaterThanOrEqualTo(1);
    }

    [Fact]
    public async Task ProcessPendingMessages_PoisonMessage_EmitsAbandonedMetricOnlyWhenMaxRetriesReached()
    {
        TestSink.Reset();
        string databaseName = Guid.NewGuid().ToString();
        var options = new OutboxOptions { Enabled = true, ProcessingInterval = TimeSpan.FromSeconds(1), MaxRetryCount = 1 };
        using ServiceProvider provider = BuildProvider(databaseName, options);

        using (IServiceScope initScope = provider.CreateScope())
        {
            LastHourDbContext initContext = initScope.ServiceProvider.GetRequiredService<LastHourDbContext>();
            initContext.OutboxMessages.Add(OutboxMessage.Create(new ThrowingTestMessage("poison-msg")));
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
                    OutboxMessage msg = await ctx.OutboxMessages.SingleAsync();
                    return msg.ProcessedOnUtc != null && msg.RetryCount >= 1;
                },
                TimeSpan.FromSeconds(5));
        }
        finally
        {
            await processor.StopAsync(CancellationToken.None);
        }

        abandonedCount.Should().Be(1);
    }

    [Fact]
    public async Task ProcessPendingMessages_ExpiredLease_EmitsReclaimedMetric()
    {
        TestSink.Reset();
        string databaseName = Guid.NewGuid().ToString();
        var options = new OutboxOptions { Enabled = true, ProcessingInterval = TimeSpan.FromSeconds(1) };
        using ServiceProvider provider = BuildProvider(databaseName, options);

        using (IServiceScope initScope = provider.CreateScope())
        {
            LastHourDbContext initContext = initScope.ServiceProvider.GetRequiredService<LastHourDbContext>();
            var message = OutboxMessage.Create(new TestMessage("reclaimed-msg"));
            message.Claim(Guid.NewGuid(), DateTime.UtcNow.AddMinutes(-5)); // Expired lease
            initContext.OutboxMessages.Add(message);
            await initContext.SaveChangesAsync();
        }

        long reclaimedCount = 0;
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
                    OutboxMessage msg = await ctx.OutboxMessages.SingleAsync();
                    return msg.ProcessedOnUtc != null;
                },
                TimeSpan.FromSeconds(5));
        }
        finally
        {
            await processor.StopAsync(CancellationToken.None);
        }

        reclaimedCount.Should().Be(1);
    }

    [Fact]
    public async Task ProcessPendingMessages_CancellationDuringDispatch_DoesNotMarkActivityAsErrorAndEmitsNoFailureMetrics()
    {
        TestSink.Reset();
        CancellingTestMessageNotificationHandler.OnHandling = null;

        string databaseName = Guid.NewGuid().ToString();
        var options = new OutboxOptions { Enabled = true, ProcessingInterval = TimeSpan.FromMilliseconds(20) };
        using ServiceProvider provider = BuildProvider(databaseName, options);

        using (IServiceScope initScope = provider.CreateScope())
        {
            LastHourDbContext initContext = initScope.ServiceProvider.GetRequiredService<LastHourDbContext>();
            initContext.OutboxMessages.Add(OutboxMessage.Create(new CancellingTestMessage("obs-cancel-msg")));
            await initContext.SaveChangesAsync();
        }

        using var cts = new CancellationTokenSource();
        var dispatchStarted = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        CancellingTestMessageNotificationHandler.OnHandling = () =>
        {
            dispatchStarted.TrySetResult(true);
            cts.Cancel(); // Shutdown cancellation during dispatch
        };

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

        Activity? capturedActivity = null;
        using var activityListener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == OutboxMetrics.MeterName,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStopped = act => capturedActivity = act,
        };
        ActivitySource.AddActivityListener(activityListener);

        var processor = provider.GetRequiredService<OutboxProcessor>();
        await processor.StartAsync(cts.Token);

        await dispatchStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await processor.StopAsync(CancellationToken.None);

        failedCount.Should().Be(0);
        retriedCount.Should().Be(0);

        capturedActivity.Should().NotBeNull();
        capturedActivity!.OperationName.Should().Be("Outbox.Dispatch");
        capturedActivity.Status.Should().NotBe(ActivityStatusCode.Error);
    }

    [Fact]
    public async Task ProcessPendingMessages_InternalCancellation_MarksActivityAsErrorAndEmitsFailureMetrics()
    {
        TestSink.Reset();
        string databaseName = Guid.NewGuid().ToString();
        var options = new OutboxOptions { Enabled = true, ProcessingInterval = TimeSpan.FromMilliseconds(20), MaxRetryCount = 3 };
        using ServiceProvider provider = BuildProvider(databaseName, options);

        using (IServiceScope initScope = provider.CreateScope())
        {
            LastHourDbContext initContext = initScope.ServiceProvider.GetRequiredService<LastHourDbContext>();
            initContext.OutboxMessages.Add(OutboxMessage.Create(new InternalCancelledTestMessage("internal-obs-msg")));
            await initContext.SaveChangesAsync();
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

        Activity? capturedActivity = null;
        using var activityListener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == OutboxMetrics.MeterName,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStopped = act => capturedActivity = act,
        };
        ActivitySource.AddActivityListener(activityListener);

        var processor = provider.GetRequiredService<OutboxProcessor>();
        await processor.StartAsync(CancellationToken.None);
        try
        {
            await WaitUntilAsync(
                async () =>
                {
                    using IServiceScope scope = provider.CreateScope();
                    LastHourDbContext ctx = scope.ServiceProvider.GetRequiredService<LastHourDbContext>();
                    OutboxMessage msg = await ctx.OutboxMessages.SingleAsync();
                    return msg.RetryCount >= 1;
                },
                TimeSpan.FromSeconds(5));
        }
        finally
        {
            await processor.StopAsync(CancellationToken.None);
        }

        failedCount.Should().BeGreaterThanOrEqualTo(1);
        retriedCount.Should().BeGreaterThanOrEqualTo(1);

        capturedActivity.Should().NotBeNull();
        capturedActivity!.OperationName.Should().Be("Outbox.Dispatch");
        capturedActivity.Status.Should().Be(ActivityStatusCode.Error);
        capturedActivity.StatusDescription.Should().Contain("Internal handler timeout");
    }

    private static ServiceProvider BuildProvider(string databaseName, OutboxOptions options)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMediatR(config => config.RegisterServicesFromAssembly(typeof(TestMessageNotificationHandler).Assembly));
        services.AddDbContext<LastHourDbContext>(builder => builder.UseInMemoryDatabase(databaseName));
        services.AddSingleton<IClock, SystemClock>();
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
