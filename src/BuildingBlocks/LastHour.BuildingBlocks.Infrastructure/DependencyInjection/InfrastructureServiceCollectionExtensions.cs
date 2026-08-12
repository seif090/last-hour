using System.Reflection;
using LastHour.BuildingBlocks.Application.Contracts;
using LastHour.BuildingBlocks.Infrastructure.Events;
using LastHour.BuildingBlocks.Infrastructure.HealthChecks;
using LastHour.BuildingBlocks.Infrastructure.Persistence;
using LastHour.BuildingBlocks.Infrastructure.Persistence.Configuration;
using LastHour.BuildingBlocks.Infrastructure.Persistence.Interceptors;
using LastHour.BuildingBlocks.Infrastructure.Persistence.Outbox;
using LastHour.BuildingBlocks.Infrastructure.Persistence.Repositories;
using LastHour.BuildingBlocks.Infrastructure.Persistence.Seeding;
using LastHour.BuildingBlocks.Infrastructure.Persistence.UnitOfWork;
using LastHour.BuildingBlocks.Infrastructure.Security;
using LastHour.BuildingBlocks.Infrastructure.Time;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Npgsql;
using StackExchange.Redis;

namespace LastHour.BuildingBlocks.Infrastructure.DependencyInjection;

/// <summary>
/// Contains extension methods that register the entire infrastructure layer: PostgreSQL and
/// EF Core with its interceptors, the unit of work and repositories, time and ambient context
/// services, the event dispatcher and outbox pipeline, database initialization and health
/// checks. Configuration is validated at startup so misconfiguration fails fast.
/// </summary>
public static class InfrastructureServiceCollectionExtensions
{
    private static readonly string[] DatabaseHealthCheckTags = { "database", "ready" };

    private static readonly string[] DiskHealthCheckTags = { "disk", "ready" };

    private static readonly string[] MemoryHealthCheckTags = { "memory", "ready" };

    private static readonly string[] RedisHealthCheckTags = { "redis", "ready" };

    private static readonly string[] SelfHealthCheckTags = { "self", "live" };

    /// <summary>
    /// Registers the infrastructure layer with the dependency injection container.
    /// </summary>
    /// <param name="services">The service collection to register into.</param>
    /// <param name="configuration">The application configuration used to bind options.</param>
    /// <param name="entityConfigurationAssemblies">Assemblies whose EF Core entity
    /// configurations are applied to the database model. Modules register their own assemblies
    /// here; the base model (outbox) is always applied.</param>
    /// <returns>The same service collection, so calls can be chained.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="services"/> or
    /// <paramref name="configuration"/> is <see langword="null"/>.</exception>
    public static IServiceCollection AddLastHourInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration,
        params Assembly[] entityConfigurationAssemblies)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddSingleton(entityConfigurationAssemblies.Distinct().ToArray());

        RegisterOptions(services, configuration);
        RegisterPersistence(services);
        RegisterServices(services, configuration);

        return services;
    }

    private static void RegisterOptions(IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<PostgresOptions>()
            .Bind(configuration.GetSection(PostgresOptions.SectionName))
            .Configure(options =>
            {
                if (string.IsNullOrWhiteSpace(options.ConnectionString))
                {
                    options.ConnectionString = configuration.GetConnectionString("Postgres") ?? string.Empty;
                }
            })
            .ValidateOnStart();
        services.AddSingleton<IValidateOptions<PostgresOptions>, PostgresOptionsValidator>();

        services.AddOptions<OutboxOptions>()
            .Bind(configuration.GetSection(OutboxOptions.SectionName))
            .ValidateOnStart();
        services.AddSingleton<IValidateOptions<OutboxOptions>, OutboxOptionsValidator>();

        services.AddOptions<HealthChecksOptions>()
            .Bind(configuration.GetSection(HealthChecksOptions.SectionName))
            .ValidateOnStart();
        services.AddSingleton<IValidateOptions<HealthChecksOptions>, HealthChecksOptionsValidator>();

        services.AddOptions<DatabaseInitializerOptions>()
            .Bind(configuration.GetSection(DatabaseInitializerOptions.SectionName));

        services.AddOptions<PasswordHashingOptions>()
            .Bind(configuration.GetSection(PasswordHashingOptions.SectionName))
            .ValidateOnStart();
        services.AddSingleton<IValidateOptions<PasswordHashingOptions>, PasswordHashingOptionsValidator>();

        services.AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName))
            .ValidateOnStart();
        services.AddSingleton<IValidateOptions<JwtOptions>, JwtOptionsValidator>();
    }

    private static void RegisterPersistence(IServiceCollection services)
    {
        services.AddScoped<AuditInterceptor>();
        services.AddScoped<SoftDeleteInterceptor>();
        services.AddScoped<DomainEventsInterceptor>();

        services.AddSingleton<NpgsqlDataSource>(CreateDataSource);

        services.AddScoped<IDbContextOptionsConfiguration<LastHourDbContext>, LastHourDbContextOptionsSetup>();
        services.AddDbContext<LastHourDbContext>(optionsLifetime: ServiceLifetime.Scoped);

        services.AddScoped<IUnitOfWork, EfUnitOfWork>();
        services.AddScoped(typeof(IRepository<>), typeof(EfRepository<>));
        services.AddScoped(typeof(IReadRepository<,>), typeof(EfReadRepository<,>));
        services.AddScoped(typeof(ITrackedRepository<,>), typeof(EfTrackedRepository<,>));

        services.AddScoped<SeederExecutor>();
    }

    private static NpgsqlDataSource CreateDataSource(IServiceProvider serviceProvider)
    {
        PostgresOptions postgres = serviceProvider.GetRequiredService<IOptions<PostgresOptions>>().Value;

        var connectionStringBuilder = new NpgsqlConnectionStringBuilder(postgres.ConnectionString)
        {
            MaxPoolSize = postgres.MaxPoolSize,
            MinPoolSize = postgres.MinPoolSize,
            ConnectionIdleLifetime = (int)postgres.ConnectionIdleLifetime.TotalSeconds,
            ConnectionPruningInterval = (int)postgres.ConnectionPruningInterval.TotalSeconds,
            Timeout = (int)postgres.ConnectionTimeout.TotalSeconds,
        };

        var dataSourceBuilder = new NpgsqlDataSourceBuilder(connectionStringBuilder.ConnectionString);

        if (postgres.EnableDynamicJson)
        {
            dataSourceBuilder.EnableDynamicJson();
        }

        return dataSourceBuilder.Build();
    }

    private static void RegisterServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<IClock, SystemClock>();
        services.AddSingleton<IDateTimeProvider, SystemDateTimeProvider>();
        services.AddSingleton<IPasswordHasher, Argon2PasswordHasher>();
        services.AddSingleton<IAccessTokenGenerator, JwtAccessTokenGenerator>();
        services.AddScoped<ICurrentUser, DefaultCurrentUser>();
        services.AddScoped<ICurrentTenant, DefaultCurrentTenant>();

        services.AddScoped<IEventDispatcher, EventDispatcher>();
        services.AddScoped<IPublisher, OutboxPublisher>();

        services.AddHostedService<OutboxProcessor>();
        services.AddHostedService<DatabaseInitializer>();

        IHealthChecksBuilder healthChecks = services.AddHealthChecks()
            .AddCheck<SelfHealthCheck>("self", tags: SelfHealthCheckTags)
            .AddDbContextCheck<LastHourDbContext>("postgres", tags: DatabaseHealthCheckTags)
            .AddCheck<DiskHealthCheck>("disk", tags: DiskHealthCheckTags)
            .AddCheck<MemoryHealthCheck>("memory", tags: MemoryHealthCheckTags);

        string? redisConnectionString = configuration[$"{HealthChecksOptions.SectionName}:Redis:ConnectionString"];
        if (!string.IsNullOrWhiteSpace(redisConnectionString))
        {
            services.AddSingleton<IConnectionMultiplexer>(_ =>
            {
                ConfigurationOptions redisOptions = ConfigurationOptions.Parse(redisConnectionString);
                redisOptions.AbortOnConnectFail = false;
                return ConnectionMultiplexer.Connect(redisOptions);
            });
            services.AddSingleton<IRedisHealthProbe, RedisHealthProbe>();
            healthChecks.AddCheck<RedisHealthCheck>("redis", tags: RedisHealthCheckTags);
        }
    }
}
