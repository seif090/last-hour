using LastHour.Api.Observability.Logging;
using Microsoft.Extensions.Options;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace LastHour.Api.Observability.Telemetry;

/// <summary>
/// Registers OpenTelemetry: a service resource (name, version, environment), ASP.NET Core and
/// HTTP client instrumentation for tracing and metrics, the application/EF Core/Npgsql trace
/// sources, and configurable exporters (console for local development, OTLP for production
/// collectors). Everything is controlled by the <c>OpenTelemetry</c> configuration section so
/// operators can point traces and metrics at any backend without a deployment.
/// </summary>
public static class OpenTelemetryServiceCollectionExtensions
{
    /// <summary>
    /// Registers OpenTelemetry tracing, metrics and exporters from configuration.
    /// </summary>
    /// <param name="services">The service collection to register into.</param>
    /// <param name="configuration">The application configuration used to bind options.</param>
    /// <returns>The same service collection, so calls can be chained.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="services"/> or <paramref name="configuration"/>
    /// is <see langword="null"/>.</exception>
    public static IServiceCollection AddLastHourOpenTelemetry(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddOptions<OpenTelemetryOptions>()
            .Bind(configuration.GetSection(OpenTelemetryOptions.SectionName))
            .ValidateOnStart();
        services.AddSingleton<IValidateOptions<OpenTelemetryOptions>, OpenTelemetryOptionsValidator>();

        OpenTelemetryOptions options = configuration.GetSection(OpenTelemetryOptions.SectionName).Get<OpenTelemetryOptions>() ?? new OpenTelemetryOptions();
        if (!options.Enabled)
        {
            return services;
        }

        string environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";

        OpenTelemetryBuilder builder = services.AddOpenTelemetry()
            .ConfigureResource(resource => resource
                .AddService(
                    serviceName: options.ServiceName,
                    serviceVersion: options.ServiceVersion ?? ApplicationVersion.Get())
                .AddAttributes(new Dictionary<string, object>
                {
                    ["deployment.environment"] = environment,
                }));

        if (options.TracingEnabled)
        {
            builder = builder.WithTracing(tracing => tracing
                .AddSource("LastHour.Api")
                .AddSource("LastHour.Outbox")
                .AddSource("LastHour.ConsumerIdempotency")
                .AddSource("Microsoft.EntityFrameworkCore")
                .AddSource("Npgsql")
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation());

            if (options.ConsoleExporterEnabled)
            {
                builder = builder.WithTracing(tracing => tracing.AddConsoleExporter());
            }

            if (options.UseOtlpExporter)
            {
                string endpoint = options.OtlpEndpoint!;
                builder = builder.WithTracing(tracing => tracing.AddOtlpExporter(exporter => exporter.Endpoint = new Uri(endpoint)));
            }
        }

        if (options.MetricsEnabled)
        {
            builder = builder.WithMetrics(metrics => metrics
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddMeter(
                    "Microsoft.AspNetCore.Hosting",
                    "Microsoft.AspNetCore.Server.Kestrel",
                    "System.Net.Http",
                    "Npgsql",
                    "LastHour.Outbox",
                    "LastHour.ConsumerIdempotency"));

            if (options.ConsoleExporterEnabled)
            {
                builder = builder.WithMetrics(metrics => metrics.AddConsoleExporter());
            }

            if (options.UseOtlpExporter)
            {
                string endpoint = options.OtlpEndpoint!;
                builder = builder.WithMetrics(metrics => metrics.AddOtlpExporter(exporter => exporter.Endpoint = new Uri(endpoint)));
            }
        }

        return services;
    }
}
