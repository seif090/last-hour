using System.Diagnostics.Metrics;
using FluentAssertions;
using LastHour.BuildingBlocks.Infrastructure.Persistence.Outbox;

namespace LastHour.BuildingBlocks.Infrastructure.Tests.Persistence;

[Collection("Events")]
public class OutboxMetricsTests
{
    [Fact]
    public void ActivitySource_HasCorrectNameAndVersion()
    {
        OutboxMetrics.ActivitySource.Name.Should().Be(OutboxMetrics.MeterName);
        OutboxMetrics.ActivitySource.Version.Should().Be(OutboxMetrics.MeterVersion);
    }

    [Fact]
    public void RecordDeliveryLatency_EmitsHistogram_WithEventTypeTag()
    {
        using var metrics = new OutboxMetrics();
        double histogramValue = 0;
        string? eventTypeTag = null;

        using var listener = new MeterListener();
        listener.InstrumentPublished = (instrument, meterListener) =>
        {
            if (instrument.Meter.Name == OutboxMetrics.MeterName)
            {
                meterListener.EnableMeasurementEvents(instrument);
            }
        };

        listener.SetMeasurementEventCallback<double>((instrument, measurement, tags, state) =>
        {
            if (instrument.Name == "last_hour.outbox.delivery.latency")
            {
                histogramValue = measurement;
                eventTypeTag = tags.ToArray().FirstOrDefault(t => t.Key == "event_type").Value?.ToString();
            }
        });

        listener.Start();

        metrics.RecordDeliveryLatency("OrderPlacedEvent", 150.75);

        histogramValue.Should().Be(150.75);
        eventTypeTag.Should().Be("OrderPlacedEvent");
    }

    [Fact]
    public void RecordDeliveryLatency_ClampsNegativeValuesToZero()
    {
        using var metrics = new OutboxMetrics();
        double histogramValue = -1;

        using var listener = new MeterListener();
        listener.InstrumentPublished = (instrument, meterListener) =>
        {
            if (instrument.Meter.Name == OutboxMetrics.MeterName)
            {
                meterListener.EnableMeasurementEvents(instrument);
            }
        };

        listener.SetMeasurementEventCallback<double>((instrument, measurement, tags, state) =>
        {
            if (instrument.Name == "last_hour.outbox.delivery.latency")
            {
                histogramValue = measurement;
            }
        });

        listener.Start();

        metrics.RecordDeliveryLatency("OrderPlacedEvent", -50.0);

        histogramValue.Should().Be(0);
    }

    [Fact]
    public void RecordMessageProcessed_EmitsCounterAndHistogram()
    {
        using var metrics = new OutboxMetrics();
        long counterValue = 0;
        double histogramValue = 0;
        string? counterTag = null;

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
                counterValue += measurement;
                counterTag = tags.ToArray().FirstOrDefault(t => t.Key == "event_type").Value?.ToString();
            }
        });

        listener.SetMeasurementEventCallback<double>((instrument, measurement, tags, state) =>
        {
            if (instrument.Name == "last_hour.outbox.processing.duration")
            {
                histogramValue = measurement;
            }
        });

        listener.Start();

        metrics.RecordMessageProcessed("OrderPlacedEvent", 42.5);

        Assert.Equal(1, counterValue);
        Assert.Equal("OrderPlacedEvent", counterTag);
        Assert.Equal(42.5, histogramValue);
    }

    [Fact]
    public void RecordMessageFailed_EmitsCounter()
    {
        using var metrics = new OutboxMetrics();
        long counterValue = 0;
        string? counterTag = null;

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
                counterValue += measurement;
                counterTag = tags.ToArray().FirstOrDefault(t => t.Key == "event_type").Value?.ToString();
            }
        });

        listener.Start();

        metrics.RecordMessageFailed("PaymentFailedEvent");

        Assert.Equal(1, counterValue);
        Assert.Equal("PaymentFailedEvent", counterTag);
    }

    [Fact]
    public void RecordMessageAbandoned_EmitsCounter()
    {
        using var metrics = new OutboxMetrics();
        long counterValue = 0;
        string? counterTag = null;

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
                counterValue += measurement;
                counterTag = tags.ToArray().FirstOrDefault(t => t.Key == "event_type").Value?.ToString();
            }
        });

        listener.Start();

        metrics.RecordMessageAbandoned("PoisonEvent");

        Assert.Equal(1, counterValue);
        Assert.Equal("PoisonEvent", counterTag);
    }

    [Fact]
    public void RecordMessageReclaimed_EmitsCounter()
    {
        using var metrics = new OutboxMetrics();
        long counterValue = 0;
        string? counterTag = null;

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
                counterValue += measurement;
                counterTag = tags.ToArray().FirstOrDefault(t => t.Key == "event_type").Value?.ToString();
            }
        });

        listener.Start();

        metrics.RecordMessageReclaimed("ReclaimedEvent");

        Assert.Equal(1, counterValue);
        Assert.Equal("ReclaimedEvent", counterTag);
    }

    [Fact]
    public void RecordClaimConflict_EmitsCounter()
    {
        using var metrics = new OutboxMetrics();
        long counterValue = 0;

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
            if (instrument.Name == "last_hour.outbox.claims.conflicts")
            {
                counterValue += measurement;
            }
        });

        listener.Start();

        metrics.RecordClaimConflict();

        Assert.Equal(1, counterValue);
    }

    [Fact]
    public void RecordMessageRetried_EmitsCounter()
    {
        using var metrics = new OutboxMetrics();
        long counterValue = 0;
        string? counterTag = null;

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
            if (instrument.Name == "last_hour.outbox.messages.retried")
            {
                counterValue += measurement;
                counterTag = tags.ToArray().FirstOrDefault(t => t.Key == "event_type").Value?.ToString();
            }
        });

        listener.Start();

        metrics.RecordMessageRetried("RetriedEvent");

        Assert.Equal(1, counterValue);
        Assert.Equal("RetriedEvent", counterTag);
    }

    [Fact]
    public void RecordMessageRequeued_EmitsCounter()
    {
        using var metrics = new OutboxMetrics();
        long counterValue = 0;
        string? counterTag = null;

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
                counterValue += measurement;
                counterTag = tags.ToArray().FirstOrDefault(t => t.Key == "event_type").Value?.ToString();
            }
        });

        listener.Start();

        metrics.RecordMessageRequeued("RequeuedEvent");

        Assert.Equal(1, counterValue);
        Assert.Equal("RequeuedEvent", counterTag);
    }
}
