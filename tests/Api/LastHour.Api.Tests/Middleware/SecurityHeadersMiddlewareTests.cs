using System.Net;
using LastHour.Api.Tests.Support;
using Microsoft.AspNetCore.Mvc.Testing;

namespace LastHour.Api.Tests.Middleware;

/// <summary>
/// Verifies the HTTPS-only security header behavior of the API host. The forwarded-headers
/// middleware processes <c>X-Forwarded-Proto: https</c> from the loopback test client, so the
/// request scheme becomes HTTPS exactly as it does behind a TLS-terminating reverse proxy. HSTS
/// is emitted only in that case; plain-HTTP requests never receive it.
/// </summary>
[Collection("ApiEndpoints")]
public class SecurityHeadersMiddlewareTests : IClassFixture<LastHourApiFactory>
{
    private readonly WebApplicationFactory<Program> _factory;

    /// <summary>
    /// Initializes a new instance of the <see cref="SecurityHeadersMiddlewareTests"/> class.
    /// </summary>
    /// <param name="factory">The shared API host factory provided by xUnit.</param>
    public SecurityHeadersMiddlewareTests(LastHourApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Get_Root_OverHttps_EmitHstsWithConfiguredDirectives()
    {
        HttpClient client = _factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync("/", forwardedProto: "https");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("max-age=31536000; includeSubDomains", response.Headers.GetValues("Strict-Transport-Security").Single());
    }

    [Fact]
    public async Task Get_Root_OverHttpsWithPreloadConfigured_EmitPreloadDirective()
    {
        using WebApplicationFactory<Program> factory = _factory.WithWebHostBuilder(builder =>
            builder.UseSetting("SecurityHeaders:Hsts:Preload", "true"));

        HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync("/", forwardedProto: "https");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(
            "max-age=31536000; includeSubDomains; preload",
            response.Headers.GetValues("Strict-Transport-Security").Single());
    }

    [Fact]
    public async Task Get_Root_OverHttp_DoesNotEmitHsts()
    {
        HttpClient client = _factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync("/");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.False(response.Headers.Contains("Strict-Transport-Security"));
    }

    [Fact]
    public async Task Get_Root_OverHttps_StillEmitsRemainingSecurityHeaders()
    {
        HttpClient client = _factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync("/", forwardedProto: "https");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("nosniff", response.Headers.GetValues("X-Content-Type-Options").Single());
        Assert.Equal("DENY", response.Headers.GetValues("X-Frame-Options").Single());
        Assert.Contains("frame-ancestors 'none'", response.Headers.GetValues("Content-Security-Policy").Single());
    }
}

/// <summary>
/// Adds a forwarded-proto helper used by the HTTPS scenarios.
/// </summary>
internal static class HttpClientExtensions
{
    /// <summary>
    /// Sends a GET request that advertises the given <c>X-Forwarded-Proto</c> value.
    /// </summary>
    /// <param name="client">The HTTP client.</param>
    /// <param name="path">The request path.</param>
    /// <param name="forwardedProto">The forwarded protocol, or <see langword="null"/> to omit the header.</param>
    /// <returns>The response.</returns>
    public static async Task<HttpResponseMessage> GetAsync(
        this HttpClient client,
        string path,
        string? forwardedProto)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, path);
        if (forwardedProto is not null)
        {
            request.Headers.TryAddWithoutValidation("X-Forwarded-Proto", forwardedProto);
        }

        return await client.SendAsync(request);
    }
}
