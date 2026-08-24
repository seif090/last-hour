using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using LastHour.Api.Tests.Support;
using LastHour.BuildingBlocks.Application.Models;
using LastHour.BuildingBlocks.Infrastructure.Persistence;
using LastHour.BuildingBlocks.Infrastructure.Persistence.Outbox;
using LastHour.BuildingBlocks.Infrastructure.Persistence.Outbox.Queries;
using LastHour.Modules.Identity.Application.Permissions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace LastHour.Api.Tests.Endpoints;

[Collection("ApiEndpoints")]
public class OutboxEndpointsTests
{
    private const string ValidPassword = "correct horse battery staple";

    private sealed record LoginResponse(string AccessToken, string RefreshToken, string TokenType, int ExpiresIn);

    private sealed record DummyDomainEvent(string Value);

    private static string CreateConnectionString()
        => new SqliteConnectionStringBuilder
        {
            DataSource = Path.Combine(Path.GetTempPath(), $"lasthour-outbox-test-{Guid.NewGuid():N}.db"),
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

    private static async Task<(Guid AdminId, string Email)> SeedAdminAsync(SqliteApiFactory factory)
    {
        string email = $"admin-{Guid.NewGuid():N}@example.com";
        Guid adminId = await UserSeeder.SeedUserAsync(factory, email, ValidPassword);
        await RoleSeeder.AssignRoleAsync(factory, adminId, BootstrapRoles.Administrator);
        return (adminId, email);
    }

    private static async Task<(Guid UserId, string Email)> SeedRegularUserAsync(SqliteApiFactory factory)
    {
        string email = $"user-{Guid.NewGuid():N}@example.com";
        Guid userId = await UserSeeder.SeedUserAsync(factory, email, ValidPassword);
        return (userId, email);
    }

    private static async Task<Guid> SeedOutboxMessageAsync(
        SqliteApiFactory factory,
        bool isProcessed,
        string? error,
        int retryCount)
    {
        using IServiceScope scope = factory.Services.CreateScope();
        LastHourDbContext context = scope.ServiceProvider.GetRequiredService<LastHourDbContext>();

        var message = OutboxMessage.Create(new DummyDomainEvent("secret-payload-data"));
        if (isProcessed && error is not null)
        {
            message.RecordFailure(error, DateTime.UtcNow, 1, TimeSpan.Zero);
        }
        else if (isProcessed)
        {
            message.MarkProcessed(DateTime.UtcNow);
        }

        context.OutboxMessages.Add(message);
        await context.SaveChangesAsync();
        return message.Id;
    }

    [Fact]
    public async Task GetOutboxMessages_Anonymous_ReturnsUnauthorized401()
    {
        using SqliteApiFactory factory = await CreateFactoryAsync();
        HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync("/api/v1/outbox");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetOutboxMessages_UserWithoutPermission_ReturnsForbidden403()
    {
        using SqliteApiFactory factory = await CreateFactoryAsync();
        HttpClient client = factory.CreateClient();

        var user = await SeedRegularUserAsync(factory);
        string token = await LoginAsync(client, user.Email, ValidPassword);

        var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/outbox");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        HttpResponseMessage response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetOutboxMessages_Admin_ReturnsPagedResult200_AndExcludesContent()
    {
        using SqliteApiFactory factory = await CreateFactoryAsync();
        HttpClient client = factory.CreateClient();

        Guid messageId = await SeedOutboxMessageAsync(factory, isProcessed: false, error: null, retryCount: 0);

        var admin = await SeedAdminAsync(factory);
        string token = await LoginAsync(client, admin.Email, ValidPassword);

        var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/outbox?pageNumber=1&pageSize=10");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        HttpResponseMessage response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        string rawJson = await response.Content.ReadAsStringAsync();
        rawJson.Should().NotContain("secret-payload-data");
        rawJson.Should().NotContain("\"content\"");

        PagedResult<OutboxMessageSummaryDto>? paged = await response.Content.ReadFromJsonAsync<PagedResult<OutboxMessageSummaryDto>>();
        paged.Should().NotBeNull();
        paged!.Items.Should().ContainSingle(m => m.Id == messageId);
        paged.Items[0].Status.Should().Be(OutboxMessageStatus.Pending);
    }

    [Fact]
    public async Task GetOutboxMessages_WithStatusFilter_ReturnsFilteredResults()
    {
        using SqliteApiFactory factory = await CreateFactoryAsync();
        HttpClient client = factory.CreateClient();

        Guid pendingId = await SeedOutboxMessageAsync(factory, isProcessed: false, error: null, retryCount: 0);
        Guid abandonedId = await SeedOutboxMessageAsync(factory, isProcessed: true, error: "Dead letter", retryCount: 5);

        var admin = await SeedAdminAsync(factory);
        string token = await LoginAsync(client, admin.Email, ValidPassword);

        var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/outbox?status=Abandoned");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        HttpResponseMessage response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        PagedResult<OutboxMessageSummaryDto>? paged = await response.Content.ReadFromJsonAsync<PagedResult<OutboxMessageSummaryDto>>();
        paged.Should().NotBeNull();
        paged!.Items.Should().ContainSingle(m => m.Id == abandonedId);
        paged.Items[0].Status.Should().Be(OutboxMessageStatus.Abandoned);
    }

    [Fact]
    public async Task GetOutboxMessageById_Anonymous_ReturnsUnauthorized401()
    {
        using SqliteApiFactory factory = await CreateFactoryAsync();
        HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync($"/api/v1/outbox/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetOutboxMessageById_UserWithoutPermission_ReturnsForbidden403()
    {
        using SqliteApiFactory factory = await CreateFactoryAsync();
        HttpClient client = factory.CreateClient();

        var user = await SeedRegularUserAsync(factory);
        string token = await LoginAsync(client, user.Email, ValidPassword);

        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/outbox/{Guid.NewGuid()}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        HttpResponseMessage response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetOutboxMessageById_NonExistentMessage_ReturnsNotFound404()
    {
        using SqliteApiFactory factory = await CreateFactoryAsync();
        HttpClient client = factory.CreateClient();

        var admin = await SeedAdminAsync(factory);
        string token = await LoginAsync(client, admin.Email, ValidPassword);

        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/outbox/{Guid.NewGuid()}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        HttpResponseMessage response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetOutboxMessageById_Admin_ReturnsDetail200_AndExcludesContent()
    {
        using SqliteApiFactory factory = await CreateFactoryAsync();
        HttpClient client = factory.CreateClient();

        Guid messageId = await SeedOutboxMessageAsync(factory, isProcessed: true, error: "Failure detail trace", retryCount: 3);

        var admin = await SeedAdminAsync(factory);
        string token = await LoginAsync(client, admin.Email, ValidPassword);

        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/outbox/{messageId}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        HttpResponseMessage response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        string rawJson = await response.Content.ReadAsStringAsync();
        rawJson.Should().NotContain("secret-payload-data");
        rawJson.Should().NotContain("\"content\"");

        OutboxMessageDetailDto? detail = await response.Content.ReadFromJsonAsync<OutboxMessageDetailDto>();
        detail.Should().NotBeNull();
        detail!.Id.Should().Be(messageId);
        detail.Status.Should().Be(OutboxMessageStatus.Abandoned);
        detail.Error.Should().Be("Failure detail trace");
    }

    [Fact]
    public async Task Requeue_Anonymous_ReturnsUnauthorized401()
    {
        using SqliteApiFactory factory = await CreateFactoryAsync();
        HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.PostAsync($"/api/v1/outbox/{Guid.NewGuid()}/requeue", null);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Requeue_UserWithoutPermission_ReturnsForbidden403()
    {
        using SqliteApiFactory factory = await CreateFactoryAsync();
        HttpClient client = factory.CreateClient();

        var user = await SeedRegularUserAsync(factory);
        string token = await LoginAsync(client, user.Email, ValidPassword);

        var request = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/outbox/{Guid.NewGuid()}/requeue");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        HttpResponseMessage response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Requeue_NonExistentMessage_ReturnsNotFound404()
    {
        using SqliteApiFactory factory = await CreateFactoryAsync();
        HttpClient client = factory.CreateClient();

        var admin = await SeedAdminAsync(factory);
        string token = await LoginAsync(client, admin.Email, ValidPassword);

        var request = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/outbox/{Guid.NewGuid()}/requeue");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        HttpResponseMessage response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Requeue_PendingMessage_ReturnsConflict409()
    {
        using SqliteApiFactory factory = await CreateFactoryAsync();
        HttpClient client = factory.CreateClient();

        Guid messageId = await SeedOutboxMessageAsync(factory, isProcessed: false, error: null, retryCount: 0);

        var admin = await SeedAdminAsync(factory);
        string token = await LoginAsync(client, admin.Email, ValidPassword);

        var request = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/outbox/{messageId}/requeue");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        HttpResponseMessage response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Requeue_AbandonedMessage_ReturnsNoContent204_AndResetsState()
    {
        using SqliteApiFactory factory = await CreateFactoryAsync();
        HttpClient client = factory.CreateClient();

        Guid messageId = await SeedOutboxMessageAsync(factory, isProcessed: true, error: "Permanent error", retryCount: 5);

        var admin = await SeedAdminAsync(factory);
        string token = await LoginAsync(client, admin.Email, ValidPassword);

        var request = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/outbox/{messageId}/requeue");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        HttpResponseMessage response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        using (IServiceScope verifyScope = factory.Services.CreateScope())
        {
            LastHourDbContext verifyContext = verifyScope.ServiceProvider.GetRequiredService<LastHourDbContext>();
            OutboxMessage message = await verifyContext.OutboxMessages.SingleAsync(m => m.Id == messageId);

            message.ProcessedOnUtc.Should().BeNull();
            message.RetryCount.Should().Be(0);
            message.Error.Should().BeNull();
            message.ClaimToken.Should().BeNull();
            message.LockExpiresAtUtc.Should().BeNull();
            message.NextAttemptAtUtc.Should().NotBeNull();
        }
    }
}
