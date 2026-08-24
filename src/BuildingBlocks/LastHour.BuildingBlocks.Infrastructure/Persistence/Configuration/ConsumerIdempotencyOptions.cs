namespace LastHour.BuildingBlocks.Infrastructure.Persistence.Configuration;

/// <summary>
/// Configuration for consumer idempotency controls. Bound from the <c>ConsumerIdempotency</c> configuration section.
/// </summary>
public sealed class ConsumerIdempotencyOptions
{
    /// <summary>
    /// Gets the name of the configuration section this type binds to.
    /// </summary>
    public const string SectionName = "ConsumerIdempotency";

    /// <summary>
    /// Gets or sets the lease duration for an exclusively claimed consumer idempotency lock.
    /// If an instance crashes or stalls during processing, the lease expires and another delivery
    /// attempt may reclaim it. Default is 2 minutes.
    /// </summary>
    public TimeSpan LeaseDuration { get; set; } = TimeSpan.FromMinutes(2);
}
