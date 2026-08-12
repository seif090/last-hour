using LastHour.BuildingBlocks.Application.Contracts;

namespace LastHour.Modules.Identity.Tests.Application.TestSupport;

/// <summary>
/// Deterministic <see cref="IClock"/> for tests that reason about expirations and revocations.
/// </summary>
public sealed class FixedClock : IClock
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FixedClock"/> class.
    /// </summary>
    /// <param name="utcNow">The fixed instant the clock reports as "now".</param>
    public FixedClock(DateTime utcNow)
    {
        UtcNow = utcNow;
    }

    /// <inheritdoc/>
    public DateTime UtcNow { get; }
}
