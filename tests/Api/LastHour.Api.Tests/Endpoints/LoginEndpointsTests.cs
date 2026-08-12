using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using LastHour.Api.Tests.Support;
using LastHour.Modules.Identity.Domain.Users;
using Microsoft.Data.Sqlite;

namespace LastHour.Api.Tests.Endpoints;    /// <summary>
                                           /// Exercises the login endpoint over the full HTTP → pipeline → database → token path against a
                                           /// per-test SQLite database. Every failure class — wrong password, unknown account, unavailable
                                           /// account — is asserted to produce the identical safe 401 payload, so the endpoint cannot be
                                           /// used to enumerate accounts, and no password, hash or token material ever leaks into a
                                           /// response. A successful login returns both an access token and a refresh token.
                                           /// </summary>
[Collection("ApiEndpoints")]
public class LoginEndpointsTests
{
    private const string ValidPassword = "correct horse battery staple";

    private static readonly string[] ExpectedMissingFieldCodes = { "EmailRequired", "PasswordRequired" };

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
        return await response.Content.ReadAsStringAsync();
    }

    [Fact]
    public async Task Login_ValidCredentials_ReturnsTokenWithSafeBody()
    {
        using SqliteApiFactory factory = CreateFactory();
        await UserSeeder.SeedUserAsync(factory, "User@Example.com", ValidPassword);
        HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new { email = "user@example.com", password = ValidPassword });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        LoginResponse? payload = await response.Content.ReadFromJsonAsync<LoginResponse>();
        payload.Should().NotBeNull();
        payload!.AccessToken.Should().NotBeNullOrWhiteSpace();
        payload.RefreshToken.Should().NotBeNullOrWhiteSpace();
        payload.RefreshToken.Should().NotBe(payload.AccessToken);
        payload.TokenType.Should().Be("Bearer");
        payload.ExpiresIn.Should().BeGreaterThan(0);

        string body = await response.Content.ReadAsStringAsync();
        body.Should().NotContain(ValidPassword);
        body.ToLowerInvariant().Should().NotContain("hash");
    }

    [Fact]
    public async Task Login_WrongPassword_ReturnsUniform401WithoutToken()
    {
        using SqliteApiFactory factory = CreateFactory();
        await UserSeeder.SeedUserAsync(factory, "user@example.com", ValidPassword);
        HttpClient client = factory.CreateClient();

        string body = await LoginAsync(client, "user@example.com", "wrong-password-value");
        using JsonDocument document = JsonDocument.Parse(body);

        document.RootElement.GetProperty("status").GetInt32().Should().Be(401);
        document.RootElement.GetProperty("code").GetString().Should().Be("AuthenticationFailed");
        body.Should().NotContain("accessToken");
        body.Should().NotContain("refreshToken");
        body.Should().NotContain(ValidPassword);
    }

    [Fact]
    public async Task Login_UnknownUser_ReturnsPayloadIdenticalToWrongPassword()
    {
        using SqliteApiFactory factory = CreateFactory();
        await UserSeeder.SeedUserAsync(factory, "user@example.com", ValidPassword);
        HttpClient client = factory.CreateClient();

        string wrongPasswordBody = await LoginAsync(client, "user@example.com", "wrong-password-value");
        string unknownUserBody = await LoginAsync(client, "nobody@example.com", ValidPassword);

        using JsonDocument wrong = JsonDocument.Parse(wrongPasswordBody);
        using JsonDocument unknown = JsonDocument.Parse(unknownUserBody);
        wrong.RootElement.GetProperty("code").GetString().Should().Be(unknown.RootElement.GetProperty("code").GetString());
        wrong.RootElement.GetProperty("detail").GetString().Should().Be(unknown.RootElement.GetProperty("detail").GetString());
        wrong.RootElement.GetProperty("status").GetInt32().Should().Be(unknown.RootElement.GetProperty("status").GetInt32());
    }

    [Theory]
    [InlineData(AccountStatus.Pending)]
    [InlineData(AccountStatus.Suspended)]
    [InlineData(AccountStatus.Disabled)]
    public async Task Login_UnavailableAccount_ReturnsUniform401(AccountStatus status)
    {
        using SqliteApiFactory factory = CreateFactory();
        await UserSeeder.SeedUserAsync(factory, "user@example.com", ValidPassword, status);
        HttpClient client = factory.CreateClient();

        string body = await LoginAsync(client, "user@example.com", ValidPassword);
        using JsonDocument document = JsonDocument.Parse(body);

        document.RootElement.GetProperty("status").GetInt32().Should().Be(401);
        document.RootElement.GetProperty("code").GetString().Should().Be("AuthenticationFailed");
    }

    [Fact]
    public async Task Login_MalformedEmail_ReturnsBadRequest()
    {
        using SqliteApiFactory factory = CreateFactory();
        HttpClient client = factory.CreateClient();

        string body = await LoginAsync(client, "not-an-email", ValidPassword);
        using JsonDocument document = JsonDocument.Parse(body);

        document.RootElement.GetProperty("status").GetInt32().Should().Be(400);
        document.RootElement.GetProperty("code").GetString().Should().Be("InvalidEmail");
    }

    [Fact]
    public async Task Login_MissingFields_ReturnsBadRequestWithValidationErrors()
    {
        using SqliteApiFactory factory = CreateFactory();
        HttpClient client = factory.CreateClient();

        string body = await LoginAsync(client, string.Empty, string.Empty);
        using JsonDocument document = JsonDocument.Parse(body);

        document.RootElement.GetProperty("status").GetInt32().Should().Be(400);
        document.RootElement.GetProperty("errors")
            .EnumerateObject()
            .Select(property => property.Name)
            .Should()
            .Contain(ExpectedMissingFieldCodes);
    }

    [Fact]
    public async Task Login_MalformedJson_ReturnsBadRequest()
    {
        using SqliteApiFactory factory = CreateFactory();
        HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.PostAsync(
            "/api/v1/auth/login",
            new StringContent("{ this is not json", Encoding.UTF8, "application/json"));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    private sealed record LoginResponse(string AccessToken, string RefreshToken, string TokenType, int ExpiresIn);
}
