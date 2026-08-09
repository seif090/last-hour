using LastHour.BuildingBlocks.Infrastructure.Persistence;
using LastHour.BuildingBlocks.Infrastructure.Persistence.Interceptors;
using LastHour.BuildingBlocks.Infrastructure.Persistence.Outbox;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace LastHour.BuildingBlocks.Infrastructure.Tests.Persistence.TestSupport;

public static class TestPersistence
{
    public const string UserId = "test-user";

    public static readonly DateTime FixedUtcNow = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

    public static LastHourDbContext CreateContext(string? databaseName = null)
    {
        IServiceProvider services = new ServiceCollection()
            .AddEntityFrameworkInMemoryDatabase()
            .BuildServiceProvider();

        DbContextOptions<LastHourDbContext> options = new DbContextOptionsBuilder<LastHourDbContext>()
            .UseInMemoryDatabase(databaseName ?? Guid.NewGuid().ToString())
            .UseInternalServiceProvider(services)
            .AddInterceptors(
                new AuditInterceptor(new FixedClock(), new FixedCurrentUser()),
                new SoftDeleteInterceptor(new FixedClock(), new FixedCurrentUser()),
                new DomainEventsInterceptor())
            .Options;

        return new LastHourDbContext(options, new[] { typeof(TestAggregate).Assembly });
    }

    public static LastHourDbContext CreateSqliteContext(SqliteConnection connection)
    {
        IServiceProvider services = new ServiceCollection()
            .AddEntityFrameworkSqlite()
            .BuildServiceProvider();

        DbContextOptions<LastHourDbContext> options = new DbContextOptionsBuilder<LastHourDbContext>()
            .UseSqlite(connection)
            .UseInternalServiceProvider(services)
            .AddInterceptors(
                new AuditInterceptor(new FixedClock(), new FixedCurrentUser()),
                new SoftDeleteInterceptor(new FixedClock(), new FixedCurrentUser()),
                new DomainEventsInterceptor())
            .Options;

        return new LastHourDbContext(options, new[] { typeof(TestAggregate).Assembly });
    }
}
