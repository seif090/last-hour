using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using LastHour.Api.Tests.Support;
using Microsoft.Data.Sqlite;

namespace LastHour.Api.Tests.Endpoints;

/// <summary>
/// Exercises the registration endpoint over the full HTTP → pipeline → database path against a
/// per-test SQLite database. The response contract is verified end to end: safe success
/// payloads, RFC 7807 problem details for every failure class, and no password, hash or
/// internal state ever exposed.
/// </summary>
[Collection("ApiEndpoints")]
public class RegistrationEndpointsTests
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

    [Fact]
    public async Task Register_ValidRequest_ReturnsCreatedWithSafeBody()
    {
        using SqliteApiFactory factory = CreateFactory();
        HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/v1/auth/register",
            new { email = "User@Example.com", password = ValidPassword });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        RegisterResponse? payload = await response.Content.ReadFromJsonAsync<RegisterResponse>();
        payload.Should().NotBeNull();
        payload!.UserId.Should().NotBeEmpty();
        payload.Status.Should().Be("Pending");
    }

    [Fact]
    public async Task Register_ValidRequest_ResponseNeverExposesPasswordOrHash()
    {
        using SqliteApiFactory factory = CreateFactory();
        HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/v1/auth/register",
            new { email = "user@example.com", password = ValidPassword });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        string body = await response.Content.ReadAsStringAsync();
        body.Should().NotContain(ValidPassword);
        body.ToLowerInvariant().Should().NotContain("hash");
    }

    [Fact]
    public async Task Register_DuplicateEmail_ReturnsConflictWithoutInternalDetails()
    {
        using SqliteApiFactory factory = CreateFactory();
        HttpClient client = factory.CreateClient();
        await client.PostAsJsonAsync(
            "/api/v1/auth/register",
            new { email = "user@example.com", password = ValidPassword });

        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/v1/auth/register",
            new { email = "USER@example.com", password = ValidPassword });

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        string body = await response.Content.ReadAsStringAsync();
        using JsonDocument document = JsonDocument.Parse(body);
        document.RootElement.GetProperty("code").GetString().Should().Be("Identity.EmailAlreadyRegistered");
        body.Should().NotContain("IX_users_normalized_email");
        body.ToLowerInvariant().Should().NotContain("constraint");
        body.ToLowerInvariant().Should().NotContain("sql");
    }

    [Fact]
    public async Task Register_InvalidEmail_ReturnsBadRequestWithValidationCode()
    {
        using SqliteApiFactory factory = CreateFactory();
        HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/v1/auth/register",
            new { email = "not-an-email", password = ValidPassword });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        using JsonDocument document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        document.RootElement.GetProperty("code").GetString().Should().Be("InvalidEmail");
        document.RootElement.GetProperty("status").GetInt32().Should().Be(400);
    }

    [Fact]
    public async Task Register_WeakPassword_ReturnsBadRequestWithPolicyCode()
    {
        using SqliteApiFactory factory = CreateFactory();
        HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/v1/auth/register",
            new { email = "user@example.com", password = "short" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        using JsonDocument document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        document.RootElement.GetProperty("code").GetString().Should().Be("Identity.PasswordTooShort");
    }

    [Fact]
    public async Task Register_MissingFields_ReturnsBadRequestWithValidationErrors()
    {
        using SqliteApiFactory factory = CreateFactory();
        HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/v1/auth/register",
            new { email = string.Empty, password = string.Empty });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        using JsonDocument document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        document.RootElement.GetProperty("errors")
            .EnumerateObject()
            .Select(property => property.Name)
            .Should()
            .Contain(ExpectedMissingFieldCodes);
    }

    [Fact]
    public async Task Register_MalformedJson_ReturnsBadRequest()
    {
        using SqliteApiFactory factory = CreateFactory();
        HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.PostAsync(
            "/api/v1/auth/register",
            new StringContent("{ this is not json", Encoding.UTF8, "application/json"));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    private sealed record RegisterResponse(Guid UserId, string Status);
}
