namespace LastHour.BuildingBlocks.Infrastructure.Persistence.Outbox;

/// <summary>
/// Provides thread-safe, in-memory state and heartbeat tracking for the <see cref="OutboxProcessor"/>.
/// </summary>
public interface IOutboxProcessorTracker
{
    /// <summary>
    /// Gets the UTC timestamp of the most recent processor heartbeat.
    /// </summary>
    DateTime? LastHeartbeatUtc { get; }

    /// <summary>
    /// Gets the UTC timestamp of the most recent successful message processing.
    /// </summary>
    DateTime? LastSuccessUtc { get; }

    /// <summary>
    /// Gets the UTC timestamp of the most recent processor failure.
    /// </summary>
    DateTime? LastFailureUtc { get; }

    /// <summary>
    /// Gets a value indicating whether the processor is actively running.
    /// </summary>
    bool IsRunning { get; }

    /// <summary>
    /// Records a heartbeat pulse for the processor.
    /// </summary>
    /// <param name="utcNow">The current UTC instant.</param>
    void RecordHeartbeat(DateTime utcNow);

    /// <summary>
    /// Records a successful message processing event.
    /// </summary>
    /// <param name="utcNow">The current UTC instant.</param>
    void RecordSuccess(DateTime utcNow);

    /// <summary>
    /// Records a processor failure event.
    /// </summary>
    /// <param name="utcNow">The current UTC instant.</param>
    void RecordFailure(DateTime utcNow);

    /// <summary>
    /// Sets the active running state of the processor.
    /// </summary>
    /// <param name="isRunning"><see langword="true"/> when running; <see langword="false"/> when stopped.</param>
    void SetRunning(bool isRunning);
}
