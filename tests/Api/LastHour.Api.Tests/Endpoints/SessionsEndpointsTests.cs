using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using LastHour.Api.Tests.Support;
using Microsoft.Data.Sqlite;

namespace LastHour.Api.Tests.Endpoints;

/// <summary>
/// Exercises the authenticated session-inspection endpoint: it returns only safe metadata (id,
/// timestamps, active state) for the caller's own sessions — never hashes, tokens or internal
/// state — and rejects unauthenticated callers.
/// </summary>
[Collection("ApiEndpoints")]
public class SessionsEndpointsTests
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

    private static async Task<HttpClient> CreateAuthenticatedClientAsync(SqliteApiFactory factory, string email)
    {
        await UserSeeder.SeedUserAsync(factory, email, ValidPassword);
        HttpClient client = factory.CreateClient();
        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new { email, password = ValidPassword });
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        LoginPayload? login = await response.Content.ReadFromJsonAsync<LoginPayload>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login!.AccessToken);
        return client;
    }

    [Fact]
    public async Task Sessions_ReturnsOnlySafeMetadataOfTheCallersSessions()
    {
        using SqliteApiFactory factory = CreateFactory();
        HttpClient client = await CreateAuthenticatedClientAsync(factory, "user@example.com");

        // A second session for the same user.
        HttpResponseMessage secondLogin = await client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new { email = "user@example.com", password = ValidPassword });
        secondLogin.StatusCode.Should().Be(HttpStatusCode.OK);

        HttpResponseMessage response = await client.GetAsync("/api/v1/auth/sessions");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        using JsonDocument document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        List<JsonElement> sessions = document.RootElement.EnumerateArray().ToList();
        sessions.Should().HaveCount(2);
        foreach (JsonElement session in sessions)
        {
            Guid.TryParse(session.GetProperty("sessionId").GetString(), out Guid parsed).Should().BeTrue();
            parsed.Should().NotBe(Guid.Empty);
            session.TryGetProperty("createdAtUtc", out _).Should().BeTrue();
            session.TryGetProperty("expiresAtUtc", out _).Should().BeTrue();
            session.GetProperty("active").GetBoolean().Should().BeTrue();

            // No token material or internal state may leak.
            session.TryGetProperty("tokenHash", out _).Should().BeFalse();
            session.TryGetProperty("familyId", out _).Should().BeFalse();
            session.TryGetProperty("revocationReason", out _).Should().BeFalse();
        }
    }

    [Fact]
    public async Task Sessions_Unauthenticated_Returns401()
    {
        using SqliteApiFactory factory = CreateFactory();
        HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync("/api/v1/auth/sessions");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Sessions_OnlyReturnsTheCallersSessions()
    {
        using SqliteApiFactory factory = CreateFactory();
        await UserSeeder.SeedUserAsync(factory, "other@example.com", ValidPassword);
        HttpClient client = await CreateAuthenticatedClientAsync(factory, "user@example.com");

        HttpResponseMessage response = await client.GetAsync("/api/v1/auth/sessions");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        using JsonDocument document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        document.RootElement.GetArrayLength().Should().Be(1, "the other user's session is not exposed");
    }

    private sealed record LoginPayload(string AccessToken, string RefreshToken, string TokenType, int ExpiresIn);
}
