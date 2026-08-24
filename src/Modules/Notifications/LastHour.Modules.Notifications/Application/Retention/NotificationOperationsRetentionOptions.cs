namespace LastHour.Modules.Notifications.Application.Retention;

/// <summary>
/// Configuration settings governing historical operational record retention and cleanup policies.
/// </summary>
public sealed record NotificationOperationsRetentionOptions
{
    /// <summary>
    /// Default retention period in days for notification delivery execution attempt history (30 days).
    /// </summary>
    public const int DefaultAttemptRetentionDays = 30;

    /// <summary>
    /// Default retention period in days for administrative delivery audit log entries (90 days).
    /// </summary>
    public const int DefaultAuditRetentionDays = 90;

    /// <summary>
    /// Default maximum batch size for a single retention cleanup iteration (100 records).
    /// </summary>
    public const int DefaultCleanupBatchSize = 100;

    /// <summary>
    /// Default execution interval in minutes for the retention background worker (60 minutes).
    /// </summary>
    public const int DefaultExecutionIntervalMinutes = 60;

    /// <summary>
    /// Gets a value indicating whether background retention cleanup is enabled.
    /// </summary>
    public bool Enabled { get; init; } = true;

    /// <summary>
    /// Gets the retention period in days for execution attempt history.
    /// </summary>
    public int AttemptRetentionDays { get; init; } = DefaultAttemptRetentionDays;

    /// <summary>
    /// Gets the retention period in days for administrative audit log entries.
    /// </summary>
    public int AuditRetentionDays { get; init; } = DefaultAuditRetentionDays;

    /// <summary>
    /// Gets the maximum number of records deleted per batch iteration.
    /// </summary>
    public int CleanupBatchSize { get; init; } = DefaultCleanupBatchSize;

    /// <summary>
    /// Gets the background worker execution interval in minutes.
    /// </summary>
    public int ExecutionIntervalMinutes { get; init; } = DefaultExecutionIntervalMinutes;
}
