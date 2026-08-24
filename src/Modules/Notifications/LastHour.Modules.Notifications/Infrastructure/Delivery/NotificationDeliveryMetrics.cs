using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace LastHour.Modules.Notifications.Infrastructure.Delivery;

/// <summary>
/// OpenTelemetry metrics and ActivitySource provider for notification delivery operations.
/// Emits strictly low-cardinality metrics (channel, status, failure_type) with ZERO PII.
/// </summary>
public sealed class NotificationDeliveryMetrics
{
    public const string MeterName = "LastHour.NotificationDelivery";
    public const string ActivitySourceName = "LastHour.NotificationDelivery";

    public static readonly ActivitySource ActivitySource = new ActivitySource(ActivitySourceName, "1.0.0");

    private readonly Counter<long> _processedCounter;
    private readonly Counter<long> _deliveredCounter;
    private readonly Counter<long> _failedCounter;
    private readonly Counter<long> _retriedCounter;
    private readonly Counter<long> _abandonedCounter;
    private readonly Counter<long> _reclaimedCounter;
    private readonly Counter<long> _claimConflictCounter;
    private readonly Histogram<double> _processingDurationHistogram;

    /// <summary>
    /// Initializes a new instance of the <see cref="NotificationDeliveryMetrics"/> class.
    /// </summary>
    public NotificationDeliveryMetrics(IMeterFactory meterFactory)
    {
        ArgumentNullException.ThrowIfNull(meterFactory);

        Meter meter = meterFactory.Create(MeterName, "1.0.0");

        _processedCounter = meter.CreateCounter<long>(
            "last_hour.notification_delivery.processed",
            unit: "{delivery}",
            description: "Total number of notification deliveries processed.");

        _deliveredCounter = meter.CreateCounter<long>(
            "last_hour.notification_delivery.delivered",
            unit: "{delivery}",
            description: "Total number of notification deliveries completed successfully.");

        _failedCounter = meter.CreateCounter<long>(
            "last_hour.notification_delivery.failed",
            unit: "{delivery}",
            description: "Total number of notification delivery attempt failures.");

        _retriedCounter = meter.CreateCounter<long>(
            "last_hour.notification_delivery.retried",
            unit: "{delivery}",
            description: "Total number of notification delivery retries scheduled.");

        _abandonedCounter = meter.CreateCounter<long>(
            "last_hour.notification_delivery.abandoned",
            unit: "{delivery}",
            description: "Total number of notification deliveries abandoned to permanent failure.");

        _reclaimedCounter = meter.CreateCounter<long>(
            "last_hour.notification_delivery.reclaimed",
            unit: "{delivery}",
            description: "Total number of expired notification delivery leases reclaimed.");

        _claimConflictCounter = meter.CreateCounter<long>(
            "last_hour.notification_delivery.claim_conflict",
            unit: "{delivery}",
            description: "Total number of delivery claim concurrency conflicts.");

        _processingDurationHistogram = meter.CreateHistogram<double>(
            "last_hour.notification_delivery.processing_duration",
            unit: "ms",
            description: "Processing duration of notification delivery attempts in milliseconds.");
    }

    public void RecordProcessed(string channel, double durationMs)
    {
        _processedCounter.Add(1, new KeyValuePair<string, object?>("channel", channel));
        _processingDurationHistogram.Record(durationMs, new KeyValuePair<string, object?>("channel", channel));
    }

    public void RecordDelivered(string channel)
    {
        _deliveredCounter.Add(1, new KeyValuePair<string, object?>("channel", channel));
    }

    public void RecordFailed(string channel, string failureType)
    {
        _failedCounter.Add(1, new KeyValuePair<string, object?>("channel", channel), new KeyValuePair<string, object?>("failure_type", failureType));
    }

    public void RecordRetried(string channel, string failureType)
    {
        _retriedCounter.Add(1, new KeyValuePair<string, object?>("channel", channel), new KeyValuePair<string, object?>("failure_type", failureType));
    }

    public void RecordAbandoned(string channel, string failureType)
    {
        _abandonedCounter.Add(1, new KeyValuePair<string, object?>("channel", channel), new KeyValuePair<string, object?>("failure_type", failureType));
    }

    public void RecordReclaimed(string channel)
    {
        _reclaimedCounter.Add(1, new KeyValuePair<string, object?>("channel", channel));
    }

    public void RecordClaimConflict()
    {
        _claimConflictCounter.Add(1);
    }
}
