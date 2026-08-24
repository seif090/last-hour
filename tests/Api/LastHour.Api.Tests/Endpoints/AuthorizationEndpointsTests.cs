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
/// Exercises permission-based authorization end to end through the role-management endpoints.
/// The matrix covers the full surface: anonymous and invalid-token requests are rejected with
/// 401, an authenticated caller without the required permission receives a consistent 403
/// problem details payload, and a caller holding the permission succeeds. The live-permission
/// test proves the consistency model: assigning and removing a role changes what an existing,
/// already-issued access token may do, because permissions are resolved from the database on
/// every request rather than embedded in the token.
/// </summary>
[Collection("ApiEndpoints")]
public class AuthorizationEndpointsTests
{
    private const string ValidPassword = "correct horse battery staple";

    private sealed record LoginResponse(string AccessToken, string RefreshToken, string TokenType, int ExpiresIn);

    private sealed record RoleCreatedResponse(Guid RoleId, string Name);

    private sealed record RoleResponse(Guid RoleId, string Name);

    private static string CreateConnectionString()
        => new SqliteConnectionStringBuilder
        {
            DataSource = Path.Combine(Path.GetTempPath(), $"lasthour-test-{Guid.NewGuid():N}.db"),
        }.ToString();

    private static async Task<SqliteApiFactory> CreateFactoryAsync()
    {
        var factory = new SqliteApiFactory(CreateConnectionString());
        factory.EnsureSchemaCreated();
        await RoleSeeder.SeedAuthorizationAsync(factory);
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

    private static async Task<Guid> SeedAdminAsync(SqliteApiFactory factory, HttpClient client)
    {
        Guid adminId = await UserSeeder.SeedUserAsync(factory, "admin@example.com", ValidPassword);
        await RoleSeeder.AssignRoleAsync(factory, adminId, LastHour.Modules.Identity.Application.Permissions.BootstrapRoles.Administrator);
        return adminId;
    }

    private static async Task<Guid> SeedRegularUserAsync(SqliteApiFactory factory)
        => await UserSeeder.SeedUserAsync(factory, "user@example.com", ValidPassword);

    private static HttpRequestMessage CreateRequest(HttpMethod method, string url, string? token, object? body = null)
    {
        var request = new HttpRequestMessage(method, url);
        if (token is not null)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        if (body is not null)
        {
            request.Content = JsonContent.Create(body);
        }

        return request;
    }

    private static string CreateTokenWithWrongKey()
    {
        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = TestJwt.Issuer,
            Audience = TestJwt.Audience,
            IssuedAt = DateTime.UtcNow,
            NotBefore = DateTime.UtcNow,
            Expires = DateTime.UtcNow.AddMinutes(15),
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes("a-completely-different-test-key-0123456789abcdef")),
                SecurityAlgorithms.HmacSha256),
            Claims = new Dictionary<string, object>
            {
                [JwtRegisteredClaimNames.Sub] = Guid.NewGuid().ToString(),
                [JwtRegisteredClaimNames.Jti] = Guid.NewGuid().ToString("N"),
            },
        };

        return new JsonWebTokenHandler().CreateToken(descriptor);
    }

    private static string CreateExpiredToken(Guid subject)
    {
        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = TestJwt.Issuer,
            Audience = TestJwt.Audience,
            IssuedAt = DateTime.UtcNow.AddMinutes(-30),
            NotBefore = DateTime.UtcNow.AddMinutes(-30),
            Expires = DateTime.UtcNow.AddMinutes(-15),
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(TestJwt.SigningKey)),
                SecurityAlgorithms.HmacSha256),
            Claims = new Dictionary<string, object>
            {
                [JwtRegisteredClaimNames.Sub] = subject.ToString("D"),
                [JwtRegisteredClaimNames.Jti] = Guid.NewGuid().ToString("N"),
            },
        };

        return new JsonWebTokenHandler().CreateToken(descriptor);
    }

    // ------------------------------------------------------------------
    // 401: not authenticated or token invalid
    // ------------------------------------------------------------------

    [Fact]
    public async Task CreateRole_Anonymous_ReturnsUnauthorized()
    {
        using SqliteApiFactory factory = await CreateFactoryAsync();
        HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/v1/roles",
            new { name = "Manager" });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateRole_InvalidToken_ReturnsUnauthorized()
    {
        using SqliteApiFactory factory = await CreateFactoryAsync();
        HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.SendAsync(CreateRequest(
            HttpMethod.Post,
            "/api/v1/roles",
            CreateTokenWithWrongKey()));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateRole_ExpiredToken_ReturnsUnauthorized()
    {
        using SqliteApiFactory factory = await CreateFactoryAsync();
        HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.SendAsync(CreateRequest(
            HttpMethod.Post,
            "/api/v1/roles",
            CreateExpiredToken(Guid.NewGuid())));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ------------------------------------------------------------------
    // 403: authenticated but lacking the permission
    // ------------------------------------------------------------------

    [Fact]
    public async Task CreateRole_AuthenticatedWithoutPermission_ReturnsForbiddenProblemDetails()
    {
        using SqliteApiFactory factory = await CreateFactoryAsync();
        HttpClient client = factory.CreateClient();
        Guid userId = await SeedRegularUserAsync(factory);
        string token = await LoginAsync(client, "user@example.com", ValidPassword);

        HttpResponseMessage response = await client.SendAsync(CreateRequest(
            HttpMethod.Post,
            "/api/v1/roles",
            token));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        var problem = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        problem!["status"].ToString().Should().Be("403");
        problem["title"].ToString().Should().Be("Forbidden");
        problem["code"].ToString().Should().Be("AuthorizationFailed");

        // No internal permission graph or role structure leaks.
        problem.Should().NotContainKey("permissions");
        problem.Should().NotContainKey("roles");
    }

    [Fact]
    public async Task GetUserRoles_AuthenticatedWithoutPermission_ReturnsForbidden()
    {
        using SqliteApiFactory factory = await CreateFactoryAsync();
        HttpClient client = factory.CreateClient();
        Guid targetId = await SeedRegularUserAsync(factory);
        string token = await LoginAsync(client, "user@example.com", ValidPassword);

        HttpResponseMessage response = await client.SendAsync(CreateRequest(
            HttpMethod.Get,
            $"/api/v1/users/{targetId}/roles",
            token));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task AssignRole_AuthenticatedWithoutPermission_ReturnsForbidden()
    {
        using SqliteApiFactory factory = await CreateFactoryAsync();
        HttpClient client = factory.CreateClient();
        Guid targetId = await SeedRegularUserAsync(factory);
        string token = await LoginAsync(client, "user@example.com", ValidPassword);

        HttpResponseMessage response = await client.SendAsync(CreateRequest(
            HttpMethod.Put,
            $"/api/v1/users/{targetId}/roles/{Guid.NewGuid()}",
            token));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ------------------------------------------------------------------
    // Permission held: success paths
    // ------------------------------------------------------------------

    [Fact]
    public async Task CreateRole_AuthenticatedWithPermission_ReturnsCreated()
    {
        using SqliteApiFactory factory = await CreateFactoryAsync();
        HttpClient client = factory.CreateClient();
        await SeedAdminAsync(factory, client);
        string token = await LoginAsync(client, "admin@example.com", ValidPassword);

        HttpResponseMessage response = await client.SendAsync(CreateRequest(
            HttpMethod.Post,
            "/api/v1/roles",
            token,
            new { name = "Manager" }));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        RoleCreatedResponse? payload = await response.Content.ReadFromJsonAsync<RoleCreatedResponse>();
        payload.Should().NotBeNull();
        payload!.RoleId.Should().NotBeEmpty();
        payload.Name.Should().Be("Manager");
    }

    [Fact]
    public async Task CreateRole_AuthenticatedWithPermission_DuplicateName_ReturnsConflict()
    {
        using SqliteApiFactory factory = await CreateFactoryAsync();
        HttpClient client = factory.CreateClient();
        await SeedAdminAsync(factory, client);
        string token = await LoginAsync(client, "admin@example.com", ValidPassword);

        HttpResponseMessage first = await client.SendAsync(CreateRequest(
            HttpMethod.Post,
            "/api/v1/roles",
            token,
            new { name = "Manager" }));
        first.StatusCode.Should().Be(HttpStatusCode.Created);

        HttpResponseMessage second = await client.SendAsync(CreateRequest(
            HttpMethod.Post,
            "/api/v1/roles",
            token,
            new { name = "Manager" }));

        second.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task GetUserRoles_AuthenticatedWithPermission_ReturnsAssignedRoles()
    {
        using SqliteApiFactory factory = await CreateFactoryAsync();
        HttpClient client = factory.CreateClient();
        Guid adminId = await SeedAdminAsync(factory, client);
        Guid targetId = await SeedRegularUserAsync(factory);
        await RoleSeeder.AssignRoleAsync(factory, targetId, "Administrator");
        string token = await LoginAsync(client, "admin@example.com", ValidPassword);

        HttpResponseMessage response = await client.SendAsync(CreateRequest(
            HttpMethod.Get,
            $"/api/v1/users/{targetId}/roles",
            token));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        List<RoleResponse>? roles = await response.Content.ReadFromJsonAsync<List<RoleResponse>>();
        roles.Should().ContainSingle()
            .Which.Name.Should().Be("Administrator");
    }

    [Fact]
    public async Task AssignRole_AuthenticatedWithPermission_ReturnsNoContent()
    {
        using SqliteApiFactory factory = await CreateFactoryAsync();
        HttpClient client = factory.CreateClient();
        Guid adminId = await SeedAdminAsync(factory, client);
        Guid targetId = await SeedRegularUserAsync(factory);
        string token = await LoginAsync(client, "admin@example.com", ValidPassword);

        // Create a role through the endpoint, then assign it.
        HttpResponseMessage created = await client.SendAsync(CreateRequest(
            HttpMethod.Post,
            "/api/v1/roles",
            token,
            new { name = "Manager" }));
        RoleCreatedResponse? createdRole = await created.Content.ReadFromJsonAsync<RoleCreatedResponse>();

        HttpResponseMessage response = await client.SendAsync(CreateRequest(
            HttpMethod.Put,
            $"/api/v1/users/{targetId}/roles/{createdRole!.RoleId}",
            token));

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task AssignRole_DuplicateAssignment_ReturnsConflict()
    {
        using SqliteApiFactory factory = await CreateFactoryAsync();
        HttpClient client = factory.CreateClient();
        Guid adminId = await SeedAdminAsync(factory, client);
        Guid targetId = await SeedRegularUserAsync(factory);
        await RoleSeeder.AssignRoleAsync(factory, targetId, "Administrator");
        string token = await LoginAsync(client, "admin@example.com", ValidPassword);

        HttpResponseMessage response = await client.SendAsync(CreateRequest(
            HttpMethod.Put,
            $"/api/v1/users/{targetId}/roles/{Guid.NewGuid()}",
            token));

        // The route id is arbitrary here; the conflict test needs a real role id, so use the
        // seeded administrator role. Fetch it via the read endpoint.
        HttpResponseMessage rolesResponse = await client.SendAsync(CreateRequest(
            HttpMethod.Get,
            $"/api/v1/users/{targetId}/roles",
            token));
        List<RoleResponse>? roles = await rolesResponse.Content.ReadFromJsonAsync<List<RoleResponse>>();

        HttpResponseMessage duplicate = await client.SendAsync(CreateRequest(
            HttpMethod.Put,
            $"/api/v1/users/{targetId}/roles/{roles![0].RoleId}",
            token));

        duplicate.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task AssignRole_MissingUser_ReturnsNotFound()
    {
        using SqliteApiFactory factory = await CreateFactoryAsync();
        HttpClient client = factory.CreateClient();
        await SeedAdminAsync(factory, client);
        string token = await LoginAsync(client, "admin@example.com", ValidPassword);

        HttpResponseMessage response = await client.SendAsync(CreateRequest(
            HttpMethod.Put,
            $"/api/v1/users/{Guid.NewGuid()}/roles/{Guid.NewGuid()}",
            token));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task RemoveRole_AuthenticatedWithPermission_ReturnsNoContent()
    {
        using SqliteApiFactory factory = await CreateFactoryAsync();
        HttpClient client = factory.CreateClient();
        Guid adminId = await SeedAdminAsync(factory, client);
        Guid targetId = await SeedRegularUserAsync(factory);
        await RoleSeeder.AssignRoleAsync(factory, targetId, "Administrator");
        string token = await LoginAsync(client, "admin@example.com", ValidPassword);

        HttpResponseMessage rolesResponse = await client.SendAsync(CreateRequest(
            HttpMethod.Get,
            $"/api/v1/users/{targetId}/roles",
            token));
        List<RoleResponse>? roles = await rolesResponse.Content.ReadFromJsonAsync<List<RoleResponse>>();

        HttpResponseMessage response = await client.SendAsync(CreateRequest(
            HttpMethod.Delete,
            $"/api/v1/users/{targetId}/roles/{roles![0].RoleId}",
            token));

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task GetUserRoles_InvalidUserId_ReturnsBadRequest()
    {
        using SqliteApiFactory factory = await CreateFactoryAsync();
        HttpClient client = factory.CreateClient();
        await SeedAdminAsync(factory, client);
        string token = await LoginAsync(client, "admin@example.com", ValidPassword);

        HttpResponseMessage response = await client.SendAsync(CreateRequest(
            HttpMethod.Get,
            "/api/v1/users/not-a-guid/roles",
            token));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ------------------------------------------------------------------
    // Consistency model: permissions resolve live, not from the token
    // ------------------------------------------------------------------

    [Fact]
    public async Task PermissionChanges_TakeEffectImmediately_WithoutNewToken()
    {
        using SqliteApiFactory factory = await CreateFactoryAsync();
        HttpClient client = factory.CreateClient();
        Guid adminId = await SeedAdminAsync(factory, client);
        string adminToken = await LoginAsync(client, "admin@example.com", ValidPassword);

        Guid userId = await SeedRegularUserAsync(factory);
        string userToken = await LoginAsync(client, "user@example.com", ValidPassword);

        // 1. The user cannot manage roles.
        HttpResponseMessage before = await client.SendAsync(CreateRequest(
            HttpMethod.Post,
            "/api/v1/roles",
            userToken,
            new { name = "Manager" }));
        before.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        // 2. An administrator assigns the seeded role (the one holding the full catalog)
        // through the HTTP surface. Find its id via the read endpoint first.
        HttpResponseMessage adminRoles = await client.SendAsync(CreateRequest(
            HttpMethod.Get,
            $"/api/v1/users/{adminId}/roles",
            adminToken));
        List<RoleResponse>? adminRoleList = await adminRoles.Content.ReadFromJsonAsync<List<RoleResponse>>();
        Guid administratorRoleId = adminRoleList![0].RoleId;

        HttpResponseMessage assigned = await client.SendAsync(CreateRequest(
            HttpMethod.Put,
            $"/api/v1/users/{userId}/roles/{administratorRoleId}",
            adminToken));
        assigned.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // 3. The SAME access token now holds the permission — permissions are resolved live.
        HttpResponseMessage after = await client.SendAsync(CreateRequest(
            HttpMethod.Post,
            "/api/v1/roles",
            userToken,
            new { name = "Manager" }));
        after.StatusCode.Should().Be(HttpStatusCode.Created);

        // 4. Removing the role revokes the permission for the same token.
        HttpResponseMessage removed = await client.SendAsync(CreateRequest(
            HttpMethod.Delete,
            $"/api/v1/users/{userId}/roles/{administratorRoleId}",
            adminToken));
        removed.StatusCode.Should().Be(HttpStatusCode.NoContent);

        HttpResponseMessage revoked = await client.SendAsync(CreateRequest(
            HttpMethod.Post,
            "/api/v1/roles",
            userToken,
            new { name = "Manager" }));
        revoked.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ------------------------------------------------------------------
    // Security: no client-controlled or forged authorization
    // ------------------------------------------------------------------

    [Fact]
    public async Task AccessToken_CarriesNoPermissionOrRoleClaims()
    {
        using SqliteApiFactory factory = await CreateFactoryAsync();
        HttpClient client = factory.CreateClient();
        await SeedAdminAsync(factory, client);
        string token = await LoginAsync(client, "admin@example.com", ValidPassword);

        var handler = new JsonWebTokenHandler();
        JsonWebToken jwt = handler.ReadJsonWebToken(token);

        jwt.TryGetPayloadValue<string>("permissions", out _).Should().BeFalse();
        jwt.TryGetPayloadValue<string>("roles", out _).Should().BeFalse();
        jwt.TryGetPayloadValue<string>("role", out _).Should().BeFalse();
        jwt.TryGetPayloadValue<string>("perm", out _).Should().BeFalse();
    }

    [Fact]
    public async Task ValidlySignedToken_ForNonexistentUser_DoesNotGrantAccess()
    {
        using SqliteApiFactory factory = await CreateFactoryAsync();
        HttpClient client = factory.CreateClient();

        // Signed with the real test key and valid issuer/audience, but the subject names a
        // user that does not exist: authentication succeeds, authorization has no permission
        // set to grant, so the request is forbidden — never allowed.
        string token = CreateTokenWithSubject(Guid.NewGuid());

        HttpResponseMessage response = await client.SendAsync(CreateRequest(
            HttpMethod.Post,
            "/api/v1/roles",
            token));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    private static string CreateTokenWithSubject(Guid subject)
    {
        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = TestJwt.Issuer,
            Audience = TestJwt.Audience,
            IssuedAt = DateTime.UtcNow,
            NotBefore = DateTime.UtcNow,
            Expires = DateTime.UtcNow.AddMinutes(15),
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(TestJwt.SigningKey)),
                SecurityAlgorithms.HmacSha256),
            Claims = new Dictionary<string, object>
            {
                [JwtRegisteredClaimNames.Sub] = subject.ToString("D"),
                [JwtRegisteredClaimNames.Jti] = Guid.NewGuid().ToString("N"),
            },
        };

        return new JsonWebTokenHandler().CreateToken(descriptor);
    }
}
