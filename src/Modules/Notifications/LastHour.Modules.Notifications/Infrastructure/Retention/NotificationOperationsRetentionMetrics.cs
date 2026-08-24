using System.Diagnostics.Metrics;
using LastHour.Modules.Notifications.Application.Retention;

namespace LastHour.Modules.Notifications.Infrastructure.Retention;

/// <summary>
/// OpenTelemetry metrics provider for notification operations retention cleanup.
/// Emits strictly zero-PII metrics (operation, outcome).
/// </summary>
public sealed class NotificationOperationsRetentionMetrics : INotificationOperationsRetentionMetrics
{
    public const string MeterName = "LastHour.NotificationRetention";

    private readonly Counter<long> _cleanupStartedCounter;
    private readonly Counter<long> _cleanupCompletedCounter;
    private readonly Counter<long> _cleanupFailedCounter;
    private readonly Counter<long> _attemptsDeletedCounter;
    private readonly Counter<long> _auditEntriesDeletedCounter;

    /// <summary>
    /// Initializes a new instance of the <see cref="NotificationOperationsRetentionMetrics"/> class.
    /// </summary>
    public NotificationOperationsRetentionMetrics(IMeterFactory meterFactory)
    {
        ArgumentNullException.ThrowIfNull(meterFactory);

        Meter meter = meterFactory.Create(MeterName, "1.0.0");

        _cleanupStartedCounter = meter.CreateCounter<long>(
            "retention.cleanup.started",
            unit: "{execution}",
            description: "Total number of retention cleanup executions started.");

        _cleanupCompletedCounter = meter.CreateCounter<long>(
            "retention.cleanup.completed",
            unit: "{execution}",
            description: "Total number of retention cleanup executions completed successfully.");

        _cleanupFailedCounter = meter.CreateCounter<long>(
            "retention.cleanup.failed",
            unit: "{execution}",
            description: "Total number of retention cleanup execution failures.");

        _attemptsDeletedCounter = meter.CreateCounter<long>(
            "retention.attempts.deleted",
            unit: "{record}",
            description: "Total number of historical attempt records deleted by retention.");

        _auditEntriesDeletedCounter = meter.CreateCounter<long>(
            "retention.audit_entries.deleted",
            unit: "{record}",
            description: "Total number of historical audit entries deleted by retention.");
    }

    /// <inheritdoc/>
    public void RecordCleanupStarted()
    {
        _cleanupStartedCounter.Add(1, new KeyValuePair<string, object?>("operation", "retention_cleanup"));
    }

    /// <inheritdoc/>
    public void RecordCleanupCompleted(int attemptsDeleted, int auditEntriesDeleted)
    {
        _cleanupCompletedCounter.Add(1, new KeyValuePair<string, object?>("operation", "retention_cleanup"), new KeyValuePair<string, object?>("outcome", "success"));
        _attemptsDeletedCounter.Add(attemptsDeleted);
        _auditEntriesDeletedCounter.Add(auditEntriesDeleted);
    }

    /// <inheritdoc/>
    public void RecordCleanupFailed()
    {
        _cleanupFailedCounter.Add(1, new KeyValuePair<string, object?>("operation", "retention_cleanup"), new KeyValuePair<string, object?>("outcome", "failure"));
    }
}
