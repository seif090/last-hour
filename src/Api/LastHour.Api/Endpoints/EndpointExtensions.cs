using LastHour.Api.Endpoints.Authentication;
using LastHour.Api.Endpoints.ProblemDetails;
using LastHour.Api.Endpoints.Registration;
using LastHour.Api.Endpoints.System;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;

namespace LastHour.Api.Endpoints;

/// <summary>
/// Composes the API endpoint surface: feature endpoint modules, controllers and the
/// operational health endpoints. Adding a feature means adding its module call here,
/// keeping <c>Program.cs</c> free of route-table concerns.
/// </summary>
public static class EndpointExtensions
{
    /// <summary>
    /// Maps all LastHour endpoint modules, MVC controllers and health checks onto the route table.
    /// </summary>
    /// <param name="app">The endpoint route builder to extend.</param>
    /// <returns>The same endpoint route builder, so calls can be chained.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="app"/> is <see langword="null"/>.</exception>
    public static IEndpointRouteBuilder MapLastHourEndpoints(this IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapSystemEndpoints();
        app.MapProblemDetailsEndpoints();
        app.MapRegistrationEndpoints();
        app.MapAuthEndpoints();
        app.MapControllers();
        app.MapHealthChecks("/health").DisableRateLimiting();
        app.MapHealthChecks(
            "/health/live",
            new HealthCheckOptions { Predicate = check => check.Tags.Contains("live") }).DisableRateLimiting();
        app.MapHealthChecks(
            "/health/ready",
            new HealthCheckOptions { Predicate = check => check.Tags.Contains("ready") }).DisableRateLimiting();

        return app;
    }
}
