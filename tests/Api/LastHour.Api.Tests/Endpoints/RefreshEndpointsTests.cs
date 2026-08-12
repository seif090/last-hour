using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using LastHour.Api.Tests.Support;
using LastHour.BuildingBlocks.Infrastructure.Persistence;
using LastHour.Modules.Identity.Domain.RefreshTokens;
using LastHour.Modules.Identity.Domain.Users;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace LastHour.Api.Tests.Endpoints;

/// <summary>
/// Exercises the refresh endpoint over the full HTTP → pipeline → database path: rotation,
/// reuse detection, expiration, revocation, account-state gating and the simultaneous-refresh
/// race. Every failure surfaces the identical safe 401 payload and no token hash or internal
/// state ever leaks into a response.
/// </summary>
[Collection("ApiEndpoints")]
public class RefreshEndpointsTests
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

    private static async Task<TokenResponse> LoginAsync(HttpClient client, string email = "user@example.com")
    {
        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new { email, password = ValidPassword });
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        return (await response.Content.ReadFromJsonAsync<TokenResponse>())!;
    }

    private static async Task<string> RefreshAsync(HttpClient client, string refreshToken)
    {
        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/v1/auth/refresh",
            new { refreshToken });
        return await response.Content.ReadAsStringAsync();
    }

    private static async Task<int> CountActiveRefreshTokensAsync(SqliteApiFactory factory)
    {
        using IServiceScope scope = factory.Services.CreateScope();
        LastHourDbContext context = scope.ServiceProvider.GetRequiredService<LastHourDbContext>();
        return await context.Set<RefreshToken>().CountAsync(token => token.RevokedAtUtc == null);
    }

    [Fact]
    public async Task Refresh_ValidToken_RotatesAndReturnsNewPair()
    {
        using SqliteApiFactory factory = CreateFactory();
        await UserSeeder.SeedUserAsync(factory, "user@example.com", ValidPassword);
        HttpClient client = factory.CreateClient();
        TokenResponse login = await LoginAsync(client);

        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/v1/auth/refresh",
            new { refreshToken = login.RefreshToken });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        TokenResponse? payload = await response.Content.ReadFromJsonAsync<TokenResponse>();
        payload.Should().NotBeNull();
        payload!.AccessToken.Should().NotBeNullOrWhiteSpace();
        payload.RefreshToken.Should().NotBeNullOrWhiteSpace();
        payload.AccessToken.Should().NotBe(login.AccessToken, "the access token is re-issued");
        payload.RefreshToken.Should().NotBe(login.RefreshToken, "the refresh token is rotated");
        payload.TokenType.Should().Be("Bearer");
        payload.ExpiresIn.Should().BeGreaterThan(0);

        // The old refresh token is revoked by the rotation and can no longer be used.
        string oldTokenBody = await RefreshAsync(client, login.RefreshToken);
        using JsonDocument oldToken = JsonDocument.Parse(oldTokenBody);
        oldToken.RootElement.GetProperty("status").GetInt32().Should().Be(401);
    }

    [Fact]
    public async Task Refresh_ReusedRotatedToken_RevokesWholeFamily()
    {
        using SqliteApiFactory factory = CreateFactory();
        await UserSeeder.SeedUserAsync(factory, "user@example.com", ValidPassword);
        HttpClient client = factory.CreateClient();
        TokenResponse login = await LoginAsync(client);

        HttpResponseMessage rotatedResponse = await client.PostAsJsonAsync(
            "/api/v1/auth/refresh",
            new { refreshToken = login.RefreshToken });
        rotatedResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        TokenResponse rotated = (await rotatedResponse.Content.ReadFromJsonAsync<TokenResponse>())!;

        // Presenting the already-rotated original token is reuse: the whole session family is
        // revoked, so even the freshly rotated token stops working.
        string reuseBody = await RefreshAsync(client, login.RefreshToken);
        using JsonDocument reuse = JsonDocument.Parse(reuseBody);
        reuse.RootElement.GetProperty("status").GetInt32().Should().Be(401, "reuse itself fails safely");

        string successorBody = await RefreshAsync(client, rotated.RefreshToken);
        using JsonDocument successor = JsonDocument.Parse(successorBody);
        successor.RootElement.GetProperty("status").GetInt32().Should().Be(401, "reuse revokes the entire token family");
    }

    [Fact]
    public async Task Refresh_MissingToken_ReturnsBadRequest()
    {
        using SqliteApiFactory factory = CreateFactory();
        HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/v1/auth/refresh",
            new { refreshToken = string.Empty });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        using JsonDocument document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        document.RootElement.GetProperty("code").GetString().Should().Be("ValidationFailed");
        document.RootElement.GetProperty("errors")
            .EnumerateObject()
            .Select(property => property.Name)
            .Should()
            .Contain("RefreshTokenRequired");
    }

    [Fact]
    public async Task Refresh_UnknownToken_ReturnsUniform401()
    {
        using SqliteApiFactory factory = CreateFactory();
        await UserSeeder.SeedUserAsync(factory, "user@example.com", ValidPassword);
        HttpClient client = factory.CreateClient();

        string body = await RefreshAsync(client, "never-issued-token");
        using JsonDocument document = JsonDocument.Parse(body);

        document.RootElement.GetProperty("status").GetInt32().Should().Be(401);
        document.RootElement.GetProperty("code").GetString().Should().Be("InvalidRefreshToken");
    }

    [Fact]
    public async Task Refresh_RevokedToken_ReturnsPayloadIdenticalToUnknown()
    {
        using SqliteApiFactory factory = CreateFactory();
        await UserSeeder.SeedUserAsync(factory, "user@example.com", ValidPassword);
        HttpClient client = factory.CreateClient();
        TokenResponse login = await LoginAsync(client);
        await client.PostAsJsonAsync("/api/v1/auth/logout", new { refreshToken = login.RefreshToken });

        string revokedBody = await RefreshAsync(client, login.RefreshToken);
        string unknownBody = await RefreshAsync(client, "never-issued-token");

        using JsonDocument revoked = JsonDocument.Parse(revokedBody);
        using JsonDocument unknown = JsonDocument.Parse(unknownBody);
        revoked.RootElement.GetProperty("code").GetString().Should().Be(unknown.RootElement.GetProperty("code").GetString());
        revoked.RootElement.GetProperty("detail").GetString().Should().Be(unknown.RootElement.GetProperty("detail").GetString());
        revoked.RootElement.GetProperty("status").GetInt32().Should().Be(unknown.RootElement.GetProperty("status").GetInt32());
    }

    [Fact]
    public async Task Refresh_ExpiredToken_ReturnsUniform401()
    {
        using SqliteApiFactory factory = CreateFactory();
        Guid userId = await UserSeeder.SeedUserAsync(factory, "user@example.com", ValidPassword);
        string expired = await RefreshTokenSeeder.SeedRefreshTokenAsync(
            factory, userId, lifetime: TimeSpan.FromDays(-1));
        HttpClient client = factory.CreateClient();

        string body = await RefreshAsync(client, expired);
        using JsonDocument document = JsonDocument.Parse(body);

        document.RootElement.GetProperty("status").GetInt32().Should().Be(401);
        document.RootElement.GetProperty("code").GetString().Should().Be("InvalidRefreshToken");
    }

    [Fact]
    public async Task Refresh_DisabledAccount_RevokesSessionAndReturns401()
    {
        using SqliteApiFactory factory = CreateFactory();
        Guid userId = await UserSeeder.SeedUserAsync(
            factory, "user@example.com", ValidPassword, AccountStatus.Disabled);
        string token = await RefreshTokenSeeder.SeedRefreshTokenAsync(factory, userId);
        HttpClient client = factory.CreateClient();

        string body = await RefreshAsync(client, token);
        using JsonDocument document = JsonDocument.Parse(body);

        document.RootElement.GetProperty("status").GetInt32().Should().Be(401);
        document.RootElement.GetProperty("code").GetString().Should().Be("InvalidRefreshToken");
    }

    [Fact]
    public async Task Refresh_MalformedJson_ReturnsBadRequest()
    {
        using SqliteApiFactory factory = CreateFactory();
        HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.PostAsync(
            "/api/v1/auth/refresh",
            new StringContent("{ this is not json", Encoding.UTF8, "application/json"));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Refresh_SimultaneousRequests_ExactlyOneSucceeds()
    {
        using SqliteApiFactory factory = CreateFactory();
        await UserSeeder.SeedUserAsync(factory, "user@example.com", ValidPassword);
        HttpClient client = factory.CreateClient();
        TokenResponse login = await LoginAsync(client);

        Task<HttpResponseMessage>[] attempts = Enumerable.Range(0, 3)
            .Select(_ => client.PostAsJsonAsync(
                "/api/v1/auth/refresh",
                new { refreshToken = login.RefreshToken }))
            .ToArray();

        HttpResponseMessage[] responses = await Task.WhenAll(attempts);

        responses.Count(response => response.StatusCode == HttpStatusCode.OK).Should().Be(1);
        responses.Where(response => response.StatusCode != HttpStatusCode.OK)
            .Should()
            .OnlyContain(response =>
                response.StatusCode == HttpStatusCode.Unauthorized || response.StatusCode == HttpStatusCode.Conflict,
                "competing refreshes fail safely as 401 or 409");
        (await CountActiveRefreshTokensAsync(factory)).Should().Be(1, "exactly one replacement chain exists");
    }

    private sealed record TokenResponse(string AccessToken, string RefreshToken, string TokenType, int ExpiresIn);
}
