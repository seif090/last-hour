using LastHour.BuildingBlocks.Application.Contracts;
using LastHour.BuildingBlocks.Infrastructure.Persistence;
using LastHour.BuildingBlocks.Infrastructure.Persistence.Interceptors;
using LastHour.BuildingBlocks.Infrastructure.Persistence.Outbox;
using LastHour.Modules.Catalog.Domain.Products;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace LastHour.Modules.Catalog.Tests.Persistence.TestSupport;

/// <summary>
/// Creates <see cref="LastHourDbContext"/> instances for Catalog persistence tests using SQLite.
/// </summary>
public static class TestCatalogPersistence
{
    public static readonly DateTime FixedUtcNow = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

    public const string CurrentUserId = "catalog-test-user";

    /// <summary>
    /// Creates a <see cref="LastHourDbContext"/> over a caller-owned SQLite connection.
    /// </summary>
    /// <param name="connection">The open SQLite connection to use.</param>
    /// <returns>A configured context.</returns>
    public static LastHourDbContext CreateSqliteContext(SqliteConnection connection)
        => CreateSqliteContextCore(options => options.UseSqlite(connection));

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

        return new LastHourDbContext(optionsBuilder.Options, new[] { typeof(Product).Assembly });
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
        public string? Name => "catalog-test-user";

        /// <inheritdoc/>
        public IReadOnlyCollection<string> Roles { get; } = Array.Empty<string>();

        /// <inheritdoc/>
        public bool IsInRole(string role) => false;
    }
}
