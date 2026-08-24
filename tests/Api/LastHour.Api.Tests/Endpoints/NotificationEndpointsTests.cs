using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using LastHour.Api.Tests.Support;
using LastHour.BuildingBlocks.Application.Models;
using LastHour.BuildingBlocks.Infrastructure.Persistence;
using LastHour.Modules.Notifications.Application.Models;
using LastHour.Modules.Notifications.Domain.Notifications;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;

namespace LastHour.Api.Tests.Endpoints;

[Collection("ApiEndpoints")]
public class NotificationEndpointsTests
{
    private const string ValidPassword = "correct horse battery staple";

    private sealed record LoginResponse(string AccessToken, string RefreshToken, string TokenType, int ExpiresIn);

    private static string CreateConnectionString()
        => new SqliteConnectionStringBuilder
        {
            DataSource = Path.Combine(Path.GetTempPath(), $"lasthour-notifications-test-{Guid.NewGuid():N}.db"),
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

    private static async Task<(Guid UserId, string Email)> SeedUserAsync(SqliteApiFactory factory)
    {
        string email = $"user-{Guid.NewGuid():N}@example.com";
        Guid userId = await UserSeeder.SeedUserAsync(factory, email, ValidPassword);
        return (userId, email);
    }

    private static async Task<Guid> SeedNotificationAsync(
        SqliteApiFactory factory,
        Guid recipientId,
        string title = "Welcome",
        bool isRead = false)
    {
        using IServiceScope scope = factory.Services.CreateScope();
        LastHourDbContext context = scope.ServiceProvider.GetRequiredService<LastHourDbContext>();

        var notification = Notification.Create(
            null,
            recipientId,
            "recipient@example.com",
            title,
            "Notification body text",
            "UserRegistered",
            DateTime.UtcNow);

        if (isRead)
        {
            notification.MarkAsRead(DateTime.UtcNow);
        }

        context.Set<Notification>().Add(notification);
        await context.SaveChangesAsync();
        return notification.Id;
    }

    [Fact]
    public async Task GetNotifications_Anonymous_ReturnsUnauthorized401()
    {
        using SqliteApiFactory factory = await CreateFactoryAsync();
        HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync("/api/v1/notifications");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetNotifications_Authenticated_ReturnsOnlyOwnNotifications()
    {
        using SqliteApiFactory factory = await CreateFactoryAsync();
        HttpClient client = factory.CreateClient();

        var (userA, emailA) = await SeedUserAsync(factory);
        var (userB, _) = await SeedUserAsync(factory);

        Guid notifA = await SeedNotificationAsync(factory, userA, "User A Notif");
        Guid notifB = await SeedNotificationAsync(factory, userB, "User B Notif");

        string tokenA = await LoginAsync(client, emailA, ValidPassword);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenA);

        HttpResponseMessage response = await client.GetAsync("/api/v1/notifications");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        PagedResult<NotificationDto>? payload = await response.Content.ReadFromJsonAsync<PagedResult<NotificationDto>>();
        payload.Should().NotBeNull();
        payload!.Items.Should().ContainSingle();
        payload.Items[0].Id.Should().Be(notifA);
        payload.Items[0].Title.Should().Be("User A Notif");
        payload.TotalCount.Should().Be(1);
    }

    [Fact]
    public async Task MarkAsRead_WhenNotificationBelongsToUser_ReturnsNoContent204()
    {
        using SqliteApiFactory factory = await CreateFactoryAsync();
        HttpClient client = factory.CreateClient();

        var (userId, email) = await SeedUserAsync(factory);
        Guid notifId = await SeedNotificationAsync(factory, userId, "Unread Notif", isRead: false);

        string token = await LoginAsync(client, email, ValidPassword);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        HttpResponseMessage response = await client.PutAsync($"/api/v1/notifications/{notifId}/read", null);
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify read state
        HttpResponseMessage queryResponse = await client.GetAsync("/api/v1/notifications");
        PagedResult<NotificationDto>? payload = await queryResponse.Content.ReadFromJsonAsync<PagedResult<NotificationDto>>();
        payload!.Items[0].IsRead.Should().BeTrue();
    }

    [Fact]
    public async Task MarkAsRead_WhenNotificationBelongsToAnotherUser_ReturnsNotFound404()
    {
        using SqliteApiFactory factory = await CreateFactoryAsync();
        HttpClient client = factory.CreateClient();

        var (userA, _) = await SeedUserAsync(factory);
        var (userB, emailB) = await SeedUserAsync(factory);

        Guid notifA = await SeedNotificationAsync(factory, userA, "User A Notif");

        string tokenB = await LoginAsync(client, emailB, ValidPassword);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenB);

        HttpResponseMessage response = await client.PutAsync($"/api/v1/notifications/{notifA}/read", null);
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task MarkAllAsRead_Authenticated_MarksAllUnreadAndReturnsCount()
    {
        using SqliteApiFactory factory = await CreateFactoryAsync();
        HttpClient client = factory.CreateClient();

        var (userId, email) = await SeedUserAsync(factory);
        var (otherUser, _) = await SeedUserAsync(factory);

        await SeedNotificationAsync(factory, userId, "Notif 1", isRead: false);
        await SeedNotificationAsync(factory, userId, "Notif 2", isRead: false);
        await SeedNotificationAsync(factory, otherUser, "Other Notif", isRead: false);

        string token = await LoginAsync(client, email, ValidPassword);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        HttpResponseMessage response = await client.PostAsync("/api/v1/notifications/read-all", null);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        MarkAllNotificationsAsReadResponse? payload = await response.Content.ReadFromJsonAsync<MarkAllNotificationsAsReadResponse>();
        payload.Should().NotBeNull();
        payload!.UpdatedCount.Should().Be(2);

        // Verify querying unread returns empty
        HttpResponseMessage unreadQueryResponse = await client.GetAsync("/api/v1/notifications?isRead=false");
        PagedResult<NotificationDto>? unreadPayload = await unreadQueryResponse.Content.ReadFromJsonAsync<PagedResult<NotificationDto>>();
        unreadPayload!.Items.Should().BeEmpty();
    }
}
