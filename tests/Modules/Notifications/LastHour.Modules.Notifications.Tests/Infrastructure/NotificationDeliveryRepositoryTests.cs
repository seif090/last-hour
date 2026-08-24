using System.Data.Common;
using LastHour.BuildingBlocks.Infrastructure.Persistence;
using LastHour.BuildingBlocks.Infrastructure.Persistence.Repositories;
using LastHour.Modules.Notifications.Application.Persistence;
using LastHour.Modules.Notifications.Domain.Notifications;
using LastHour.Modules.Notifications.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace LastHour.Modules.Notifications.Tests.Infrastructure;

public sealed class NotificationDeliveryRepositoryTests : IDisposable
{
    private readonly DbConnection _connection;
    private readonly DbContextOptions<LastHourDbContext> _dbContextOptions;

    public NotificationDeliveryRepositoryTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        _dbContextOptions = new DbContextOptionsBuilder<LastHourDbContext>()
            .UseSqlite(_connection)
            .Options;

        using LastHourDbContext context = CreateDbContext();
        context.Database.EnsureCreated();
    }

    public void Dispose()
    {
        _connection.Dispose();
        GC.SuppressFinalize(this);
    }

    private LastHourDbContext CreateDbContext()
        => new(_dbContextOptions, new[] { typeof(NotificationConfiguration).Assembly });

    [Fact]
    public async Task AddAsync_And_GetByIdAsync_ShouldPersistAndRetrieveDelivery()
    {
        // Arrange
        Guid notificationId = Guid.NewGuid();
        DateTime now = DateTime.UtcNow;

        using (LastHourDbContext seedContext = CreateDbContext())
        {
            var notification = Notification.Create(Guid.NewGuid(), notificationId, "recip@lasthour.local", "Test Title", "Test Body", "UserRegistered", now);
            seedContext.Set<Notification>().Add(notification);
            await seedContext.SaveChangesAsync();

            var repo = new NotificationDeliveryRepository(
                new EfRepository<NotificationDelivery>(seedContext),
                new EfReadRepository<NotificationDelivery, Guid>(seedContext));

            var delivery = NotificationDelivery.Create(notification.Id, DeliveryChannel.Email, now);
            await repo.AddAsync(delivery);
            await seedContext.SaveChangesAsync();

            // Act
            NotificationDelivery? result = await repo.GetByIdAsync(delivery.Id);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(notification.Id, result.NotificationId);
            Assert.Equal(DeliveryChannel.Email, result.Channel);
            Assert.Equal(DeliveryStatus.Pending, result.Status);
        }
    }

    [Fact]
    public async Task UniqueConstraint_ShouldPreventDuplicateDeliveriesPerNotificationAndChannel()
    {
        // Arrange
        Guid recipientId = Guid.NewGuid();
        DateTime now = DateTime.UtcNow;

        using (LastHourDbContext context = CreateDbContext())
        {
            var notification = Notification.Create(Guid.NewGuid(), recipientId, "recip@lasthour.local", "Title", "Body", "UserRegistered", now);
            context.Set<Notification>().Add(notification);
            await context.SaveChangesAsync();

            var repo = new NotificationDeliveryRepository(
                new EfRepository<NotificationDelivery>(context),
                new EfReadRepository<NotificationDelivery, Guid>(context));

            var delivery1 = NotificationDelivery.Create(notification.Id, DeliveryChannel.InApp, now);
            var delivery2 = NotificationDelivery.Create(notification.Id, DeliveryChannel.InApp, now);

            await repo.AddAsync(delivery1);
            await context.SaveChangesAsync();

            await repo.AddAsync(delivery2);

            // Act & Assert
            await Assert.ThrowsAsync<DbUpdateException>(async () => await context.SaveChangesAsync());
        }
    }

    [Fact]
    public async Task GetPendingDeliveriesAsync_ShouldReturnOnlyEligibleItems()
    {
        // Arrange
        DateTime now = DateTime.UtcNow;
        Guid recipientId1 = Guid.NewGuid();
        Guid recipientId2 = Guid.NewGuid();

        using (LastHourDbContext context = CreateDbContext())
        {
            var n1 = Notification.Create(Guid.NewGuid(), recipientId1, "u1", "T1", "B1", "UserRegistered", now);
            var n2 = Notification.Create(Guid.NewGuid(), recipientId2, "u2", "T2", "B2", "UserRegistered", now);
            context.Set<Notification>().AddRange(n1, n2);
            await context.SaveChangesAsync();

            var repo = new NotificationDeliveryRepository(
                new EfRepository<NotificationDelivery>(context),
                new EfReadRepository<NotificationDelivery, Guid>(context));

            // Eligible pending
            var d1 = NotificationDelivery.Create(n1.Id, DeliveryChannel.InApp, now.AddMinutes(-5));

            // Delivered (ineligible)
            var d2 = NotificationDelivery.Create(n1.Id, DeliveryChannel.Email, now.AddMinutes(-5));
            d2.MarkDelivered(now);

            // Future scheduled retry (ineligible)
            var d3 = NotificationDelivery.Create(n2.Id, DeliveryChannel.Sms, now);
            d3.RecordFailure("Temp error", now, maxRetryCount: 3, retryDelay: TimeSpan.FromHours(1));

            await repo.AddAsync(d1);
            await repo.AddAsync(d2);
            await repo.AddAsync(d3);
            await context.SaveChangesAsync();

            // Act
            IReadOnlyList<NotificationDelivery> pending = await repo.GetPendingDeliveriesAsync(batchSize: 10, nowUtc: now);

            // Assert
            Assert.Single(pending);
            Assert.Equal(d1.Id, pending[0].Id);
        }
    }
}
