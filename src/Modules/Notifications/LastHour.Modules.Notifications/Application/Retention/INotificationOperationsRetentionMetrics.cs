namespace LastHour.Modules.Notifications.Application.Retention;

/// <summary>
/// Metrics contract for notification operations data retention cleanup.
/// </summary>
public interface INotificationOperationsRetentionMetrics
{
    /// <summary>
    /// Records the start of a retention cleanup operation.
    /// </summary>
    void RecordCleanupStarted();

    /// <summary>
    /// Records the successful completion of a retention cleanup operation with deleted record counts.
    /// </summary>
    void RecordCleanupCompleted(int attemptsDeleted, int auditEntriesDeleted);

    /// <summary>
    /// Records a failure during retention cleanup execution.
    /// </summary>
    void RecordCleanupFailed();
}
