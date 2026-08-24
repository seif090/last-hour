using FluentAssertions;
using LastHour.BuildingBlocks.Application.Contracts;
using LastHour.BuildingBlocks.Infrastructure.Persistence;
using LastHour.BuildingBlocks.Infrastructure.Persistence.Repositories;
using LastHour.BuildingBlocks.Infrastructure.Persistence.UnitOfWork;
using LastHour.BuildingBlocks.SharedKernel.Results;
using LastHour.Modules.Notifications.Application.Commands;
using LastHour.Modules.Notifications.Application.Persistence;
using LastHour.Modules.Notifications.Application.Queries;
using LastHour.Modules.Notifications.Domain.Notifications;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace LastHour.Modules.Notifications.Tests.Application.Consumers;

public class NotificationConcurrencyTests : IDisposable
{
    private readonly string _connectionString;
    private readonly SqliteConnection _keepAliveConnection;
    private readonly DateTime _baseTime = new(2026, 8, 20, 10, 0, 0, DateTimeKind.Utc);

    public NotificationConcurrencyTests()
    {
        string dbName = "notif_concurrency_" + Guid.NewGuid().ToString("N");
        _connectionString = $"Data Source={dbName};Mode=Memory;Cache=Shared";
        _keepAliveConnection = new SqliteConnection(_connectionString);
        _keepAliveConnection.Open();

        using LastHourDbContext context = CreateDbContext();
        context.Database.EnsureCreated();
    }

    public void Dispose()
    {
        _keepAliveConnection.Dispose();
        GC.SuppressFinalize(this);
    }

    private LastHourDbContext CreateDbContext()
    {
        DbContextOptions<LastHourDbContext> options = new DbContextOptionsBuilder<LastHourDbContext>()
            .UseSqlite(_connectionString)
            .Options;

        return new LastHourDbContext(
            options,
            new[]
            {
                typeof(Notification).Assembly,
            });
    }

    [Fact]
    public async Task ConcurrentMarkAsRead_SameNotification_ResolvesIdempotentlyWithoutException()
    {
        Guid userId = Guid.NewGuid();
        Guid notificationId;

        using (LastHourDbContext seedContext = CreateDbContext())
        {
            Notification notification = Notification.Create(
                null,
                userId,
                "user@example.com",
                "Concurrent Test",
                "Body",
                "UserRegistered",
                _baseTime);
            seedContext.Set<Notification>().Add(notification);
            await seedContext.SaveChangesAsync();
            notificationId = notification.Id;
        }

        // Two independent contexts execute MarkNotificationAsRead simultaneously
        Task<Result> task1 = Task.Run(async () =>
        {
            using LastHourDbContext ctx = CreateDbContext();
            var repo = new NotificationRepository(
                new EfRepository<Notification>(ctx),
                new EfReadRepository<Notification, Guid>(ctx),
                new EfTrackedRepository<Notification, Guid>(ctx));
            var uow = new EfUnitOfWork(ctx);
            var clock = new FixedClock(_baseTime.AddSeconds(1));
            var handler = new MarkNotificationAsReadCommandHandler(repo, uow, clock);
            return await handler.Handle(new MarkNotificationAsReadCommand(notificationId, userId), CancellationToken.None);
        });

        Task<Result> task2 = Task.Run(async () =>
        {
            using LastHourDbContext ctx = CreateDbContext();
            var repo = new NotificationRepository(
                new EfRepository<Notification>(ctx),
                new EfReadRepository<Notification, Guid>(ctx),
                new EfTrackedRepository<Notification, Guid>(ctx));
            var uow = new EfUnitOfWork(ctx);
            var clock = new FixedClock(_baseTime.AddSeconds(2));
            var handler = new MarkNotificationAsReadCommandHandler(repo, uow, clock);
            return await handler.Handle(new MarkNotificationAsReadCommand(notificationId, userId), CancellationToken.None);
        });

        Result[] results = await Task.WhenAll(task1, task2);

        results.Should().OnlyContain(r => r.IsSuccess);

        using (LastHourDbContext verifyContext = CreateDbContext())
        {
            Notification finalNotification = await verifyContext.Set<Notification>().SingleAsync(n => n.Id == notificationId);
            finalNotification.IsRead.Should().BeTrue();
            finalNotification.ReadAtUtc.Should().NotBeNull();
        }
    }

    [Fact]
    public async Task ConcurrentMarkAllAndQuery_ExecutesSafelyWithoutStateCorruption()
    {
        Guid userId = Guid.NewGuid();

        using (LastHourDbContext seedContext = CreateDbContext())
        {
            for (int i = 0; i < 10; i++)
            {
                Notification notification = Notification.Create(
                    null,
                    userId,
                    "user@example.com",
                    $"Title {i}",
                    "Body",
                    "UserRegistered",
                    _baseTime.AddMinutes(i));
                seedContext.Set<Notification>().Add(notification);
            }

            await seedContext.SaveChangesAsync();
        }

        Task markAllTask = Task.Run(async () =>
        {
            using LastHourDbContext ctx = CreateDbContext();
            var repo = new NotificationRepository(
                new EfRepository<Notification>(ctx),
                new EfReadRepository<Notification, Guid>(ctx),
                new EfTrackedRepository<Notification, Guid>(ctx));
            var uow = new EfUnitOfWork(ctx);
            var clock = new FixedClock(_baseTime.AddMinutes(30));
            var handler = new MarkAllNotificationsAsReadCommandHandler(repo, uow, clock);
            await handler.Handle(new MarkAllNotificationsAsReadCommand(userId), CancellationToken.None);
        });

        Task queryTask = Task.Run(async () =>
        {
            using LastHourDbContext ctx = CreateDbContext();
            var repo = new NotificationRepository(
                new EfRepository<Notification>(ctx),
                new EfReadRepository<Notification, Guid>(ctx),
                new EfTrackedRepository<Notification, Guid>(ctx));
            var handler = new GetMyNotificationsQueryHandler(repo);
            await handler.Handle(new GetMyNotificationsQuery(userId, pageNumber: 1, pageSize: 20), CancellationToken.None);
        });

        await Task.WhenAll(markAllTask, queryTask);

        using (LastHourDbContext verifyContext = CreateDbContext())
        {
            List<Notification> all = await verifyContext.Set<Notification>().Where(n => n.RecipientId == userId).ToListAsync();
            all.Should().OnlyContain(n => n.IsRead);
        }
    }

    private sealed class FixedClock : IClock
    {
        public FixedClock(DateTime utcNow)
        {
            UtcNow = utcNow;
        }

        public DateTime UtcNow { get; }
    }
}
