using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace LastHour.Api.Tests.Support;

/// <summary>
/// API host factory used by the integration tests. The infrastructure layer is wired into the
/// composition root, so the shared host disables database initialization and the outbox
/// processor (there is no database in the test environment) and points PostgreSQL at an
/// unreachable port so health checks fail fast and deterministically. The Redis health check
/// is disabled because no Redis instance is available in the test environment.
/// </summary>
public class LastHourApiFactory : WebApplicationFactory<Program>
{
    /// <inheritdoc/>
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["DatabaseInitializer:Enabled"] = "false",
                ["Outbox:Enabled"] = "false",
                ["ConnectionStrings:Postgres"] = "Host=localhost;Port=54329;Database=last_hour;Username=test;Password=test",
                ["Postgres:MaxRetryCount"] = "0",
                ["HealthChecks:Redis:ConnectionString"] = string.Empty,
                ["Jwt:Issuer"] = TestJwt.Issuer,
                ["Jwt:Audience"] = TestJwt.Audience,
                ["Jwt:SigningKey"] = TestJwt.SigningKey,
            });
        });
    }
}
