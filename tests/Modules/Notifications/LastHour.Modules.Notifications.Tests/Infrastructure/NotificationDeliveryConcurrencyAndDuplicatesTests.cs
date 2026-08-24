using System.Data.Common;
using LastHour.BuildingBlocks.Infrastructure.Persistence;
using LastHour.BuildingBlocks.Infrastructure.Persistence.Repositories;
using LastHour.Modules.Notifications.Application.Persistence;
using LastHour.Modules.Notifications.Domain.Notifications;
using LastHour.Modules.Notifications.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace LastHour.Modules.Notifications.Tests.Infrastructure;

public sealed class NotificationDeliveryConcurrencyAndDuplicatesTests : IDisposable
{
    private readonly DbConnection _connection;
    private readonly DbContextOptions<LastHourDbContext> _dbContextOptions;

    public NotificationDeliveryConcurrencyAndDuplicatesTests()
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
    public async Task ConcurrentDeliveryCreation_ProducesOneIntent()
    {
        // Arrange
        DateTime now = DateTime.UtcNow;
        Guid notificationId = Guid.NewGuid();

        using (LastHourDbContext seedContext = CreateDbContext())
        {
            var notification = Notification.Create(Guid.NewGuid(), Guid.NewGuid(), "u@domain.com", "Title", "Body", "UserRegistered", now);
            seedContext.Set<Notification>().Add(notification);
            await seedContext.SaveChangesAsync();
            notificationId = notification.Id;
        }

        int successCount = 0;
        int duplicateFailureCount = 0;

        // Act: 10 concurrent insert attempts for the same NotificationId + Channel
        var tasks = Enumerable.Range(0, 10).Select(async _ =>
        {
            try
            {
                using LastHourDbContext context = CreateDbContext();
                var repo = new NotificationDeliveryRepository(
                    new EfRepository<NotificationDelivery>(context),
                    new EfReadRepository<NotificationDelivery, Guid>(context));

                var delivery = NotificationDelivery.Create(notificationId, DeliveryChannel.InApp, now);
                await repo.AddAsync(delivery);
                await context.SaveChangesAsync();

                Interlocked.Increment(ref successCount);
            }
            catch (DbUpdateException ex) when (ex.InnerException is SqliteException sqliteEx && sqliteEx.SqliteErrorCode == 19)
            {
                Interlocked.Increment(ref duplicateFailureCount);
            }
        });

        await Task.WhenAll(tasks);

        // Assert
        Assert.Equal(1, successCount); // Exactly 1 insert succeeded
        Assert.Equal(9, duplicateFailureCount); // Exactly 9 rejected by unique index constraint

        using LastHourDbContext verifyContext = CreateDbContext();
        List<NotificationDelivery> deliveries = await verifyContext.Set<NotificationDelivery>()
            .Where(d => d.NotificationId == notificationId && d.Channel == DeliveryChannel.InApp)
            .ToListAsync();

        Assert.Single(deliveries); // Only 1 delivery intent exists in DB
    }

    [Fact]
    public async Task DuplicateUniqueConstraint_IsHandled()
    {
        // Arrange
        DateTime now = DateTime.UtcNow;
        Guid notificationId;

        using (LastHourDbContext seedContext = CreateDbContext())
        {
            var notification = Notification.Create(Guid.NewGuid(), Guid.NewGuid(), "u@domain.com", "Title", "Body", "UserRegistered", now);
            seedContext.Set<Notification>().Add(notification);
            await seedContext.SaveChangesAsync();
            notificationId = notification.Id;

            var repo = new NotificationDeliveryRepository(
                new EfRepository<NotificationDelivery>(seedContext),
                new EfReadRepository<NotificationDelivery, Guid>(seedContext));

            var delivery1 = NotificationDelivery.Create(notificationId, DeliveryChannel.Email, now);
            await repo.AddAsync(delivery1);
            await seedContext.SaveChangesAsync();
        }

        // Act & Assert
        using (LastHourDbContext context = CreateDbContext())
        {
            var repo = new NotificationDeliveryRepository(
                new EfRepository<NotificationDelivery>(context),
                new EfReadRepository<NotificationDelivery, Guid>(context));

            var duplicate = NotificationDelivery.Create(notificationId, DeliveryChannel.Email, now);
            await repo.AddAsync(duplicate);

            await Assert.ThrowsAsync<DbUpdateException>(async () => await context.SaveChangesAsync());
        }
    }

    [Fact]
    public async Task UnrelatedDatabaseFailure_IsNotSwallowed()
    {
        // Arrange: Create invalid delivery with empty NotificationId (bypassing constructor validation via reflection or invalid entity setup if possible)
        // Here we test that a foreign key violation to non-existent notification ID throws DbUpdateException
        DateTime now = DateTime.UtcNow;
        Guid nonExistentNotificationId = Guid.NewGuid();

        using (LastHourDbContext context = CreateDbContext())
        {
            var repo = new NotificationDeliveryRepository(
                new EfRepository<NotificationDelivery>(context),
                new EfReadRepository<NotificationDelivery, Guid>(context));

            var delivery = NotificationDelivery.Create(nonExistentNotificationId, DeliveryChannel.Email, now);
            await repo.AddAsync(delivery);

            // Act & Assert
            // Unrelated FK failure must throw DbUpdateException and NOT be swallowed
            await Assert.ThrowsAsync<DbUpdateException>(async () => await context.SaveChangesAsync());
        }
    }
}
