namespace LastHour.BuildingBlocks.Infrastructure.HealthChecks;

/// <summary>
/// Options controlling the health checks registered by the infrastructure layer: the
/// PostgreSQL check is driven by the EF Core model, while Redis, disk and memory are
/// configured here. Redis is only registered when a connection string is supplied.
/// </summary>
public sealed class HealthChecksOptions
{
    /// <summary>
    /// The configuration section name this type binds to.
    /// </summary>
    public const string SectionName = "HealthChecks";

    /// <summary>
    /// The default minimum free disk space, in megabytes, reported as healthy.
    /// </summary>
    public const int DefaultDiskMinimumFreeMegabytes = 512;

    /// <summary>
    /// The default maximum memory usage (working set), in bytes, reported as healthy.
    /// </summary>
    public const long DefaultMemoryMaximumUsedBytes = 1024L * 1024L * 1024L;

    /// <summary>
    /// The default per-check timeout, in seconds.
    /// </summary>
    public const int DefaultTimeoutSeconds = 5;

    /// <summary>
    /// Gets or sets the per-check timeout, in seconds.
    /// </summary>
    public int TimeoutSeconds { get; set; } = DefaultTimeoutSeconds;

    /// <summary>
    /// Gets or sets the Redis health check options.
    /// </summary>
    public RedisHealthCheckOptions Redis { get; set; } = new RedisHealthCheckOptions();

    /// <summary>
    /// Gets or sets the disk health check options.
    /// </summary>
    public DiskHealthCheckOptions Disk { get; set; } = new DiskHealthCheckOptions();

    /// <summary>
    /// Gets or sets the memory health check options.
    /// </summary>
    public MemoryHealthCheckOptions Memory { get; set; } = new MemoryHealthCheckOptions();

    /// <summary>
    /// Gets or sets the outbox health check options.
    /// </summary>
    public OutboxHealthCheckOptions Outbox { get; set; } = new OutboxHealthCheckOptions();

    /// <summary>
    /// Gets or sets the consumer inbox health check options.
    /// </summary>
    public ConsumerInboxHealthCheckOptions ConsumerInbox { get; set; } = new ConsumerInboxHealthCheckOptions();
}

/// <summary>
/// Options for the Redis health check.
/// </summary>
public sealed class RedisHealthCheckOptions
{
    /// <summary>
    /// Gets or sets the Redis connection string. When empty the Redis health check is not registered.
    /// </summary>
    public string? ConnectionString { get; set; }
}

/// <summary>
/// Options for the disk health check.
/// </summary>
public sealed class DiskHealthCheckOptions
{
    /// <summary>
    /// Gets or sets the path whose drive is inspected. Defaults to the application base directory.
    /// </summary>
    public string? Path { get; set; }

    /// <summary>
    /// Gets or sets the minimum free disk space, in megabytes, reported as healthy.
    /// </summary>
    public int MinimumFreeMegabytes { get; set; } = HealthChecksOptions.DefaultDiskMinimumFreeMegabytes;
}

/// <summary>
/// Options for the memory health check.
/// </summary>
public sealed class MemoryHealthCheckOptions
{
    /// <summary>
    /// Gets or sets the maximum memory usage (working set), in bytes, reported as healthy.
    /// </summary>
    public long MaximumUsedBytes { get; set; } = HealthChecksOptions.DefaultMemoryMaximumUsedBytes;
}

/// <summary>
/// Options for the outbox processor and backlog health check.
/// </summary>
public sealed class OutboxHealthCheckOptions
{
    /// <summary>
    /// Gets or sets the pending message count threshold for degraded status.
    /// </summary>
    public int WarningPendingMessageCount { get; set; } = 50;

    /// <summary>
    /// Gets or sets the pending message count threshold for unhealthy status.
    /// </summary>
    public int CriticalPendingMessageCount { get; set; } = 500;

    /// <summary>
    /// Gets or sets the oldest pending message age threshold for degraded status.
    /// </summary>
    public TimeSpan WarningOldestMessageAge { get; set; } = TimeSpan.FromMinutes(6);

    /// <summary>
    /// Gets or sets the oldest pending message age threshold for unhealthy status.
    /// </summary>
    public TimeSpan CriticalOldestMessageAge { get; set; } = TimeSpan.FromMinutes(15);

    /// <summary>
    /// Gets or sets the processor heartbeat freshness threshold for unhealthy status.
    /// </summary>
    public TimeSpan ProcessorHeartbeatTimeout { get; set; } = TimeSpan.FromMinutes(1);

    /// <summary>
    /// Gets or sets the abandoned message count threshold for degraded status.
    /// </summary>
    public int WarningAbandonedMessageCount { get; set; } = 10;

    /// <summary>
    /// Gets or sets the abandoned message count threshold for unhealthy status.
    /// </summary>
    public int CriticalAbandonedMessageCount { get; set; } = 50;
}

/// <summary>
/// Options for the consumer inbox and lease health check.
/// </summary>
public sealed class ConsumerInboxHealthCheckOptions
{
    /// <summary>
    /// Gets or sets the expired uncompleted lease count threshold for degraded status.
    /// </summary>
    public int WarningStalledLeaseCount { get; set; } = 20;

    /// <summary>
    /// Gets or sets the expired uncompleted lease count threshold for unhealthy status.
    /// </summary>
    public int CriticalStalledLeaseCount { get; set; } = 100;

    /// <summary>
    /// Gets or sets the oldest stalled lease age threshold for degraded status.
    /// </summary>
    public TimeSpan WarningOldestStalledAge { get; set; } = TimeSpan.FromMinutes(10);

    /// <summary>
    /// Gets or sets the oldest stalled lease age threshold for unhealthy status.
    /// </summary>
    public TimeSpan CriticalOldestStalledAge { get; set; } = TimeSpan.FromMinutes(30);
}
