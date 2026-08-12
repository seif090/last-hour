using System.Reflection;
using LastHour.BuildingBlocks.Infrastructure.Persistence;
using LastHour.BuildingBlocks.Infrastructure.Persistence.Interceptors;
using LastHour.BuildingBlocks.Infrastructure.Persistence.Outbox;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace LastHour.Api.Tests.Support;

/// <summary>
/// API host factory for the registration endpoint tests. It inherits the shared host
/// configuration (database initialization, outbox and Redis disabled) and additionally swaps
/// the persistence provider from PostgreSQL to a dedicated SQLite database so the full
/// HTTP → pipeline → database path can be exercised without a live PostgreSQL instance. Each
/// factory instance owns its own database file, which is deleted when the factory is disposed.
/// </summary>
public sealed class SqliteApiFactory : LastHourApiFactory
{
    private readonly string _connectionString;
    private readonly string _databaseFile;

    /// <summary>
    /// Initializes a new instance of the <see cref="SqliteApiFactory"/> class.
    /// </summary>
    /// <param name="connectionString">The SQLite connection string to persist against.</param>
    public SqliteApiFactory(string connectionString)
    {
        _connectionString = connectionString;
        _databaseFile = new SqliteConnectionStringBuilder(connectionString).DataSource;
    }

    /// <inheritdoc/>
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);

        builder.ConfigureServices(services =>
        {
            // Replace the PostgreSQL-backed context with a SQLite-backed one. The interceptors
            // are scoped services, so the context is created through a scoped factory rather
            // than an options configuration, which is resolved from the root provider.
            services.RemoveAll<IDbContextOptionsConfiguration<LastHourDbContext>>();
            services.RemoveAll<DbContextOptions<LastHourDbContext>>();
            services.RemoveAll<LastHourDbContext>();
            services.AddScoped(serviceProvider =>
            {
                AuditInterceptor audit = serviceProvider.GetRequiredService<AuditInterceptor>();
                SoftDeleteInterceptor softDelete = serviceProvider.GetRequiredService<SoftDeleteInterceptor>();
                DomainEventsInterceptor domainEvents = serviceProvider.GetRequiredService<DomainEventsInterceptor>();
                Assembly[] configurationAssemblies = serviceProvider.GetRequiredService<Assembly[]>();

                DbContextOptions<LastHourDbContext> options = new DbContextOptionsBuilder<LastHourDbContext>()
                    .UseSqlite(_connectionString)
                    .AddInterceptors(audit, softDelete, domainEvents)
                    .Options;

                return new LastHourDbContext(options, configurationAssemblies);
            });
        });
    }

    /// <summary>
    /// Creates the database schema for the test database.
    /// </summary>
    public void EnsureSchemaCreated()
    {
        using IServiceScope scope = Services.CreateScope();
        scope.ServiceProvider
            .GetRequiredService<LastHourDbContext>()
            .Database
            .EnsureCreated();
    }

    /// <inheritdoc/>
    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing && File.Exists(_databaseFile))
        {
            // Microsoft.Data.Sqlite pools connections by default, which keeps the file open;
            // clearing the pools releases the handle before the database file is removed.
            SqliteConnection.ClearAllPools();
            File.Delete(_databaseFile);
        }
    }
}
