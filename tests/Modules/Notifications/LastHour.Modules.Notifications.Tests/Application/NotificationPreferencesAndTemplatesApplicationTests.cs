using LastHour.BuildingBlocks.Application.Contracts;
using LastHour.BuildingBlocks.Infrastructure.Persistence;
using LastHour.BuildingBlocks.Infrastructure.Persistence.Repositories;
using LastHour.BuildingBlocks.Infrastructure.Persistence.UnitOfWork;
using LastHour.BuildingBlocks.SharedKernel.Results;
using LastHour.Modules.Notifications.Application.Commands;
using LastHour.Modules.Notifications.Application.Delivery;
using LastHour.Modules.Notifications.Application.Models;
using LastHour.Modules.Notifications.Application.Persistence;
using LastHour.Modules.Notifications.Application.Queries;
using LastHour.Modules.Notifications.Domain.Notifications;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace LastHour.Modules.Notifications.Tests.Application;

public class NotificationPreferencesAndTemplatesApplicationTests
{
    private static (SqliteConnection connection, LastHourDbContext context) CreateDbContext()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<LastHourDbContext>()
            .UseSqlite(connection)
            .Options;

        var context = new LastHourDbContext(options);
        context.Database.EnsureCreated();
        return (connection, context);
    }

    [Fact]
    public async Task PreferenceResolver_DefaultSecurityEvent_ReturnsInAppAndEmail()
    {
        var (connection, context) = CreateDbContext();
        using (connection)
        using (context)
        {
            var prefRepo = new NotificationPreferenceRepository(
                new EfRepository<NotificationPreference>(context),
                new EfReadRepository<NotificationPreference, Guid>(context));

            var resolver = new NotificationPreferenceResolver(prefRepo);
            Guid userId = Guid.NewGuid();

            IReadOnlyList<DeliveryChannel> resolved = await resolver.ResolveChannelsAsync(
                userId,
                "UserPasswordChanged",
                new[] { DeliveryChannel.InApp, DeliveryChannel.Email });

            Assert.Equal(2, resolved.Count);
            Assert.Contains(DeliveryChannel.InApp, resolved);
            Assert.Contains(DeliveryChannel.Email, resolved);
        }
    }

    [Fact]
    public async Task PreferenceResolver_DefaultStandardEvent_ReturnsInAppOnly()
    {
        var (connection, context) = CreateDbContext();
        using (connection)
        using (context)
        {
            var prefRepo = new NotificationPreferenceRepository(
                new EfRepository<NotificationPreference>(context),
                new EfReadRepository<NotificationPreference, Guid>(context));

            var resolver = new NotificationPreferenceResolver(prefRepo);
            Guid userId = Guid.NewGuid();

            IReadOnlyList<DeliveryChannel> resolved = await resolver.ResolveChannelsAsync(
                userId,
                "MerchantProfileUpdated",
                new[] { DeliveryChannel.InApp });

            Assert.Single(resolved);
            Assert.Equal(DeliveryChannel.InApp, resolved[0]);
        }
    }

    [Fact]
    public async Task PreferenceResolver_UserOptOutEmailOnSecurityEvent_SuppressesEmail()
    {
        var (connection, context) = CreateDbContext();
        using (connection)
        using (context)
        {
            var prefRepo = new NotificationPreferenceRepository(
                new EfRepository<NotificationPreference>(context),
                new EfReadRepository<NotificationPreference, Guid>(context));

            Guid userId = Guid.NewGuid();
            var optOutEmail = NotificationPreference.Create(userId, "UserPasswordChanged", DeliveryChannel.Email, isEnabled: false, DateTime.UtcNow);
            context.Set<NotificationPreference>().Add(optOutEmail);
            await context.SaveChangesAsync();

            var resolver = new NotificationPreferenceResolver(prefRepo);

            IReadOnlyList<DeliveryChannel> resolved = await resolver.ResolveChannelsAsync(
                userId,
                "UserPasswordChanged",
                new[] { DeliveryChannel.InApp, DeliveryChannel.Email });

            Assert.Single(resolved);
            Assert.Equal(DeliveryChannel.InApp, resolved[0]);
        }
    }

    [Fact]
    public async Task PreferenceResolver_UserOptOutInAppOnSecurityEvent_PreservesInApp()
    {
        var (connection, context) = CreateDbContext();
        using (connection)
        using (context)
        {
            var prefRepo = new NotificationPreferenceRepository(
                new EfRepository<NotificationPreference>(context),
                new EfReadRepository<NotificationPreference, Guid>(context));

            Guid userId = Guid.NewGuid();
            var optOutInApp = NotificationPreference.Create(userId, "UserLockedOut", DeliveryChannel.InApp, isEnabled: false, DateTime.UtcNow);
            context.Set<NotificationPreference>().Add(optOutInApp);
            await context.SaveChangesAsync();

            var resolver = new NotificationPreferenceResolver(prefRepo);

            IReadOnlyList<DeliveryChannel> resolved = await resolver.ResolveChannelsAsync(
                userId,
                "UserLockedOut",
                new[] { DeliveryChannel.InApp, DeliveryChannel.Email });

            Assert.Contains(DeliveryChannel.InApp, resolved);
        }
    }

    [Fact]
    public void TemplateRenderer_SubstitutesPlaceholdersSafely()
    {
        var renderer = new NotificationTemplateRenderer();
        var template = NotificationTemplate.Create(
            "test_key",
            "UserRegistered",
            DeliveryChannel.Email,
            version: 1,
            body: "Hello {{UserName}}, welcome to {{ApplicationName}}!",
            subject: "Welcome {{UserName}}",
            isActive: true);

        var context = new NotificationTemplateContext(userName: "Alice", applicationName: "LastHour App");

        RenderedNotificationContent result = renderer.Render(template, context);

        Assert.Equal("Welcome Alice", result.subject);
        Assert.Equal("Hello Alice, welcome to LastHour App!", result.body);
    }

    [Fact]
    public void TemplateRenderer_UnknownPlaceholder_PreservesToken()
    {
        var renderer = new NotificationTemplateRenderer();
        var template = NotificationTemplate.Create(
            "test_key",
            "UserRegistered",
            DeliveryChannel.Email,
            version: 1,
            body: "Hello {{UnknownVar}}, your ID is {{UserName}}",
            isActive: true);

        var context = new NotificationTemplateContext(userName: "Bob");

        RenderedNotificationContent result = renderer.Render(template, context);

        Assert.Equal("Hello {{UnknownVar}}, your ID is Bob", result.body);
    }

    [Fact]
    public async Task QuietHoursResolver_InApp_IsNeverDeferred()
    {
        var (connection, context) = CreateDbContext();
        using (connection)
        using (context)
        {
            var repo = new NotificationQuietHoursRepository(
                new EfRepository<NotificationQuietHours>(context),
                new EfReadRepository<NotificationQuietHours, Guid>(context));

            Guid userId = Guid.NewGuid();
            var quietHours = NotificationQuietHours.Create(userId, enabled: true, new TimeOnly(22, 0), new TimeOnly(7, 0), "UTC", DateTime.UtcNow);
            context.Set<NotificationQuietHours>().Add(quietHours);
            await context.SaveChangesAsync();

            var resolver = new NotificationQuietHoursResolver(repo);
            DateTime at23 = new DateTime(2026, 8, 20, 23, 0, 0, DateTimeKind.Utc);

            DateTime? scheduledAt = await resolver.CalculateScheduledAttemptAtAsync(userId, DeliveryChannel.InApp, at23);

            Assert.Equal(at23, scheduledAt);
        }
    }

    [Fact]
    public async Task QuietHoursResolver_EmailInQuietHours_DefersToEligibleUtc()
    {
        var (connection, context) = CreateDbContext();
        using (connection)
        using (context)
        {
            var repo = new NotificationQuietHoursRepository(
                new EfRepository<NotificationQuietHours>(context),
                new EfReadRepository<NotificationQuietHours, Guid>(context));

            Guid userId = Guid.NewGuid();
            var quietHours = NotificationQuietHours.Create(userId, enabled: true, new TimeOnly(22, 0), new TimeOnly(7, 0), "UTC", DateTime.UtcNow);
            context.Set<NotificationQuietHours>().Add(quietHours);
            await context.SaveChangesAsync();

            var resolver = new NotificationQuietHoursResolver(repo);
            DateTime at23 = new DateTime(2026, 8, 20, 23, 0, 0, DateTimeKind.Utc);

            DateTime? scheduledAt = await resolver.CalculateScheduledAttemptAtAsync(userId, DeliveryChannel.Email, at23);

            Assert.NotNull(scheduledAt);
            Assert.True(scheduledAt > at23);
            Assert.Equal(7, scheduledAt.Value.Hour);
        }
    }

    [Fact]
    public async Task CQRS_UpdateNotificationPreference_UpsertsPreference()
    {
        var (connection, context) = CreateDbContext();
        using (connection)
        using (context)
        {
            var prefRepo = new NotificationPreferenceRepository(
                new EfRepository<NotificationPreference>(context),
                new EfReadRepository<NotificationPreference, Guid>(context));
            var uow = new EfUnitOfWork(context);
            var clock = new TestClock(DateTime.UtcNow);

            var handler = new UpdateNotificationPreferenceCommandHandler(prefRepo, uow, clock);
            Guid userId = Guid.NewGuid();

            var command1 = new UpdateNotificationPreferenceCommand(userId, "UserRegistered", DeliveryChannel.Email, isEnabled: true);
            Result result1 = await handler.Handle(command1, CancellationToken.None);
            Assert.True(result1.IsSuccess);

            context.ChangeTracker.Clear();

            NotificationPreference? saved1 = await prefRepo.GetByUserAndTypeAndChannelAsync(userId, "UserRegistered", DeliveryChannel.Email);
            Assert.NotNull(saved1);
            Assert.True(saved1.IsEnabled);

            context.ChangeTracker.Clear();

            var command2 = new UpdateNotificationPreferenceCommand(userId, "UserRegistered", DeliveryChannel.Email, isEnabled: false);
            Result result2 = await handler.Handle(command2, CancellationToken.None);
            Assert.True(result2.IsSuccess);

            NotificationPreference? saved2 = await prefRepo.GetByUserAndTypeAndChannelAsync(userId, "UserRegistered", DeliveryChannel.Email);
            Assert.NotNull(saved2);
            Assert.False(saved2.IsEnabled);
        }
    }

    [Fact]
    public async Task CQRS_GetMyNotificationPreferences_IsUserScoped()
    {
        var (connection, context) = CreateDbContext();
        using (connection)
        using (context)
        {
            var prefRepo = new NotificationPreferenceRepository(
                new EfRepository<NotificationPreference>(context),
                new EfReadRepository<NotificationPreference, Guid>(context));

            Guid user1 = Guid.NewGuid();
            Guid user2 = Guid.NewGuid();

            context.Set<NotificationPreference>().Add(NotificationPreference.Create(user1, "UserRegistered", DeliveryChannel.Email, true, DateTime.UtcNow));
            context.Set<NotificationPreference>().Add(NotificationPreference.Create(user2, "UserRegistered", DeliveryChannel.Sms, true, DateTime.UtcNow));
            await context.SaveChangesAsync();

            var queryHandler = new GetMyNotificationPreferencesQueryHandler(prefRepo);

            Result<IReadOnlyList<NotificationPreferenceDto>> result1 = await queryHandler.Handle(new GetMyNotificationPreferencesQuery(user1), CancellationToken.None);

            Assert.True(result1.IsSuccess);
            Assert.Single(result1.Value);
            Assert.Equal("Email", result1.Value[0].channel);
        }
    }

    private sealed class TestClock : IClock
    {
        public DateTime UtcNow { get; }

        public TestClock(DateTime now) => UtcNow = now;
    }
}
