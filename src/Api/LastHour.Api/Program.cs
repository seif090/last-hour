using LastHour.Api.Caching.OutputCache;
using LastHour.Api.Compression;
using LastHour.Api.DependencyInjection;
using LastHour.Api.Endpoints;
using LastHour.Api.Middleware;
using LastHour.Api.Observability.Auditing;
using LastHour.Api.Observability.Logging;
using LastHour.Api.Observability.RequestLogging;
using LastHour.Api.OpenApi;
using LastHour.Api.RateLimiting;
using LastHour.Api.Secrets;
using LastHour.Api.Security.Cors;
using LastHour.Api.Security.ForwardedHeaders;
using LastHour.Api.Security.SecurityHeaders;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console(formatProvider: null)
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseLastHourSerilog();

    builder.Configuration.AddLastHourSecrets();

    builder.Services.AddLastHourApi(builder.Configuration);

    var app = builder.Build();

    app.UseLastHourExceptionMiddleware();

    if (app.Environment.IsDevelopment())
    {
        app.UseLastHourSwagger();
    }

    app.UseLastHourForwardedHeaders();
    app.UseHttpsRedirection();
    app.UseLastHourSecurityHeaders();
    app.UseLastHourCors();
    app.UseLastHourResponseCompression();
    app.UseLastHourRequestLogging();
    app.UseLastHourAuditLogging();
    app.UseRouting();
    app.UseAuthentication();
    app.UseAuthorization();
    app.UseLastHourRateLimiting();
    app.UseLastHourOutputCache();

    app.MapLastHourEndpoints();

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "LastHour.Api terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

public partial class Program
{
}
