using System.Diagnostics.Metrics;
using FluentAssertions;
using LastHour.BuildingBlocks.Infrastructure.Persistence.Idempotency;

namespace LastHour.BuildingBlocks.Infrastructure.Tests.Persistence.Idempotency;

[Collection("Events")]
public class ConsumerIdempotencyMetricsTests
{
    [Fact]
    public void ActivitySource_HasCorrectNameAndVersion()
    {
        ConsumerIdempotencyMetrics.ActivitySource.Name.Should().Be(ConsumerIdempotencyMetrics.MeterName);
        ConsumerIdempotencyMetrics.ActivitySource.Version.Should().Be(ConsumerIdempotencyMetrics.MeterVersion);
    }

    [Fact]
    public void RecordProcessed_EmitsCounter_WithConsumerTag()
    {
        using var metrics = new ConsumerIdempotencyMetrics();
        long counterValue = 0;
        string? consumerTag = null;

        using var listener = new MeterListener();
        listener.InstrumentPublished = (instrument, meterListener) =>
        {
            if (instrument.Meter.Name == ConsumerIdempotencyMetrics.MeterName)
            {
                meterListener.EnableMeasurementEvents(instrument);
            }
        };

        listener.SetMeasurementEventCallback<long>((instrument, measurement, tags, _) =>
        {
            if (instrument.Name == "last_hour.consumer.processed")
            {
                counterValue += measurement;
                consumerTag = tags.ToArray().FirstOrDefault(t => t.Key == "consumer").Value?.ToString();
            }
        });

        listener.Start();

        metrics.RecordProcessed("Notifications.WelcomeEmail");

        counterValue.Should().Be(1);
        consumerTag.Should().Be("Notifications.WelcomeEmail");
    }

    [Fact]
    public void RecordProcessed_WithDuration_EmitsCounterAndHistogram()
    {
        using var metrics = new ConsumerIdempotencyMetrics();
        long counterValue = 0;
        double durationValue = 0;
        string? consumerTag = null;

        using var listener = new MeterListener();
        listener.InstrumentPublished = (instrument, meterListener) =>
        {
            if (instrument.Meter.Name == ConsumerIdempotencyMetrics.MeterName)
            {
                meterListener.EnableMeasurementEvents(instrument);
            }
        };

        listener.SetMeasurementEventCallback<long>((instrument, measurement, tags, _) =>
        {
            if (instrument.Name == "last_hour.consumer.processed")
            {
                counterValue += measurement;
                consumerTag = tags.ToArray().FirstOrDefault(t => t.Key == "consumer").Value?.ToString();
            }
        });

        listener.SetMeasurementEventCallback<double>((instrument, measurement, tags, _) =>
        {
            if (instrument.Name == "last_hour.consumer.processing_duration")
            {
                durationValue = measurement;
            }
        });

        listener.Start();

        metrics.RecordProcessed("Notifications.MerchantCreated", 125.5);

        counterValue.Should().Be(1);
        durationValue.Should().Be(125.5);
        consumerTag.Should().Be("Notifications.MerchantCreated");
    }

    [Fact]
    public void RecordDuplicate_EmitsCounter_WithConsumerTag()
    {
        using var metrics = new ConsumerIdempotencyMetrics();
        long counterValue = 0;
        string? consumerTag = null;

        using var listener = new MeterListener();
        listener.InstrumentPublished = (instrument, meterListener) =>
        {
            if (instrument.Meter.Name == ConsumerIdempotencyMetrics.MeterName)
            {
                meterListener.EnableMeasurementEvents(instrument);
            }
        };

        listener.SetMeasurementEventCallback<long>((instrument, measurement, tags, _) =>
        {
            if (instrument.Name == "last_hour.consumer.duplicate")
            {
                counterValue += measurement;
                consumerTag = tags.ToArray().FirstOrDefault(t => t.Key == "consumer").Value?.ToString();
            }
        });

        listener.Start();

        metrics.RecordDuplicate("Analytics.TrackEvent");

        counterValue.Should().Be(1);
        consumerTag.Should().Be("Analytics.TrackEvent");
    }

    [Fact]
    public void RecordFailed_EmitsCounter_WithConsumerAndFailureTypeTags()
    {
        using var metrics = new ConsumerIdempotencyMetrics();
        long counterValue = 0;
        string? consumerTag = null;
        string? failureTypeTag = null;

        using var listener = new MeterListener();
        listener.InstrumentPublished = (instrument, meterListener) =>
        {
            if (instrument.Meter.Name == ConsumerIdempotencyMetrics.MeterName)
            {
                meterListener.EnableMeasurementEvents(instrument);
            }
        };

        listener.SetMeasurementEventCallback<long>((instrument, measurement, tags, _) =>
        {
            if (instrument.Name == "last_hour.consumer.failed")
            {
                counterValue += measurement;
                var tagArray = tags.ToArray();
                consumerTag = tagArray.FirstOrDefault(t => t.Key == "consumer").Value?.ToString();
                failureTypeTag = tagArray.FirstOrDefault(t => t.Key == "failure_type").Value?.ToString();
            }
        });

        listener.Start();

        metrics.RecordFailed("Payments.Capture", "Database");

        counterValue.Should().Be(1);
        consumerTag.Should().Be("Payments.Capture");
        failureTypeTag.Should().Be("Database");
    }

    [Fact]
    public void RecordLeaseAcquired_EmitsCounter_WithConsumerTag()
    {
        using var metrics = new ConsumerIdempotencyMetrics();
        long counterValue = 0;
        string? consumerTag = null;

        using var listener = new MeterListener();
        listener.InstrumentPublished = (instrument, meterListener) =>
        {
            if (instrument.Meter.Name == ConsumerIdempotencyMetrics.MeterName)
            {
                meterListener.EnableMeasurementEvents(instrument);
            }
        };

        listener.SetMeasurementEventCallback<long>((instrument, measurement, tags, _) =>
        {
            if (instrument.Name == "last_hour.consumer.lease_acquired")
            {
                counterValue += measurement;
                consumerTag = tags.ToArray().FirstOrDefault(t => t.Key == "consumer").Value?.ToString();
            }
        });

        listener.Start();

        metrics.RecordLeaseAcquired("Audit.Log");

        counterValue.Should().Be(1);
        consumerTag.Should().Be("Audit.Log");
    }

    [Fact]
    public void RecordLeaseReclaimed_EmitsCounter_WithConsumerTag()
    {
        using var metrics = new ConsumerIdempotencyMetrics();
        long counterValue = 0;
        string? consumerTag = null;

        using var listener = new MeterListener();
        listener.InstrumentPublished = (instrument, meterListener) =>
        {
            if (instrument.Meter.Name == ConsumerIdempotencyMetrics.MeterName)
            {
                meterListener.EnableMeasurementEvents(instrument);
            }
        };

        listener.SetMeasurementEventCallback<long>((instrument, measurement, tags, _) =>
        {
            if (instrument.Name == "last_hour.consumer.lease_reclaimed")
            {
                counterValue += measurement;
                consumerTag = tags.ToArray().FirstOrDefault(t => t.Key == "consumer").Value?.ToString();
            }
        });

        listener.Start();

        metrics.RecordLeaseReclaimed("Orders.Fulfillment");

        counterValue.Should().Be(1);
        consumerTag.Should().Be("Orders.Fulfillment");
    }
}
