using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using LastHour.Api.Tests.Support;
using Microsoft.Data.Sqlite;

namespace LastHour.Api.Tests.Endpoints;

/// <summary>
/// Exercises the authenticated password-change endpoint over the full HTTP → pipeline → database
/// path: the credential is replaced, the old password stops working, every session is revoked,
/// and failures (wrong current password, policy violation, unauthenticated) surface as safe
/// problem details with no password material anywhere.
/// </summary>
[Collection("ApiEndpoints")]
public class ChangePasswordEndpointsTests
{
    private const string ValidPassword = "correct horse battery staple";
    private const string NewPassword = "correct horse battery staple 2";

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
        SqliteApiFactory factory)
    {
        await UserSeeder.SeedUserAsync(factory, "user@example.com", ValidPassword);
        HttpClient client = factory.CreateClient();
        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new { email = "user@example.com", password = ValidPassword });
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        LoginPayload? login = await response.Content.ReadFromJsonAsync<LoginPayload>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login!.AccessToken);
        return (client, login.RefreshToken);
    }

    [Fact]
    public async Task ChangePassword_ValidRequest_ReturnsNoContentAndInvalidatesOldPassword()
    {
        using SqliteApiFactory factory = CreateFactory();
        (HttpClient client, _) = await CreateAuthenticatedClientAsync(factory);

        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/v1/auth/change-password",
            new { currentPassword = ValidPassword, newPassword = NewPassword });

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // The old password no longer authenticates; the new one does.
        HttpResponseMessage oldLogin = await client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new { email = "user@example.com", password = ValidPassword });
        oldLogin.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        HttpResponseMessage newLogin = await client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new { email = "user@example.com", password = NewPassword });
        newLogin.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ChangePassword_WrongCurrentPassword_ReturnsBadRequest()
    {
        using SqliteApiFactory factory = CreateFactory();
        (HttpClient client, _) = await CreateAuthenticatedClientAsync(factory);

        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/v1/auth/change-password",
            new { currentPassword = "wrong-current-password", newPassword = NewPassword });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        using JsonDocument document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        document.RootElement.GetProperty("code").GetString().Should().Be("Identity.InvalidCurrentPassword");
    }

    [Fact]
    public async Task ChangePassword_WeakNewPassword_ReturnsPolicyFailure()
    {
        using SqliteApiFactory factory = CreateFactory();
        (HttpClient client, _) = await CreateAuthenticatedClientAsync(factory);

        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/v1/auth/change-password",
            new { currentPassword = ValidPassword, newPassword = "short" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        using JsonDocument document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        document.RootElement.GetProperty("code").GetString().Should().Be("Identity.PasswordTooShort");
    }

    [Fact]
    public async Task ChangePassword_IdenticalPassword_ReturnsBadRequest()
    {
        using SqliteApiFactory factory = CreateFactory();
        (HttpClient client, _) = await CreateAuthenticatedClientAsync(factory);

        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/v1/auth/change-password",
            new { currentPassword = ValidPassword, newPassword = ValidPassword });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        using JsonDocument document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        document.RootElement.GetProperty("code").GetString().Should().Be("Identity.PasswordUnchanged");
    }

    [Fact]
    public async Task ChangePassword_InvalidatesEverySession()
    {
        using SqliteApiFactory factory = CreateFactory();
        (HttpClient client, string refreshToken) = await CreateAuthenticatedClientAsync(factory);

        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/v1/auth/change-password",
            new { currentPassword = ValidPassword, newPassword = NewPassword });
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // The session's refresh credential was revoked by the change; refresh fails safely.
        HttpResponseMessage refresh = await client.PostAsJsonAsync(
            "/api/v1/auth/refresh",
            new { refreshToken });
        refresh.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ChangePassword_Unauthenticated_Returns401()
    {
        using SqliteApiFactory factory = CreateFactory();
        HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/v1/auth/change-password",
            new { currentPassword = ValidPassword, newPassword = NewPassword });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private sealed record LoginPayload(string AccessToken, string RefreshToken, string TokenType, int ExpiresIn);
}
