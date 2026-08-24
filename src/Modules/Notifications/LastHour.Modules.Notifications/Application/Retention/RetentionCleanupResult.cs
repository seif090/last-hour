namespace LastHour.Modules.Notifications.Application.Retention;

/// <summary>
/// Result DTO summarizing the outcome of a retention policy cleanup run.
/// </summary>
public sealed record RetentionCleanupResult
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RetentionCleanupResult"/> class.
    /// </summary>
    public RetentionCleanupResult(int attemptsDeleted, int auditEntriesDeleted, DateTime executedAtUtc)
    {
        AttemptsDeleted = attemptsDeleted;
        AuditEntriesDeleted = auditEntriesDeleted;
        ExecutedAtUtc = executedAtUtc;
    }

    /// <summary>
    /// Gets the number of historical attempt records deleted.
    /// </summary>
    public int AttemptsDeleted { get; }

    /// <summary>
    /// Gets the number of historical audit entries deleted.
    /// </summary>
    public int AuditEntriesDeleted { get; }

    /// <summary>
    /// Gets the UTC timestamp when the cleanup executed.
    /// </summary>
    public DateTime ExecutedAtUtc { get; }
}
