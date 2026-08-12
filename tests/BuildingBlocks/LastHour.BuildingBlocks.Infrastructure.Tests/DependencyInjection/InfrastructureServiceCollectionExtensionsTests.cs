using LastHour.BuildingBlocks.Application.Contracts;
using LastHour.BuildingBlocks.Infrastructure.DependencyInjection;
using LastHour.BuildingBlocks.Infrastructure.Persistence;
using LastHour.BuildingBlocks.Infrastructure.Persistence.Outbox;
using LastHour.BuildingBlocks.Infrastructure.Security;
using LastHour.BuildingBlocks.Infrastructure.Tests.Persistence.TestSupport;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace LastHour.BuildingBlocks.Infrastructure.Tests.DependencyInjection;

public class InfrastructureServiceCollectionExtensionsTests
{
    [Fact]
    public void AddLastHourInfrastructure_PostgresOptions_AppliesPoolingToDataSourceConnectionString()
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Postgres:ConnectionString"] = "Host=localhost;Database=last_hour;Username=app;Password=secret",
                ["Postgres:MaxPoolSize"] = "50",
                ["Postgres:MinPoolSize"] = "2",
                ["Postgres:ConnectionIdleLifetime"] = "00:05:00",
                ["Postgres:ConnectionPruningInterval"] = "00:01:00",
                ["Postgres:ConnectionTimeout"] = "00:00:10",
            })
            .Build();

        using ServiceProvider provider = BuildProvider(configuration);

        NpgsqlDataSource dataSource = provider.GetRequiredService<NpgsqlDataSource>();
        var parsed = new NpgsqlConnectionStringBuilder(dataSource.ConnectionString);

        Assert.Equal(50, parsed.MaxPoolSize);
        Assert.Equal(2, parsed.MinPoolSize);
        Assert.Equal(300, parsed.ConnectionIdleLifetime);
        Assert.Equal(60, parsed.ConnectionPruningInterval);
        Assert.Equal(10, parsed.Timeout);
    }

    [Fact]
    public void AddLastHourInfrastructure_PostgresOptions_UsesConfiguredConnectionString()
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Postgres:ConnectionString"] = "Host=db.internal;Port=5433;Database=custom",
            })
            .Build();

        using ServiceProvider provider = BuildProvider(configuration);

        NpgsqlDataSource dataSource = provider.GetRequiredService<NpgsqlDataSource>();
        var parsed = new NpgsqlConnectionStringBuilder(dataSource.ConnectionString);

        Assert.Equal("db.internal", parsed.Host);
        Assert.Equal(5433, parsed.Port);
        Assert.Equal("custom", parsed.Database);
    }

    [Fact]
    public void AddLastHourInfrastructure_ConfiguresNpgsqlProviderAndInterceptors()
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Postgres:ConnectionString"] = "Host=localhost;Database=last_hour",
            })
            .Build();

        using ServiceProvider provider = BuildProvider(configuration);
        using IServiceScope scope = provider.CreateScope();

        var options = scope.ServiceProvider.GetRequiredService<DbContextOptions<LastHourDbContext>>();

        Assert.Contains(
            options.Extensions,
            extension => extension.GetType().Assembly.FullName!.StartsWith("Npgsql.EntityFrameworkCore.PostgreSQL", StringComparison.Ordinal));
        Assert.Equal(
            3,
            options.Extensions.OfType<CoreOptionsExtension>().Single().Interceptors?.Count() ?? 0);
    }

    [Fact]
    public void AddLastHourInfrastructure_RegistersArgon2PasswordHasher()
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Postgres:ConnectionString"] = "Host=localhost;Database=last_hour",
            })
            .Build();

        using ServiceProvider provider = BuildProvider(configuration);

        IPasswordHasher hasher = provider.GetRequiredService<IPasswordHasher>();

        Assert.IsType<Argon2PasswordHasher>(hasher);
    }

    [Fact]
    public void AddLastHourInfrastructure_RegistersJwtAccessTokenGenerator()
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Postgres:ConnectionString"] = "Host=localhost;Database=last_hour",
                ["Jwt:Issuer"] = "https://lasthour.test",
                ["Jwt:Audience"] = "lasthour-test",
                ["Jwt:SigningKey"] = "0123456789abcdef0123456789abcdef0123456789abcdef",
            })
            .Build();

        using ServiceProvider provider = BuildProvider(configuration);

        IAccessTokenGenerator generator = provider.GetRequiredService<IAccessTokenGenerator>();

        Assert.IsType<JwtAccessTokenGenerator>(generator);
    }

    [Fact]
    public void AddLastHourInfrastructure_LastHourDbContext_CanBeResolved()
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Postgres:ConnectionString"] = "Host=localhost;Database=last_hour",
            })
            .Build();

        using ServiceProvider provider = BuildProvider(configuration);
        using IServiceScope scope = provider.CreateScope();

        LastHourDbContext context = scope.ServiceProvider.GetRequiredService<LastHourDbContext>();

        Assert.NotNull(context);
        Assert.NotNull(context.Model.FindEntityType(typeof(OutboxMessage)));
    }

    private static ServiceProvider BuildProvider(IConfiguration configuration)
    {
        var services = new ServiceCollection();
        services.AddLastHourInfrastructure(configuration, typeof(TestAggregate).Assembly);
        return services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateScopes = true,
        });
    }
}
