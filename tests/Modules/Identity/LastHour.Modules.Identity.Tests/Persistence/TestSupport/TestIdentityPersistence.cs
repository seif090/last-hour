using LastHour.BuildingBlocks.Infrastructure.Persistence;
using LastHour.BuildingBlocks.Infrastructure.Persistence.Interceptors;
using LastHour.BuildingBlocks.Infrastructure.Persistence.Outbox;
using LastHour.Modules.Identity.Domain.Users;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace LastHour.Modules.Identity.Tests.Persistence.TestSupport;

/// <summary>
/// Creates <see cref="LastHourDbContext"/> instances for Identity persistence tests using a
/// per-test SQLite in-memory database. SQLite is used instead of the InMemory provider
/// because it is relational: unique constraints and concurrency tokens are honored, which the
/// Identity mapping relies on. Each test owns its connection, so no state is shared.
/// </summary>
public static class TestIdentityPersistence
{
    public static readonly DateTime FixedUtcNow = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

    public const string CurrentUserId = "identity-test-user";

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

        return new LastHourDbContext(options, new[] { typeof(User).Assembly });
    }

    public static SqliteConnection CreateOpenConnection()
    {
        SqliteConnection connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        return connection;
    }

    public static async Task<LastHourDbContext> CreateContextAsync(SqliteConnection connection)
    {
        LastHourDbContext context = CreateSqliteContext(connection);
        await context.Database.EnsureCreatedAsync();
        return context;
    }

    private sealed class FixedClock : LastHour.BuildingBlocks.Application.Contracts.IClock
    {
        public DateTime UtcNow => FixedUtcNow;
    }

    private sealed class FixedCurrentUser : LastHour.BuildingBlocks.Application.Contracts.ICurrentUser
    {
        public bool IsAuthenticated => true;

        public string? UserId => CurrentUserId;

        public string? Name => "identity-test-user";

        public IReadOnlyCollection<string> Roles { get; } = Array.Empty<string>();

        public bool IsInRole(string role) => false;
    }
}
