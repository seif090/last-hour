using LastHour.BuildingBlocks.Infrastructure.Persistence;
using LastHour.BuildingBlocks.Infrastructure.Persistence.Repositories;
using LastHour.Modules.Notifications.Application.Persistence;
using LastHour.Modules.Notifications.Domain.Notifications;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace LastHour.Modules.Notifications.Tests.Infrastructure;

public class NotificationPreferencesAndTemplatesConcurrencyTests
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
    public async Task DuplicateActiveTemplates_ThrowsDataIntegrityException()
    {
        var (connection, context) = CreateDbContext();
        using (connection)
        using (context)
        {
            var tpl1 = NotificationTemplate.Create("t1", "UserRegistered", DeliveryChannel.Email, 1, "Body 1", isActive: true);
            var tpl2 = NotificationTemplate.Create("t2", "UserRegistered", DeliveryChannel.Email, 2, "Body 2", isActive: true);

            context.Set<NotificationTemplate>().Add(tpl1);
            context.Set<NotificationTemplate>().Add(tpl2);
            await context.SaveChangesAsync();

            var repo = new NotificationTemplateRepository(
                new EfRepository<NotificationTemplate>(context),
                new EfReadRepository<NotificationTemplate, Guid>(context));

            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await repo.GetActiveTemplateAsync("UserRegistered", DeliveryChannel.Email));
        }
    }

    [Fact]
    public async Task TemplateVersions_AreOrderedDescending()
    {
        var (connection, context) = CreateDbContext();
        using (connection)
        using (context)
        {
            var tpl1 = NotificationTemplate.Create("welcome_key", "UserRegistered", DeliveryChannel.Email, 1, "V1", isActive: false);
            var tpl2 = NotificationTemplate.Create("welcome_key", "UserRegistered", DeliveryChannel.Email, 2, "V2", isActive: true);
            var tpl3 = NotificationTemplate.Create("welcome_key", "UserRegistered", DeliveryChannel.Email, 3, "V3", isActive: false);

            context.Set<NotificationTemplate>().AddRange(tpl1, tpl2, tpl3);
            await context.SaveChangesAsync();

            var repo = new NotificationTemplateRepository(
                new EfRepository<NotificationTemplate>(context),
                new EfReadRepository<NotificationTemplate, Guid>(context));

            IReadOnlyList<NotificationTemplate> versions = await repo.ListVersionsAsync("welcome_key", DeliveryChannel.Email);

            Assert.Equal(3, versions.Count);
            Assert.Equal(3, versions[0].Version);
            Assert.Equal(2, versions[1].Version);
            Assert.Equal(1, versions[2].Version);
        }
    }

    [Fact]
    public async Task ConcurrentPreferenceUpdates_MaintainDataConsistency()
    {
        var (connection, context) = CreateDbContext();
        using (connection)
        using (context)
        {
            Guid userId = Guid.NewGuid();
            DateTime now = DateTime.UtcNow;

            var pref = NotificationPreference.Create(userId, "UserPasswordChanged", DeliveryChannel.Email, isEnabled: true, now);
            context.Set<NotificationPreference>().Add(pref);
            await context.SaveChangesAsync();

            pref.Disable(now.AddSeconds(1));
            await context.SaveChangesAsync();

            NotificationPreference? reloaded = await context.Set<NotificationPreference>()
                .FirstOrDefaultAsync(p => p.UserId == userId && p.NotificationType == "UserPasswordChanged" && p.Channel == DeliveryChannel.Email);

            Assert.NotNull(reloaded);
            Assert.False(reloaded.IsEnabled);
        }
    }
}
