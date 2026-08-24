using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using LastHour.Api.Tests.Support;
using LastHour.BuildingBlocks.Application.Models;
using LastHour.BuildingBlocks.Infrastructure.Persistence;
using LastHour.Modules.Identity.Application.Permissions;
using LastHour.Modules.Notifications.Application.Models;
using LastHour.Modules.Notifications.Domain.Notifications;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace LastHour.Api.Tests.Endpoints;

[Collection("ApiEndpoints")]
public class NotificationAdminEndpointsTests
{
    private const string ValidPassword = "correct horse battery staple";

    private sealed record LoginResponse(string AccessToken, string RefreshToken, string TokenType, int ExpiresIn);

    private static string CreateConnectionString()
        => new SqliteConnectionStringBuilder
        {
            DataSource = Path.Combine(Path.GetTempPath(), $"lasthour-notifadmin-test-{Guid.NewGuid():N}.db"),
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

    private static async Task<NotificationDelivery> SeedDeliveryAsync(SqliteApiFactory factory, DeliveryStatus status = DeliveryStatus.FailedPermanent)
    {
        using IServiceScope scope = factory.Services.CreateScope();
        LastHourDbContext dbContext = scope.ServiceProvider.GetRequiredService<LastHourDbContext>();

        var notification = Notification.Create(Guid.NewGuid(), Guid.NewGuid(), "user@lasthour.local", "Subject", "Body", "UserRegistered", DateTime.UtcNow);
        dbContext.Set<Notification>().Add(notification);

        var delivery = NotificationDelivery.Create(notification.Id, DeliveryChannel.Email, DateTime.UtcNow);
        if (status == DeliveryStatus.Delivered)
        {
            delivery.MarkDelivered(DateTime.UtcNow);
        }
        else if (status == DeliveryStatus.FailedPermanent)
        {
            delivery.RecordFailure("Fatal error", DateTime.UtcNow, 1, TimeSpan.FromMinutes(1));
        }

        dbContext.Set<NotificationDelivery>().Add(delivery);
        await dbContext.SaveChangesAsync();
        return delivery;
    }

    [Fact]
    public async Task GetNotificationDeliveries_Unauthenticated_Returns401()
    {
        using SqliteApiFactory factory = await CreateFactoryAsync();
        using HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync("/api/v1/admin/notification-deliveries");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task RequeueNotificationDelivery_Unauthenticated_Returns401()
    {
        using SqliteApiFactory factory = await CreateFactoryAsync();
        using HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.PostAsJsonAsync($"/api/v1/admin/notification-deliveries/{Guid.NewGuid()}/requeue", new { reason = "Test" });
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetNotificationDeliveries_RegularUser_Returns403()
    {
        using SqliteApiFactory factory = await CreateFactoryAsync();
        using HttpClient client = factory.CreateClient();

        var (_, userEmail) = await SeedRegularUserAsync(factory);
        string token = await LoginAsync(client, userEmail, ValidPassword);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        HttpResponseMessage response = await client.GetAsync("/api/v1/admin/notification-deliveries");
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetNotificationDeliveries_Administrator_Returns200WithDeliveries()
    {
        using SqliteApiFactory factory = await CreateFactoryAsync();
        using HttpClient client = factory.CreateClient();

        var (_, adminEmail) = await SeedAdminAsync(factory);
        var delivery = await SeedDeliveryAsync(factory);

        string token = await LoginAsync(client, adminEmail, ValidPassword);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        HttpResponseMessage response = await client.GetAsync("/api/v1/admin/notification-deliveries");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        PagedResult<NotificationDeliverySummaryDto>? payload = await response.Content.ReadFromJsonAsync<PagedResult<NotificationDeliverySummaryDto>>();
        payload.Should().NotBeNull();
        payload!.Items.Should().Contain(d => d.Id == delivery.Id);
    }

    [Fact]
    public async Task GetNotificationDeliveryDetails_ExistingId_Returns200WithDetails()
    {
        using SqliteApiFactory factory = await CreateFactoryAsync();
        using HttpClient client = factory.CreateClient();

        var (_, adminEmail) = await SeedAdminAsync(factory);
        var delivery = await SeedDeliveryAsync(factory);

        string token = await LoginAsync(client, adminEmail, ValidPassword);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        HttpResponseMessage response = await client.GetAsync($"/api/v1/admin/notification-deliveries/{delivery.Id}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        NotificationDeliveryDetailDto? payload = await response.Content.ReadFromJsonAsync<NotificationDeliveryDetailDto>();
        payload.Should().NotBeNull();
        payload!.Id.Should().Be(delivery.Id);
    }

    [Fact]
    public async Task GetNotificationDeliveryDetails_MissingId_Returns404()
    {
        using SqliteApiFactory factory = await CreateFactoryAsync();
        using HttpClient client = factory.CreateClient();

        var (_, adminEmail) = await SeedAdminAsync(factory);
        string token = await LoginAsync(client, adminEmail, ValidPassword);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        HttpResponseMessage response = await client.GetAsync($"/api/v1/admin/notification-deliveries/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task RequeueNotificationDelivery_FailedPermanent_RequeuesSuccessfully()
    {
        using SqliteApiFactory factory = await CreateFactoryAsync();
        using HttpClient client = factory.CreateClient();

        var (adminId, adminEmail) = await SeedAdminAsync(factory);
        var delivery = await SeedDeliveryAsync(factory, DeliveryStatus.FailedPermanent);

        string token = await LoginAsync(client, adminEmail, ValidPassword);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        HttpResponseMessage response = await client.PostAsJsonAsync(
            $"/api/v1/admin/notification-deliveries/{delivery.Id}/requeue",
            new { reason = "Admin manual recovery", correlationId = "corr-100" });

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify audit entry created with claims actor ID
        using IServiceScope scope = factory.Services.CreateScope();
        LastHourDbContext dbContext = scope.ServiceProvider.GetRequiredService<LastHourDbContext>();
        List<NotificationDeliveryAuditEntry> audits = await dbContext.Set<NotificationDeliveryAuditEntry>().Where(a => a.DeliveryId == delivery.Id).ToListAsync();
        audits.Should().ContainSingle();
        audits[0].ActorId.Should().Be(adminId);
    }

    [Fact]
    public async Task BatchRequeueNotificationDeliveries_MultipleDeliveries_ReturnsBatchResponse()
    {
        using SqliteApiFactory factory = await CreateFactoryAsync();
        using HttpClient client = factory.CreateClient();

        var (_, adminEmail) = await SeedAdminAsync(factory);
        var d1 = await SeedDeliveryAsync(factory, DeliveryStatus.FailedPermanent);
        var d2 = await SeedDeliveryAsync(factory, DeliveryStatus.Delivered);

        string token = await LoginAsync(client, adminEmail, ValidPassword);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/v1/admin/notification-deliveries/requeue",
            new { deliveryIds = new[] { d1.Id, d2.Id }, reason = "Batch operation" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        BatchRequeueResponseDto? payload = await response.Content.ReadFromJsonAsync<BatchRequeueResponseDto>();
        payload.Should().NotBeNull();
        payload!.RequestedCount.Should().Be(2);
        payload.RequeuedCount.Should().Be(1);
        payload.RejectedCount.Should().Be(1);
    }
}
