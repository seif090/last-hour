using System.Data.Common;
using LastHour.BuildingBlocks.Application.Contracts;
using LastHour.BuildingBlocks.Infrastructure.Persistence;
using LastHour.BuildingBlocks.Infrastructure.Persistence.Interceptors;
using LastHour.BuildingBlocks.Infrastructure.Persistence.Outbox;
using LastHour.BuildingBlocks.Infrastructure.Persistence.Repositories;
using LastHour.Modules.Merchant.Application.Persistence;
using LastHour.Modules.Merchant.Domain.Merchants;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using MerchantAggregate = LastHour.Modules.Merchant.Domain.Merchants.Merchant;

namespace LastHour.Modules.Merchant.Tests.Persistence.TestSupport;

/// <summary>
/// Creates <see cref="LastHourDbContext"/> and repository instances for Merchant persistence tests using SQLite.
/// </summary>
public static class TestMerchantPersistence
{
    public static readonly DateTime FixedUtcNow = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

    public const string CurrentUserId = "merchant-test-user";

    /// <summary>
    /// Creates a <see cref="LastHourDbContext"/> over a caller-owned SQLite connection.
    /// </summary>
    /// <param name="connection">The open SQLite connection to use.</param>
    /// <param name="additionalInterceptors">Optional additional interceptors to register on the context.</param>
    /// <returns>A configured context.</returns>
    public static LastHourDbContext CreateSqliteContext(
        SqliteConnection connection,
        params IInterceptor[] additionalInterceptors)
        => CreateSqliteContextCore(options => options.UseSqlite(connection), additionalInterceptors);

    /// <summary>
    /// Creates a fresh per-test in-memory SQLite connection.
    /// </summary>
    /// <returns>An open connection backed by a private in-memory database.</returns>
    public static SqliteConnection CreateOpenConnection()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        return connection;
    }

    /// <summary>
    /// Creates a context with a created schema over a caller-owned connection.
    /// </summary>
    /// <param name="connection">The open SQLite connection to use.</param>
    /// <param name="additionalInterceptors">Optional additional interceptors to register on the context.</param>
    /// <returns>A configured context whose schema is created.</returns>
    public static async Task<LastHourDbContext> CreateContextAsync(
        SqliteConnection connection,
        params IInterceptor[] additionalInterceptors)
    {
        LastHourDbContext context = CreateSqliteContext(connection, additionalInterceptors);
        await context.Database.EnsureCreatedAsync();
        return context;
    }

    /// <summary>
    /// Creates a <see cref="MerchantRepository"/> instance over the given database context.
    /// </summary>
    /// <param name="context">The database context to use.</param>
    /// <returns>A configured repository instance.</returns>
    public static IMerchantRepository CreateRepository(LastHourDbContext context)
    {
        var writeRepo = new EfRepository<MerchantAggregate>(context);
        var readRepo = new EfReadRepository<MerchantAggregate, MerchantId>(context);
        var trackedRepo = new EfTrackedRepository<MerchantAggregate, MerchantId>(context);
        return new MerchantRepository(writeRepo, readRepo, trackedRepo);
    }

    private static LastHourDbContext CreateSqliteContextCore(
        Action<DbContextOptionsBuilder<LastHourDbContext>> useProvider,
        params IInterceptor[] additionalInterceptors)
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

        if (additionalInterceptors.Length > 0)
        {
            optionsBuilder.AddInterceptors(additionalInterceptors);
        }

        useProvider(optionsBuilder);

        return new LastHourDbContext(optionsBuilder.Options, new[] { typeof(MerchantAggregate).Assembly });
    }

    /// <summary>
    /// A command interceptor that records all executed SQL statements.
    /// </summary>
    public sealed class TestCommandInterceptor : DbCommandInterceptor
    {
        private readonly List<string> _executedCommands = new();

        /// <summary>
        /// Gets the list of executed SQL commands.
        /// </summary>
        public IReadOnlyList<string> ExecutedCommands => _executedCommands;

        /// <inheritdoc/>
        public override InterceptionResult<DbDataReader> ReaderExecuting(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<DbDataReader> result)
        {
            _executedCommands.Add(command.CommandText);
            return base.ReaderExecuting(command, eventData, result);
        }

        /// <inheritdoc/>
        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<DbDataReader> result,
            CancellationToken cancellationToken = default)
        {
            _executedCommands.Add(command.CommandText);
            return base.ReaderExecutingAsync(command, eventData, result, cancellationToken);
        }

        /// <inheritdoc/>
        public override InterceptionResult<int> NonQueryExecuting(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<int> result)
        {
            _executedCommands.Add(command.CommandText);
            return base.NonQueryExecuting(command, eventData, result);
        }

        /// <inheritdoc/>
        public override ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            _executedCommands.Add(command.CommandText);
            return base.NonQueryExecutingAsync(command, eventData, result, cancellationToken);
        }
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
        public string? Name => "merchant-test-user";

        /// <inheritdoc/>
        public IReadOnlyCollection<string> Roles { get; } = Array.Empty<string>();

        /// <inheritdoc/>
        public bool IsInRole(string role) => false;
    }

    /// <summary>
    /// A SaveChanges interceptor that throws an exception during saving, used to simulate database commit failures.
    /// </summary>
    public sealed class ThrowingSaveChangesInterceptor : SaveChangesInterceptor
    {
        private readonly Exception _exceptionToThrow;

        /// <summary>
        /// Initializes a new instance of the <see cref="ThrowingSaveChangesInterceptor"/> class.
        /// </summary>
        public ThrowingSaveChangesInterceptor(Exception? exceptionToThrow = null)
        {
            _exceptionToThrow = exceptionToThrow ?? new InvalidOperationException("Simulated database failure during SaveChanges.");
        }

        /// <inheritdoc/>
        public override InterceptionResult<int> SavingChanges(
            DbContextEventData eventData,
            InterceptionResult<int> result)
        {
            throw _exceptionToThrow;
        }

        /// <inheritdoc/>
        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            throw _exceptionToThrow;
        }
    }
}
