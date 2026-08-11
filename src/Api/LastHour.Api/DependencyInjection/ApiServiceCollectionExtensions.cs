using LastHour.Api.Caching.OutputCache;
using LastHour.Api.Compression;
using LastHour.Api.Middleware;
using LastHour.Api.Observability.Auditing;
using LastHour.Api.Observability.RequestLogging;
using LastHour.Api.Observability.Telemetry;
using LastHour.Api.OpenApi;
using LastHour.Api.ProblemDetails;
using LastHour.Api.RateLimiting;
using LastHour.Api.RequestLimits;
using LastHour.Api.Secrets;
using LastHour.Api.Security.Cors;
using LastHour.Api.Security.ForwardedHeaders;
using LastHour.Api.Security.SecurityHeaders;
using LastHour.Api.Versioning;
using LastHour.BuildingBlocks.Infrastructure.DependencyInjection;

namespace LastHour.Api.DependencyInjection;

/// <summary>
/// Registers the LastHour API surface. The API is the composition root: it wires the
/// infrastructure layer (PostgreSQL and EF Core with its interceptors, the unit of work,
/// repositories, outbox, seeding and health checks), the CQRS pipeline and its options,
/// together with the HTTP surface. Infrastructure is referenced only here, at the composition
/// root; feature endpoints orchestrate requests purely through application abstractions and
/// never touch infrastructure types.
/// </summary>
public static class ApiServiceCollectionExtensions
{
    /// <summary>
    /// Registers the infrastructure layer, MVC controllers, API versioning with per-version
    /// Swagger documents, health checks and the CQRS pipeline with its options.
    /// </summary>
    /// <param name="services">The service collection to register into.</param>
    /// <param name="configuration">The application configuration used to bind options.</param>
    /// <returns>The same service collection, so calls can be chained.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="services"/> or <paramref name="configuration"/>
    /// is <see langword="null"/>.</exception>
    public static IServiceCollection AddLastHourApi(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        // Entity configuration assemblies: each module contributes its own assembly so the
        // shared DbContext discovers its IEntityTypeConfiguration implementations. Modules
        // with no entities yet are omitted; add them when they gain persistence.
        services.AddLastHourInfrastructure(
            configuration,
            typeof(LastHour.Modules.Identity.Domain.Users.User).Assembly);

        services.AddControllers();
        services.AddEndpointsApiExplorer();

        services.AddCqrs();
        services.AddPerformanceBehaviorOptions(configuration);

        services.AddLastHourApiVersioning();
        services.AddLastHourSwagger();
        services.AddLastHourProblemDetails();
        services.AddLastHourResponseCompression(configuration);
        services.AddLastHourOutputCache(configuration);
        services.AddLastHourRateLimiting(configuration);
        services.AddLastHourMiddleware(configuration);
        services.AddLastHourRequestLogging();
        services.AddLastHourAuditLogging(configuration);
        services.AddLastHourOpenTelemetry(configuration);
        services.AddLastHourSecurityHeaders(configuration);
        services.AddLastHourCors(configuration);
        services.AddLastHourForwardedHeaders(configuration);
        services.AddLastHourSecrets(configuration);
        services.AddLastHourRequestLimits(configuration);

        return services;
    }
}
