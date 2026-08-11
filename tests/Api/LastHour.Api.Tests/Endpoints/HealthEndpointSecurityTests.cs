using System.Net;
using LastHour.Api.Tests.Support;
using Microsoft.AspNetCore.Mvc.Testing;

namespace LastHour.Api.Tests.Endpoints;

/// <summary>
/// Verifies the operational health endpoints answer with minimal status text only. They must
/// never disclose connection strings, hostnames, credentials, stack traces or database details,
/// even while a dependency is down. The live endpoint is used for the happy path (it has no
/// checks); readiness exercises the degraded path with PostgreSQL unreachable.
/// </summary>
[Collection("ApiEndpoints")]
public class HealthEndpointSecurityTests : IClassFixture<LastHourApiFactory>
{
    private readonly WebApplicationFactory<Program> _factory;

    /// <summary>
    /// Initializes a new instance of the <see cref="HealthEndpointSecurityTests"/> class.
    /// </summary>
    /// <param name="factory">The shared API host factory provided by xUnit.</param>
    public HealthEndpointSecurityTests(LastHourApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Get_Liveness_ReturnsMinimalHealthyBody()
    {
        HttpClient client = _factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync("/health/live");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        string body = await response.Content.ReadAsStringAsync();
        Assert.Equal("Healthy", body);
    }

    [Fact]
    public async Task Get_Readiness_WithDatabaseDown_DoesNotDiscloseDetails()
    {
        HttpClient client = _factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync("/health/ready");

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        string body = await response.Content.ReadAsStringAsync();

        Assert.Equal("Unhealthy", body);
        Assert.DoesNotContain("54329", body);          // test connection string port
        Assert.DoesNotContain("localhost", body);       // internal hostname
        Assert.DoesNotContain("Password", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Exception", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("StackTrace", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Npgsql", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Get_Health_WithDatabaseDown_DoesNotDiscloseDetails()
    {
        HttpClient client = _factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        string body = await response.Content.ReadAsStringAsync();

        Assert.Equal("Unhealthy", body);
        Assert.DoesNotContain("54329", body);
        Assert.DoesNotContain("localhost", body);
        Assert.DoesNotContain("Password", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Exception", body, StringComparison.OrdinalIgnoreCase);
    }
}
