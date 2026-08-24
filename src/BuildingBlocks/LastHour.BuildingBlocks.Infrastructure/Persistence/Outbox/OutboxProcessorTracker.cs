namespace LastHour.BuildingBlocks.Infrastructure.Persistence.Outbox;

/// <summary>
/// Thread-safe in-memory implementation of <see cref="IOutboxProcessorTracker"/>.
/// </summary>
public sealed class OutboxProcessorTracker : IOutboxProcessorTracker
{
    private readonly object _syncLock = new object();
    private DateTime? _lastHeartbeatUtc;
    private DateTime? _lastSuccessUtc;
    private DateTime? _lastFailureUtc;
    private bool _isRunning;

    /// <inheritdoc/>
    public DateTime? LastHeartbeatUtc
    {
        get
        {
            lock (_syncLock)
            {
                return _lastHeartbeatUtc;
            }
        }
    }

    /// <inheritdoc/>
    public DateTime? LastSuccessUtc
    {
        get
        {
            lock (_syncLock)
            {
                return _lastSuccessUtc;
            }
        }
    }

    /// <inheritdoc/>
    public DateTime? LastFailureUtc
    {
        get
        {
            lock (_syncLock)
            {
                return _lastFailureUtc;
            }
        }
    }

    /// <inheritdoc/>
    public bool IsRunning
    {
        get
        {
            lock (_syncLock)
            {
                return _isRunning;
            }
        }
    }

    /// <inheritdoc/>
    public void RecordHeartbeat(DateTime utcNow)
    {
        lock (_syncLock)
        {
            _lastHeartbeatUtc = utcNow;
        }
    }

    /// <inheritdoc/>
    public void RecordSuccess(DateTime utcNow)
    {
        lock (_syncLock)
        {
            _lastSuccessUtc = utcNow;
        }
    }

    /// <inheritdoc/>
    public void RecordFailure(DateTime utcNow)
    {
        lock (_syncLock)
        {
            _lastFailureUtc = utcNow;
        }
    }

    /// <inheritdoc/>
    public void SetRunning(bool isRunning)
    {
        lock (_syncLock)
        {
            _isRunning = isRunning;
        }
    }
}
