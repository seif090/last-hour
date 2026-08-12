using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using LastHour.Api.Tests.Support;
using Microsoft.Data.Sqlite;

namespace LastHour.Api.Tests.Endpoints;

/// <summary>
/// Exercises the logout endpoint: revoking one session's refresh credential without touching
/// other sessions, staying idempotent for unknown or already-revoked tokens, and never
/// revealing whether a presented token was ever valid.
/// </summary>
[Collection("ApiEndpoints")]
public class LogoutEndpointsTests
{
    private const string ValidPassword = "correct horse battery staple";

    private static string CreateConnectionString()
        => new SqliteConnectionStringBuilder
        {
            DataSource = Path.Combine(Path.GetTempPath(), $"lasthour-test-{Guid.NewGuid():N}.db"),
        }.ToString();

    private static SqliteApiFactory CreateFactory()
    {
        var factory = new SqliteApiFactory(CreateConnectionString());
        factory.EnsureSchemaCreated();
        return factory;
    }

    private static async Task<TokenResponse> LoginAsync(HttpClient client, string email)
    {
        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new { email, password = ValidPassword });
        return (await response.Content.ReadFromJsonAsync<TokenResponse>())!;
    }

    [Fact]
    public async Task Logout_ActiveToken_ReturnsNoContentAndRevokesSession()
    {
        using SqliteApiFactory factory = CreateFactory();
        await UserSeeder.SeedUserAsync(factory, "user@example.com", ValidPassword);
        HttpClient client = factory.CreateClient();
        TokenResponse login = await LoginAsync(client, "user@example.com");

        HttpResponseMessage logout = await client.PostAsJsonAsync(
            "/api/v1/auth/logout",
            new { refreshToken = login.RefreshToken });

        logout.StatusCode.Should().Be(HttpStatusCode.NoContent);

        HttpResponseMessage refresh = await client.PostAsJsonAsync(
            "/api/v1/auth/refresh",
            new { refreshToken = login.RefreshToken });
        refresh.StatusCode.Should().Be(HttpStatusCode.Unauthorized, "the logged-out session cannot refresh");
    }

    [Fact]
    public async Task Logout_UnknownToken_StillReturnsNoContent()
    {
        using SqliteApiFactory factory = CreateFactory();
        HttpClient client = factory.CreateClient();

        HttpResponseMessage logout = await client.PostAsJsonAsync(
            "/api/v1/auth/logout",
            new { refreshToken = "never-issued-token" });

        logout.StatusCode.Should().Be(HttpStatusCode.NoContent, "logout is idempotent and reveals nothing");
    }

    [Fact]
    public async Task Logout_AlreadyRevokedToken_StillReturnsNoContent()
    {
        using SqliteApiFactory factory = CreateFactory();
        await UserSeeder.SeedUserAsync(factory, "user@example.com", ValidPassword);
        HttpClient client = factory.CreateClient();
        TokenResponse login = await LoginAsync(client, "user@example.com");
        await client.PostAsJsonAsync("/api/v1/auth/logout", new { refreshToken = login.RefreshToken });

        HttpResponseMessage logout = await client.PostAsJsonAsync(
            "/api/v1/auth/logout",
            new { refreshToken = login.RefreshToken });

        logout.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Logout_OneSession_DoesNotAffectOtherSessions()
    {
        using SqliteApiFactory factory = CreateFactory();
        await UserSeeder.SeedUserAsync(factory, "user@example.com", ValidPassword);
        HttpClient client = factory.CreateClient();
        TokenResponse desktop = await LoginAsync(client, "user@example.com");
        TokenResponse mobile = await LoginAsync(client, "user@example.com");

        HttpResponseMessage logout = await client.PostAsJsonAsync(
            "/api/v1/auth/logout",
            new { refreshToken = desktop.RefreshToken });
        logout.StatusCode.Should().Be(HttpStatusCode.NoContent);

        HttpResponseMessage mobileRefresh = await client.PostAsJsonAsync(
            "/api/v1/auth/refresh",
            new { refreshToken = mobile.RefreshToken });
        mobileRefresh.StatusCode.Should().Be(HttpStatusCode.OK, "logging out one session leaves the other intact");

        HttpResponseMessage desktopRefresh = await client.PostAsJsonAsync(
            "/api/v1/auth/refresh",
            new { refreshToken = desktop.RefreshToken });
        desktopRefresh.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private sealed record TokenResponse(string AccessToken, string RefreshToken, string TokenType, int ExpiresIn);
}
