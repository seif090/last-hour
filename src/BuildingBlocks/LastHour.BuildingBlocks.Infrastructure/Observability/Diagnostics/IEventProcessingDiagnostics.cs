namespace LastHour.BuildingBlocks.Infrastructure.Observability.Diagnostics;

/// <summary>
/// Provides operational diagnostic snapshots of the Transactional Outbox and Consumer Inbox pipelines.
/// </summary>
public interface IEventProcessingDiagnostics
{
    /// <summary>
    /// Captures a point-in-time diagnostic summary of the Transactional Outbox pipeline.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A diagnostic snapshot of the outbox.</returns>
    Task<OutboxDiagnosticsSnapshot> GetOutboxSnapshotAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Captures a point-in-time diagnostic summary of the Consumer Inbox pipeline.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A diagnostic snapshot of the consumer inbox.</returns>
    Task<ConsumerInboxDiagnosticsSnapshot> GetConsumerInboxSnapshotAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Point-in-time diagnostic snapshot of the Outbox subsystem.
/// </summary>
public sealed class OutboxDiagnosticsSnapshot
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OutboxDiagnosticsSnapshot"/> class.
    /// </summary>
    /// <param name="totalPendingCount">Total pending count.</param>
    /// <param name="readyPendingCount">Ready pending count.</param>
    /// <param name="scheduledRetryCount">Scheduled retry count.</param>
    /// <param name="activeProcessingCount">Active processing count.</param>
    /// <param name="abandonedCount">Abandoned count.</param>
    /// <param name="oldestPendingAge">Oldest pending age.</param>
    /// <param name="processorIsRunning">Whether the processor is running.</param>
    /// <param name="lastHeartbeatUtc">Last heartbeat UTC.</param>
    /// <param name="lastSuccessUtc">Last success UTC.</param>
    /// <param name="lastFailureUtc">Last failure UTC.</param>
    public OutboxDiagnosticsSnapshot(
        int totalPendingCount,
        int readyPendingCount,
        int scheduledRetryCount,
        int activeProcessingCount,
        int abandonedCount,
        TimeSpan oldestPendingAge,
        bool processorIsRunning,
        DateTime? lastHeartbeatUtc,
        DateTime? lastSuccessUtc,
        DateTime? lastFailureUtc)
    {
        TotalPendingCount = totalPendingCount;
        ReadyPendingCount = readyPendingCount;
        ScheduledRetryCount = scheduledRetryCount;
        ActiveProcessingCount = activeProcessingCount;
        AbandonedCount = abandonedCount;
        OldestPendingAge = oldestPendingAge;
        ProcessorIsRunning = processorIsRunning;
        LastHeartbeatUtc = lastHeartbeatUtc;
        LastSuccessUtc = lastSuccessUtc;
        LastFailureUtc = lastFailureUtc;
    }

    /// <summary>Gets the total pending message count.</summary>
    public int TotalPendingCount { get; }

    /// <summary>Gets the ready pending message count.</summary>
    public int ReadyPendingCount { get; }

    /// <summary>Gets the scheduled retry message count.</summary>
    public int ScheduledRetryCount { get; }

    /// <summary>Gets the active processing message count.</summary>
    public int ActiveProcessingCount { get; }

    /// <summary>Gets the abandoned poison message count.</summary>
    public int AbandonedCount { get; }

    /// <summary>Gets the oldest pending message age.</summary>
    public TimeSpan OldestPendingAge { get; }

    /// <summary>Gets a value indicating whether the processor background service is currently active.</summary>
    public bool ProcessorIsRunning { get; }

    /// <summary>Gets the timestamp of the last heartbeat.</summary>
    public DateTime? LastHeartbeatUtc { get; }

    /// <summary>Gets the timestamp of the last successful processing cycle.</summary>
    public DateTime? LastSuccessUtc { get; }

    /// <summary>Gets the timestamp of the last failed processing cycle.</summary>
    public DateTime? LastFailureUtc { get; }
}

/// <summary>
/// Point-in-time diagnostic snapshot of the Consumer Inbox subsystem.
/// </summary>
public sealed class ConsumerInboxDiagnosticsSnapshot
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ConsumerInboxDiagnosticsSnapshot"/> class.
    /// </summary>
    /// <param name="totalInboxCount">Total inbox count.</param>
    /// <param name="completedCount">Completed count.</param>
    /// <param name="activeLeaseCount">Active lease count.</param>
    /// <param name="expiredLeaseCount">Expired lease count.</param>
    /// <param name="oldestExpiredLeaseAge">Oldest expired lease age.</param>
    public ConsumerInboxDiagnosticsSnapshot(
        int totalInboxCount,
        int completedCount,
        int activeLeaseCount,
        int expiredLeaseCount,
        TimeSpan oldestExpiredLeaseAge)
    {
        TotalInboxCount = totalInboxCount;
        CompletedCount = completedCount;
        ActiveLeaseCount = activeLeaseCount;
        ExpiredLeaseCount = expiredLeaseCount;
        OldestExpiredLeaseAge = oldestExpiredLeaseAge;
    }

    /// <summary>Gets the total consumer inbox message count.</summary>
    public int TotalInboxCount { get; }

    /// <summary>Gets the completed consumer inbox message count.</summary>
    public int CompletedCount { get; }

    /// <summary>Gets the active in-progress consumer lease count.</summary>
    public int ActiveLeaseCount { get; }

    /// <summary>Gets the expired uncompleted consumer lease count.</summary>
    public int ExpiredLeaseCount { get; }

    /// <summary>Gets the age of the oldest expired uncompleted lease.</summary>
    public TimeSpan OldestExpiredLeaseAge { get; }
}
