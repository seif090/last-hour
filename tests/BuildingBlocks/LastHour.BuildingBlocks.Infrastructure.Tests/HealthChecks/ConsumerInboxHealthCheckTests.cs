using FluentAssertions;
using LastHour.BuildingBlocks.Application.Contracts;
using LastHour.BuildingBlocks.Infrastructure.HealthChecks;
using LastHour.BuildingBlocks.Infrastructure.Persistence;
using LastHour.BuildingBlocks.Infrastructure.Persistence.Idempotency;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace LastHour.BuildingBlocks.Infrastructure.Tests.HealthChecks;

public class ConsumerInboxHealthCheckTests
{
    private readonly FixedClock _clock = new() { UtcNow = new DateTime(2026, 8, 19, 12, 0, 0, DateTimeKind.Utc) };
    private readonly HealthChecksOptions _options = new();

    [Fact]
    public async Task CheckHealthAsync_WhenInboxEmpty_ReturnsHealthy()
    {
        using LastHourDbContext dbContext = CreateDbContext();
        var healthCheck = new ConsumerInboxHealthCheck(dbContext, _clock, Options.Create(_options));

        HealthCheckResult result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        result.Status.Should().Be(HealthStatus.Healthy);
        result.Data["active_lease_count"].Should().Be(0);
        result.Data["stalled_expired_lease_count"].Should().Be(0);
    }

    [Fact]
    public async Task CheckHealthAsync_WhenActiveLeasesOnly_ReturnsHealthy()
    {
        using LastHourDbContext dbContext = CreateDbContext();
        var activeMessage = ConsumerInboxMessage.Create(Guid.NewGuid(), "Consumer.A", _clock.UtcNow, TimeSpan.FromMinutes(2));
        dbContext.ConsumerInboxMessages.Add(activeMessage);
        await dbContext.SaveChangesAsync();

        var healthCheck = new ConsumerInboxHealthCheck(dbContext, _clock, Options.Create(_options));

        HealthCheckResult result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        result.Status.Should().Be(HealthStatus.Healthy);
        result.Data["active_lease_count"].Should().Be(1);
        result.Data["stalled_expired_lease_count"].Should().Be(0);
    }

    [Fact]
    public async Task CheckHealthAsync_WhenStalledExpiredLeasesExceedWarningThreshold_ReturnsDegraded()
    {
        using LastHourDbContext dbContext = CreateDbContext();
        _options.ConsumerInbox.WarningStalledLeaseCount = 2;
        _options.ConsumerInbox.CriticalStalledLeaseCount = 10;

        for (int i = 0; i < 3; i++)
        {
            var stalledMessage = ConsumerInboxMessage.Create(Guid.NewGuid(), $"Consumer.{i}", _clock.UtcNow.AddMinutes(-5), TimeSpan.FromMinutes(2));
            dbContext.ConsumerInboxMessages.Add(stalledMessage);
        }
        await dbContext.SaveChangesAsync();

        var healthCheck = new ConsumerInboxHealthCheck(dbContext, _clock, Options.Create(_options));

        HealthCheckResult result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        result.Status.Should().Be(HealthStatus.Degraded);
        result.Data["stalled_expired_lease_count"].Should().Be(3);
    }

    [Fact]
    public async Task CheckHealthAsync_WhenStalledExpiredLeasesExceedCriticalThreshold_ReturnsUnhealthy()
    {
        using LastHourDbContext dbContext = CreateDbContext();
        _options.ConsumerInbox.WarningStalledLeaseCount = 2;
        _options.ConsumerInbox.CriticalStalledLeaseCount = 5;

        for (int i = 0; i < 6; i++)
        {
            var stalledMessage = ConsumerInboxMessage.Create(Guid.NewGuid(), $"Consumer.{i}", _clock.UtcNow.AddMinutes(-5), TimeSpan.FromMinutes(2));
            dbContext.ConsumerInboxMessages.Add(stalledMessage);
        }
        await dbContext.SaveChangesAsync();

        var healthCheck = new ConsumerInboxHealthCheck(dbContext, _clock, Options.Create(_options));

        HealthCheckResult result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        result.Status.Should().Be(HealthStatus.Unhealthy);
        result.Data["stalled_expired_lease_count"].Should().Be(6);
    }

    [Fact]
    public async Task CheckHealthAsync_WhenOldestStalledAgeExceedsCriticalThreshold_ReturnsUnhealthy()
    {
        using LastHourDbContext dbContext = CreateDbContext();
        _options.ConsumerInbox.WarningOldestStalledAge = TimeSpan.FromMinutes(5);
        _options.ConsumerInbox.CriticalOldestStalledAge = TimeSpan.FromMinutes(20);

        var ancientStalledMessage = ConsumerInboxMessage.Create(Guid.NewGuid(), "Consumer.Ancient", _clock.UtcNow.AddMinutes(-60), TimeSpan.FromMinutes(2));
        dbContext.ConsumerInboxMessages.Add(ancientStalledMessage);
        await dbContext.SaveChangesAsync();

        var healthCheck = new ConsumerInboxHealthCheck(dbContext, _clock, Options.Create(_options));

        HealthCheckResult result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        result.Status.Should().Be(HealthStatus.Unhealthy);
    }

    private static LastHourDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<LastHourDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new LastHourDbContext(options);
    }

    private sealed class FixedClock : IClock
    {
        public DateTime UtcNow { get; set; }
    }
}
