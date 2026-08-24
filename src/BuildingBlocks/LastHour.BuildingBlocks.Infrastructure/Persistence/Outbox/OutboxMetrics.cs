using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace LastHour.BuildingBlocks.Infrastructure.Persistence.Outbox;

/// <summary>
/// Emits OpenTelemetry and .NET runtime metrics and tracing for the Transactional Outbox pipeline.
/// </summary>
public sealed class OutboxMetrics : IDisposable
{
    /// <summary>
    /// The name of the outbox meter and activity source.
    /// </summary>
    public const string MeterName = "LastHour.Outbox";

    /// <summary>
    /// The version of the outbox meter and activity source.
    /// </summary>
    public const string MeterVersion = "1.0.0";

    /// <summary>
    /// The shared ActivitySource for tracing outbox message dispatch operations.
    /// </summary>
    public static readonly ActivitySource ActivitySource = new ActivitySource(MeterName, MeterVersion);

    private readonly Meter _meter;
    private readonly Counter<long> _messagesProcessedCounter;
    private readonly Counter<long> _messagesFailedCounter;
    private readonly Counter<long> _messagesAbandonedCounter;
    private readonly Counter<long> _messagesReclaimedCounter;
    private readonly Counter<long> _messagesRetriedCounter;
    private readonly Counter<long> _messagesRequeuedCounter;
    private readonly Counter<long> _claimsConflictsCounter;
    private readonly Histogram<double> _processingDurationHistogram;
    private readonly Histogram<double> _deliveryLatencyHistogram;

    /// <summary>
    /// Initializes a new instance of the <see cref="OutboxMetrics"/> class.
    /// </summary>
    public OutboxMetrics()
    {
        _meter = new Meter(MeterName, MeterVersion);

        _messagesProcessedCounter = _meter.CreateCounter<long>(
            "last_hour.outbox.messages.processed",
            unit: "{message}",
            description: "Number of outbox messages successfully dispatched and committed.");

        _messagesFailedCounter = _meter.CreateCounter<long>(
            "last_hour.outbox.messages.failed",
            unit: "{attempt}",
            description: "Number of failed outbox message processing attempts.");

        _messagesAbandonedCounter = _meter.CreateCounter<long>(
            "last_hour.outbox.messages.abandoned",
            unit: "{message}",
            description: "Number of poison outbox messages abandoned after exhausting retry attempts.");

        _messagesRetriedCounter = _meter.CreateCounter<long>(
            "last_hour.outbox.messages.retried",
            unit: "{attempt}",
            description: "Number of failed outbox messages scheduled for subsequent retry.");

        _messagesRequeuedCounter = _meter.CreateCounter<long>(
            "last_hour.outbox.messages.requeued",
            unit: "{message}",
            description: "Number of abandoned outbox messages manually re-queued for processing.");

        _messagesReclaimedCounter = _meter.CreateCounter<long>(
            "last_hour.outbox.messages.reclaimed",
            unit: "{message}",
            description: "Number of expired outbox message processing leases successfully reclaimed.");

        _claimsConflictsCounter = _meter.CreateCounter<long>(
            "last_hour.outbox.claims.conflicts",
            unit: "{conflict}",
            description: "Number of atomic outbox message claim conflicts due to concurrency.");

        _processingDurationHistogram = _meter.CreateHistogram<double>(
            "last_hour.outbox.processing.duration",
            unit: "ms",
            description: "Duration in milliseconds to process an outbox message from claim acquisition to completion.");

        _deliveryLatencyHistogram = _meter.CreateHistogram<double>(
            "last_hour.outbox.delivery.latency",
            unit: "ms",
            description: "End-to-end delivery latency in milliseconds from event occurrence to successful outbox processing completion.");
    }

    /// <summary>
    /// Records a successfully processed outbox message and its processing duration.
    /// </summary>
    /// <param name="eventType">The type name of the processed message or event.</param>
    /// <param name="durationMs">The processing duration in milliseconds.</param>
    public void RecordMessageProcessed(string eventType, double durationMs)
    {
        var tag = new KeyValuePair<string, object?>("event_type", eventType);
        _messagesProcessedCounter.Add(1, tag);
        _processingDurationHistogram.Record(durationMs, tag);
    }

    /// <summary>
    /// Records the end-to-end delivery latency of a successfully processed outbox message.
    /// </summary>
    /// <param name="eventType">The type name of the processed message or event.</param>
    /// <param name="latencyMs">The delivery latency in milliseconds from occurrence to commit.</param>
    public void RecordDeliveryLatency(string eventType, double latencyMs)
    {
        double clampedLatency = Math.Max(0, latencyMs);
        _deliveryLatencyHistogram.Record(clampedLatency, new KeyValuePair<string, object?>("event_type", eventType));
    }

    /// <summary>
    /// Records a failed outbox message processing attempt with failure classification.
    /// </summary>
    /// <param name="eventType">The type name of the failed message or event.</param>
    /// <param name="failureType">The classified failure category.</param>
    public void RecordMessageFailed(string eventType, string failureType = "Unknown")
    {
        _messagesFailedCounter.Add(
            1,
            new KeyValuePair<string, object?>("event_type", eventType),
            new KeyValuePair<string, object?>("failure_type", failureType));
    }

    /// <summary>
    /// Records a poison outbox message that has exhausted retries and was abandoned with failure classification.
    /// </summary>
    /// <param name="eventType">The type name of the abandoned message or event.</param>
    /// <param name="failureType">The classified failure category.</param>
    public void RecordMessageAbandoned(string eventType, string failureType = "Unknown")
    {
        _messagesAbandonedCounter.Add(
            1,
            new KeyValuePair<string, object?>("event_type", eventType),
            new KeyValuePair<string, object?>("failure_type", failureType));
    }

    /// <summary>
    /// Records a failed outbox message scheduled for subsequent retry with failure classification.
    /// </summary>
    /// <param name="eventType">The type name of the retried message or event.</param>
    /// <param name="failureType">The classified failure category.</param>
    public void RecordMessageRetried(string eventType, string failureType = "Unknown")
    {
        _messagesRetriedCounter.Add(
            1,
            new KeyValuePair<string, object?>("event_type", eventType),
            new KeyValuePair<string, object?>("failure_type", failureType));
    }

    /// <summary>
    /// Records a successfully re-queued abandoned outbox message.
    /// </summary>
    /// <param name="eventType">The type name of the re-queued message or event.</param>
    public void RecordMessageRequeued(string eventType)
    {
        _messagesRequeuedCounter.Add(1, new KeyValuePair<string, object?>("event_type", eventType));
    }

    /// <summary>
    /// Records a successfully reclaimed expired processing lease.
    /// </summary>
    /// <param name="eventType">The type name of the reclaimed message or event.</param>
    public void RecordMessageReclaimed(string eventType)
    {
        _messagesReclaimedCounter.Add(1, new KeyValuePair<string, object?>("event_type", eventType));
    }

    /// <summary>
    /// Records an atomic claim race conflict when another processor instance claimed the message first.
    /// </summary>
    public void RecordClaimConflict()
    {
        _claimsConflictsCounter.Add(1);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _meter.Dispose();
    }
}
