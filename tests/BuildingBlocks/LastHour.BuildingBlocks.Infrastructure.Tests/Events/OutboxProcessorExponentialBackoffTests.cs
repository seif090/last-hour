using LastHour.BuildingBlocks.Infrastructure.Persistence.Outbox;

namespace LastHour.BuildingBlocks.Infrastructure.Tests.Events;

public class OutboxProcessorExponentialBackoffTests
{
    [Theory]
    [InlineData(0, 5)]    // Retry 1: 5 * 2^0 = 5s
    [InlineData(1, 10)]   // Retry 2: 5 * 2^1 = 10s
    [InlineData(2, 20)]   // Retry 3: 5 * 2^2 = 20s
    [InlineData(3, 40)]   // Retry 4: 5 * 2^3 = 40s
    [InlineData(4, 80)]   // Retry 5: 5 * 2^4 = 80s
    [InlineData(5, 160)]  // Retry 6: 5 * 2^5 = 160s
    [InlineData(6, 300)]  // Retry 7: 5 * 2^6 = 320s -> capped at 300s (5m)
    [InlineData(10, 300)] // Retry 11: capped at 300s
    public void CalculateRetryDelay_ExponentialGrowth_ProducesExpectedDelaysAndCaps(int retryCount, int expectedSeconds)
    {
        TimeSpan baseDelay = TimeSpan.FromSeconds(5);
        TimeSpan maxDelay = TimeSpan.FromMinutes(5); // 300s

        TimeSpan actualDelay = OutboxProcessor.CalculateRetryDelay(baseDelay, maxDelay, retryCount);

        Assert.Equal(TimeSpan.FromSeconds(expectedSeconds), actualDelay);
    }

    [Fact]
    public void CalculateRetryDelay_ExtremeRetryCount_DoesNotOverflow()
    {
        TimeSpan baseDelay = TimeSpan.FromSeconds(5);
        TimeSpan maxDelay = TimeSpan.FromMinutes(5);

        TimeSpan actualDelay = OutboxProcessor.CalculateRetryDelay(baseDelay, maxDelay, 100);

        Assert.Equal(maxDelay, actualDelay);
    }

    [Fact]
    public void CalculateRetryDelay_NonPositiveBaseDelay_ReturnsZero()
    {
        TimeSpan actualDelay = OutboxProcessor.CalculateRetryDelay(TimeSpan.Zero, TimeSpan.FromMinutes(5), 1);

        Assert.Equal(TimeSpan.Zero, actualDelay);
    }
}
