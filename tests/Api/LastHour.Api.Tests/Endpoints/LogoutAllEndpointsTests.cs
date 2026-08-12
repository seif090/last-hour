using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using LastHour.Api.Tests.Support;
using Microsoft.Data.Sqlite;

namespace LastHour.Api.Tests.Endpoints;

/// <summary>
/// Exercises the authenticated logout-all endpoint: every session of the caller is revoked at
/// once, other users' sessions are untouched, and an unauthenticated caller is rejected.
/// </summary>
[Collection("ApiEndpoints")]
public class LogoutAllEndpointsTests
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

    private static async Task<(HttpClient Client, string RefreshToken)> CreateAuthenticatedClientAsync(
        SqliteApiFactory factory,
        string email)
    {
        await UserSeeder.SeedUserAsync(factory, email, ValidPassword);
        HttpClient client = factory.CreateClient();
        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new { email, password = ValidPassword });
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        LoginPayload? login = await response.Content.ReadFromJsonAsync<LoginPayload>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login!.AccessToken);
        return (client, login.RefreshToken);
    }

    [Fact]
    public async Task LogoutAll_RevokesEverySessionOfTheCallerOnly()
    {
        using SqliteApiFactory factory = CreateFactory();
        (HttpClient ownerClient, string ownerRefresh) = await CreateAuthenticatedClientAsync(factory, "owner@example.com");
        (HttpClient otherClient, string otherRefresh) = await CreateAuthenticatedClientAsync(factory, "other@example.com");

        // A second session for the owner.
        HttpResponseMessage secondLogin = await ownerClient.PostAsJsonAsync(
            "/api/v1/auth/login",
            new { email = "owner@example.com", password = ValidPassword });
        secondLogin.StatusCode.Should().Be(HttpStatusCode.OK);

        HttpResponseMessage response = await ownerClient.PostAsync("/api/v1/auth/logout-all", null);
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Every owner session is revoked; the other user's session is untouched.
        HttpResponseMessage ownerRefreshResponse = await ownerClient.PostAsJsonAsync(
            "/api/v1/auth/refresh",
            new { refreshToken = ownerRefresh });
        ownerRefreshResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        HttpResponseMessage otherRefreshResponse = await otherClient.PostAsJsonAsync(
            "/api/v1/auth/refresh",
            new { refreshToken = otherRefresh });
        otherRefreshResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task LogoutAll_NoSessions_ReturnsNoContent()
    {
        using SqliteApiFactory factory = CreateFactory();
        (HttpClient client, _) = await CreateAuthenticatedClientAsync(factory, "user@example.com");

        // Log out the single session first, then logout-all is a no-op.
        await client.PostAsync("/api/v1/auth/logout-all", null);

        HttpResponseMessage response = await client.PostAsync("/api/v1/auth/logout-all", null);
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task LogoutAll_Unauthenticated_Returns401()
    {
        using SqliteApiFactory factory = CreateFactory();
        HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.PostAsync("/api/v1/auth/logout-all", null);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private sealed record LoginPayload(string AccessToken, string RefreshToken, string TokenType, int ExpiresIn);
}
