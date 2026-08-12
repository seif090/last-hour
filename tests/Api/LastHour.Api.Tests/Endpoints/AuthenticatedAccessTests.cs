using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using FluentAssertions;
using LastHour.Api.Tests.Support;
using Microsoft.Data.Sqlite;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace LastHour.Api.Tests.Endpoints;

/// <summary>
/// Validates JWT bearer authentication end to end through the authenticated <c>/me</c>
/// endpoint: a valid token authenticates, and missing, malformed, expired, unsigned or
/// mis-issued tokens are all rejected with 401. The tokens for the negative cases are crafted
/// with the test signing key, so each rejection isolates exactly one validation rule.
/// </summary>
[Collection("ApiEndpoints")]
public class AuthenticatedAccessTests
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

    private static async Task<string> LoginAsync(HttpClient client, string email, string password)
    {
        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new { email, password });
        LoginResponse? payload = await response.Content.ReadFromJsonAsync<LoginResponse>();
        return payload!.AccessToken;
    }

    private static async Task<HttpResponseMessage> GetMeAsync(HttpClient client, string? token)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/auth/me");
        if (token is not null)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        return await client.SendAsync(request);
    }

    private static string CreateToken(
        string? issuer = null,
        string? audience = null,
        string? signingKey = null,
        TimeSpan? lifetime = null)
    {
        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = issuer ?? TestJwt.Issuer,
            Audience = audience ?? TestJwt.Audience,
            IssuedAt = DateTime.UtcNow,
            NotBefore = DateTime.UtcNow,
            Expires = DateTime.UtcNow.Add(lifetime ?? TimeSpan.FromMinutes(15)),
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey ?? TestJwt.SigningKey)),
                SecurityAlgorithms.HmacSha256),
            Claims = new Dictionary<string, object>
            {
                [JwtRegisteredClaimNames.Sub] = Guid.NewGuid().ToString(),
                [JwtRegisteredClaimNames.Jti] = Guid.NewGuid().ToString("N"),
            },
        };

        return new JsonWebTokenHandler().CreateToken(descriptor);
    }

    private static string CreateUnsignedToken()
    {
        static string Base64Url(string value)
            => Convert.ToBase64String(Encoding.UTF8.GetBytes(value))
                .TrimEnd('=')
                .Replace('+', '-')
                .Replace('/', '_');

        long expires = new DateTimeOffset(DateTime.UtcNow.AddMinutes(15)).ToUnixTimeSeconds();
        string header = Base64Url("{\"alg\":\"none\",\"typ\":\"JWT\"}");
        string payload = Base64Url(
            $"{{\"sub\":\"{Guid.NewGuid()}\",\"iss\":\"{TestJwt.Issuer}\",\"aud\":\"{TestJwt.Audience}\",\"exp\":{expires}}}");
        return $"{header}.{payload}.";
    }

    [Fact]
    public async Task Me_ValidToken_ReturnsAuthenticatedUserId()
    {
        using SqliteApiFactory factory = CreateFactory();
        Guid userId = await UserSeeder.SeedUserAsync(factory, "user@example.com", ValidPassword);
        HttpClient client = factory.CreateClient();
        string token = await LoginAsync(client, "user@example.com", ValidPassword);

        HttpResponseMessage response = await GetMeAsync(client, token);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        CurrentUserResponse? payload = await response.Content.ReadFromJsonAsync<CurrentUserResponse>();
        payload.Should().NotBeNull();
        payload!.UserId.Should().Be(userId);
    }

    [Fact]
    public async Task Me_MissingToken_ReturnsUnauthorized()
    {
        using SqliteApiFactory factory = CreateFactory();
        HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await GetMeAsync(client, null);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Me_MalformedToken_ReturnsUnauthorized()
    {
        using SqliteApiFactory factory = CreateFactory();
        HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await GetMeAsync(client, "not-a-jwt");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Me_ExpiredToken_ReturnsUnauthorized()
    {
        using SqliteApiFactory factory = CreateFactory();
        HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await GetMeAsync(
            client,
            CreateToken(lifetime: TimeSpan.FromMinutes(-10)));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Me_TokenSignedWithDifferentKey_ReturnsUnauthorized()
    {
        using SqliteApiFactory factory = CreateFactory();
        HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await GetMeAsync(
            client,
            CreateToken(signingKey: "a-different-key-0123456789abcdef0123456789abcdef"));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Me_TokenWithWrongIssuer_ReturnsUnauthorized()
    {
        using SqliteApiFactory factory = CreateFactory();
        HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await GetMeAsync(client, CreateToken(issuer: "https://evil.example"));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Me_TokenWithWrongAudience_ReturnsUnauthorized()
    {
        using SqliteApiFactory factory = CreateFactory();
        HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await GetMeAsync(client, CreateToken(audience: "another-api"));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Me_UnsignedToken_ReturnsUnauthorized()
    {
        using SqliteApiFactory factory = CreateFactory();
        HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await GetMeAsync(client, CreateUnsignedToken());

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private sealed record LoginResponse(string AccessToken, string TokenType, int ExpiresIn);

    private sealed record CurrentUserResponse(Guid UserId);
}
