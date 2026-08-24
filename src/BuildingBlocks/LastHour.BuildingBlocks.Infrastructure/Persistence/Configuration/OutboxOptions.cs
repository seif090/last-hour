namespace LastHour.BuildingBlocks.Infrastructure.Persistence.Configuration;

/// <summary>
/// Configuration for the outbox processor. Bound from the <c>Outbox</c> configuration section.
/// </summary>
public sealed class OutboxOptions
{
    /// <summary>
    /// Gets the name of the configuration section this type binds to.
    /// </summary>
    public const string SectionName = "Outbox";

    /// <summary>
    /// Gets or sets a value indicating whether the outbox processor runs. When disabled, outbox
    /// messages are still written durably but are not dispatched automatically.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the delay between processor cycles. A value below one second is not supported.
    /// </summary>
    public TimeSpan ProcessingInterval { get; set; } = TimeSpan.FromSeconds(15);

    /// <summary>
    /// Gets or sets the maximum number of pending messages processed in a single cycle.
    /// </summary>
    public int BatchSize { get; set; } = 100;

    /// <summary>
    /// Gets or sets the maximum number of attempts per message before it is abandoned and
    /// flagged as processed with its last error preserved.
    /// </summary>
    public int MaxRetryCount { get; set; } = 10;

    /// <summary>
    /// Gets or sets the lease duration for an exclusively claimed outbox message.
    /// If an instance crashes or stalls, the lease expires and another instance may reclaim it.
    /// </summary>
    public TimeSpan LeaseDuration { get; set; } = TimeSpan.FromMinutes(1);

    /// <summary>
    /// Gets or sets the base delay used for exponential backoff on transient processing failures.
    /// A value below one second is not supported.
    /// </summary>
    public TimeSpan BaseRetryDelay { get; set; } = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Gets or sets the maximum retry delay cap for exponential backoff.
    /// Must be greater than or equal to <see cref="BaseRetryDelay"/>.
    /// </summary>
    public TimeSpan MaximumRetryDelay { get; set; } = TimeSpan.FromMinutes(5);
}
