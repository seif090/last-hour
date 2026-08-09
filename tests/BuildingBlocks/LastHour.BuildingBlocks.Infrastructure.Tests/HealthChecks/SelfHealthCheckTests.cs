using LastHour.BuildingBlocks.Infrastructure.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace LastHour.BuildingBlocks.Infrastructure.Tests.HealthChecks;

public class SelfHealthCheckTests
{
    [Fact]
    public async Task CheckHealthAsync_AlwaysReportsHealthy()
    {
        var check = new SelfHealthCheck();

        HealthCheckResult result = await check.CheckHealthAsync(new HealthCheckContext());

        Assert.Equal(HealthStatus.Healthy, result.Status);
    }

    [Fact]
    public async Task CheckHealthAsync_ReportsDescription()
    {
        var check = new SelfHealthCheck();

        HealthCheckResult result = await check.CheckHealthAsync(new HealthCheckContext());

        Assert.False(string.IsNullOrWhiteSpace(result.Description));
    }
}
