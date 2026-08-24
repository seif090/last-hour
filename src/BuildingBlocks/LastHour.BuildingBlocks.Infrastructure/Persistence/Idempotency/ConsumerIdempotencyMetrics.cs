using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace LastHour.BuildingBlocks.Infrastructure.Persistence.Idempotency;

/// <summary>
/// Emits OpenTelemetry and .NET runtime metrics and tracing for the Consumer Idempotency layer.
/// </summary>
public sealed class ConsumerIdempotencyMetrics : IDisposable
{
    /// <summary>
    /// The name of the consumer idempotency meter and activity source.
    /// </summary>
    public const string MeterName = "LastHour.ConsumerIdempotency";

    /// <summary>
    /// The version of the consumer idempotency meter and activity source.
    /// </summary>
    public const string MeterVersion = "1.0.0";

    /// <summary>
    /// The shared ActivitySource for tracing consumer event execution.
    /// </summary>
    public static readonly ActivitySource ActivitySource = new ActivitySource(MeterName, MeterVersion);

    private readonly Meter _meter;
    private readonly Counter<long> _consumerProcessedCounter;
    private readonly Counter<long> _consumerDuplicateCounter;
    private readonly Counter<long> _consumerFailedCounter;
    private readonly Counter<long> _consumerLeaseAcquiredCounter;
    private readonly Counter<long> _consumerLeaseReclaimedCounter;
    private readonly Histogram<double> _processingDurationHistogram;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConsumerIdempotencyMetrics"/> class.
    /// </summary>
    public ConsumerIdempotencyMetrics()
    {
        _meter = new Meter(MeterName, MeterVersion);

        _consumerProcessedCounter = _meter.CreateCounter<long>(
            "last_hour.consumer.processed",
            unit: "{event}",
            description: "Number of events successfully processed and committed by a consumer.");

        _consumerDuplicateCounter = _meter.CreateCounter<long>(
            "last_hour.consumer.duplicate",
            unit: "{event}",
            description: "Number of duplicate event deliveries suppressed for a consumer.");

        _consumerFailedCounter = _meter.CreateCounter<long>(
            "last_hour.consumer.failed",
            unit: "{attempt}",
            description: "Number of failed consumer event handling attempts.");

        _consumerLeaseAcquiredCounter = _meter.CreateCounter<long>(
            "last_hour.consumer.lease_acquired",
            unit: "{lease}",
            description: "Number of initial consumer processing leases acquired.");

        _consumerLeaseReclaimedCounter = _meter.CreateCounter<long>(
            "last_hour.consumer.lease_reclaimed",
            unit: "{lease}",
            description: "Number of expired consumer processing leases reclaimed by a new worker.");

        _processingDurationHistogram = _meter.CreateHistogram<double>(
            "last_hour.consumer.processing_duration",
            unit: "ms",
            description: "Duration in milliseconds to execute consumer event processing.");
    }

    /// <summary>
    /// Records a successfully processed and completed event by a consumer along with its processing duration.
    /// </summary>
    /// <param name="consumer">The consumer identifier.</param>
    /// <param name="durationMs">The processing duration in milliseconds.</param>
    public void RecordProcessed(string consumer, double durationMs = 0)
    {
        var tag = new KeyValuePair<string, object?>("consumer", consumer);
        _consumerProcessedCounter.Add(1, tag);
        if (durationMs > 0)
        {
            _processingDurationHistogram.Record(durationMs, tag);
        }
    }

    /// <summary>
    /// Records a suppressed duplicate event delivery for a consumer.
    /// </summary>
    /// <param name="consumer">The consumer identifier.</param>
    public void RecordDuplicate(string consumer)
    {
        _consumerDuplicateCounter.Add(1, new KeyValuePair<string, object?>("consumer", consumer));
    }

    /// <summary>
    /// Records a failed event processing attempt by a consumer with failure classification.
    /// </summary>
    /// <param name="consumer">The consumer identifier.</param>
    /// <param name="failureType">The classified failure category.</param>
    public void RecordFailed(string consumer, string failureType = "Unknown")
    {
        _consumerFailedCounter.Add(
            1,
            new KeyValuePair<string, object?>("consumer", consumer),
            new KeyValuePair<string, object?>("failure_type", failureType));
    }

    /// <summary>
    /// Records an initial processing lease acquisition for an event by a consumer.
    /// </summary>
    /// <param name="consumer">The consumer identifier.</param>
    public void RecordLeaseAcquired(string consumer)
    {
        _consumerLeaseAcquiredCounter.Add(1, new KeyValuePair<string, object?>("consumer", consumer));
    }

    /// <summary>
    /// Records a reclaimed expired processing lease for an event by a consumer.
    /// </summary>
    /// <param name="consumer">The consumer identifier.</param>
    public void RecordLeaseReclaimed(string consumer)
    {
        _consumerLeaseReclaimedCounter.Add(1, new KeyValuePair<string, object?>("consumer", consumer));
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _meter.Dispose();
    }
}
