using LastHour.BuildingBlocks.Application.Contracts;
using LastHour.BuildingBlocks.Infrastructure.Persistence;
using LastHour.BuildingBlocks.Infrastructure.Persistence.Interceptors;
using LastHour.BuildingBlocks.Infrastructure.Persistence.Outbox;
using LastHour.Modules.Identity.Domain.Users;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace LastHour.Modules.Identity.Tests.Persistence.TestSupport;

/// <summary>
/// Creates <see cref="LastHourDbContext"/> instances for Identity persistence tests using
/// SQLite. SQLite is used instead of the InMemory provider because it is relational: unique
/// constraints and concurrency tokens are honored, which the Identity mapping relies on.
/// Contexts can be built either over a caller-owned connection (per-test isolation) or over a
/// shared-cache connection string (required when several contexts must see the same database,
/// for example concurrency tests).
/// </summary>
public static class TestIdentityPersistence
{
    public static readonly DateTime FixedUtcNow = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

    public const string CurrentUserId = "identity-test-user";

    /// <summary>
    /// Creates a <see cref="LastHourDbContext"/> over a caller-owned SQLite connection.
    /// </summary>
    /// <param name="connection">The open SQLite connection to use.</param>
    /// <returns>A configured context.</returns>
    public static LastHourDbContext CreateSqliteContext(SqliteConnection connection)
        => CreateSqliteContextCore(options => options.UseSqlite(connection));

    /// <summary>
    /// Creates a <see cref="LastHourDbContext"/> over a connection string. Each context opens
    /// its own connection, which is required when multiple contexts must operate concurrently
    /// against the same shared-cache database.
    /// </summary>
    /// <param name="connectionString">The SQLite connection string to use.</param>
    /// <returns>A configured context.</returns>
    public static LastHourDbContext CreateSqliteContext(string connectionString)
        => CreateSqliteContextCore(options => options.UseSqlite(connectionString));

    /// <summary>
    /// Creates a fresh per-test in-memory SQLite connection.
    /// </summary>
    /// <returns>An open connection backed by a private in-memory database.</returns>
    public static SqliteConnection CreateOpenConnection()
    {
        SqliteConnection connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        return connection;
    }

    /// <summary>
    /// Creates a shared-cache in-memory connection string that keeps its database alive while
    /// <paramref name="anchor"/> is open and allows several concurrent connections.
    /// </summary>
    /// <param name="anchor">The connection to open and hold for the lifetime of the test.</param>
    /// <returns>A connection string for the shared database.</returns>
    public static string CreateSharedConnectionString(out SqliteConnection anchor)
    {
        var builder = new SqliteConnectionStringBuilder
        {
            DataSource = $"file:{Guid.NewGuid():N}?mode=memory&cache=shared",
            Cache = SqliteCacheMode.Shared,
            DefaultTimeout = 10,
        };

        anchor = new SqliteConnection(builder.ConnectionString);
        anchor.Open();
        return builder.ConnectionString;
    }

    /// <summary>
    /// Creates a context with a created schema over a caller-owned connection.
    /// </summary>
    /// <param name="connection">The open SQLite connection to use.</param>
    /// <returns>A configured context whose schema is created.</returns>
    public static async Task<LastHourDbContext> CreateContextAsync(SqliteConnection connection)
    {
        LastHourDbContext context = CreateSqliteContext(connection);
        await context.Database.EnsureCreatedAsync();
        return context;
    }

    private static LastHourDbContext CreateSqliteContextCore(
        Action<DbContextOptionsBuilder<LastHourDbContext>> useProvider)
    {
        IServiceProvider services = new ServiceCollection()
            .AddEntityFrameworkSqlite()
            .BuildServiceProvider();

        var optionsBuilder = new DbContextOptionsBuilder<LastHourDbContext>()
            .UseInternalServiceProvider(services)
            .AddInterceptors(
                new AuditInterceptor(new TestClock(), new TestCurrentUser()),
                new SoftDeleteInterceptor(new TestClock(), new TestCurrentUser()),
                new DomainEventsInterceptor());

        useProvider(optionsBuilder);

        return new LastHourDbContext(optionsBuilder.Options, new[] { typeof(User).Assembly });
    }

    /// <summary>
    /// A clock that always reports the fixed test instant.
    /// </summary>
    public sealed class TestClock : IClock
    {
        /// <inheritdoc/>
        public DateTime UtcNow => FixedUtcNow;
    }

    /// <summary>
    /// A current-user context that reports the fixed test actor.
    /// </summary>
    public sealed class TestCurrentUser : ICurrentUser
    {
        /// <inheritdoc/>
        public bool IsAuthenticated => true;

        /// <inheritdoc/>
        public string? UserId => CurrentUserId;

        /// <inheritdoc/>
        public string? Name => "identity-test-user";

        /// <inheritdoc/>
        public IReadOnlyCollection<string> Roles { get; } = Array.Empty<string>();

        /// <inheritdoc/>
        public bool IsInRole(string role) => false;
    }
}
