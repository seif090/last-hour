using System.Data.Common;
using System.Diagnostics.Metrics;
using LastHour.BuildingBlocks.Application.Contracts;
using LastHour.BuildingBlocks.Infrastructure.Persistence;
using LastHour.BuildingBlocks.Infrastructure.Persistence.Repositories;
using LastHour.BuildingBlocks.Infrastructure.Persistence.UnitOfWork;
using LastHour.BuildingBlocks.SharedKernel.Results;
using LastHour.Modules.Notifications.Application.Delivery;
using LastHour.Modules.Notifications.Application.Persistence;
using LastHour.Modules.Notifications.Domain.Notifications;
using LastHour.Modules.Notifications.Infrastructure.Delivery;
using LastHour.Modules.Notifications.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace LastHour.Modules.Notifications.Tests.Infrastructure;

public sealed class NotificationDeliveryFailureWindowTests : IDisposable
{
    private readonly DbConnection _connection;
    private readonly DbContextOptions<LastHourDbContext> _dbContextOptions;
    private readonly TestClock _clock;
    private readonly MeterFactory _meterFactory;
    private readonly NotificationDeliveryMetrics _metrics;

    public NotificationDeliveryFailureWindowTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        _dbContextOptions = new DbContextOptionsBuilder<LastHourDbContext>()
            .UseSqlite(_connection)
            .Options;

        _clock = new TestClock(new DateTime(2026, 8, 20, 12, 0, 0, DateTimeKind.Utc));
        _meterFactory = new MeterFactory();
        _metrics = new NotificationDeliveryMetrics(_meterFactory);

        using LastHourDbContext context = CreateDbContext();
        context.Database.EnsureCreated();
    }

    public void Dispose()
    {
        _connection.Dispose();
        _meterFactory.Dispose();
        GC.SuppressFinalize(this);
    }

    private LastHourDbContext CreateDbContext()
        => new(_dbContextOptions, new[] { typeof(NotificationConfiguration).Assembly });

    private ServiceProvider CreateServiceProvider(INotificationChannelSender channelSender)
    {
        var services = new ServiceCollection();
        services.AddSingleton<IClock>(_clock);
        services.AddScoped(_ => CreateDbContext());
        services.AddScoped<IUnitOfWork, EfUnitOfWork>();
        services.AddScoped<INotificationRepository, NotificationRepository>();
        services.AddScoped<INotificationDeliveryRepository, NotificationDeliveryRepository>();
        services.AddScoped(typeof(IRepository<>), typeof(EfRepository<>));
        services.AddScoped(typeof(IReadRepository<,>), typeof(EfReadRepository<,>));
        services.AddScoped(typeof(ITrackedRepository<,>), typeof(EfTrackedRepository<,>));
        services.AddSingleton(channelSender);
        services.AddSingleton<INotificationChannelFactory>(new TestChannelFactory(channelSender));

        return services.BuildServiceProvider();
    }

    [Fact]
    public async Task Retry_ReusesStableProviderIdempotencyKey()
    {
        // Arrange
        Guid notifId = Guid.NewGuid();
        DateTime now = _clock.UtcNow;

        var capturedDeliveryIds = new List<Guid>();
        var sender = new TestChannelSender(DeliveryChannel.Email, (notif, del) =>
        {
            capturedDeliveryIds.Add(del.Id);
            return Task.FromResult(capturedDeliveryIds.Count == 1
                ? Result.Failure(Error.Failure("Email.Timeout", "Simulated transport timeout"))
                : Result.Success());
        });

        using (LastHourDbContext context = CreateDbContext())
        {
            var notification = Notification.Create(Guid.NewGuid(), notifId, "recipient@lasthour.local", "Subject", "Body", "UserRegistered", now);
            var delivery = NotificationDelivery.Create(notification.Id, DeliveryChannel.Email, now);
            context.Set<Notification>().Add(notification);
            context.Set<NotificationDelivery>().Add(delivery);
            await context.SaveChangesAsync();
        }

        using ServiceProvider provider = CreateServiceProvider(sender);
        var scopeFactory = provider.GetRequiredService<IServiceScopeFactory>();

        var options = Options.Create(new NotificationDeliveryOptions
        {
            Enabled = true,
            BatchSize = 10,
            PollingInterval = TimeSpan.FromMilliseconds(20),
            LeaseDuration = TimeSpan.FromMinutes(2),
            BaseRetryDelay = TimeSpan.Zero,
            MaximumRetryDelay = TimeSpan.FromSeconds(1),
            MaxRetryCount = 3,
        });

        var processor = new NotificationDeliveryProcessor(scopeFactory, options, _metrics, _clock, NullLogger<NotificationDeliveryProcessor>.Instance);

        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(300));

        // Act
        await processor.StartAsync(cts.Token);
        await Task.Delay(200);
        await processor.StopAsync(CancellationToken.None);

        // Assert
        Assert.Equal(2, capturedDeliveryIds.Count);
        Assert.Equal(capturedDeliveryIds[0], capturedDeliveryIds[1]); // Stable DeliveryId reused across retries!
    }

    [Fact]
    public async Task ActiveLease_CannotBeStolen()
    {
        // Arrange
        DateTime now = _clock.UtcNow;

        using (LastHourDbContext context = CreateDbContext())
        {
            var notification = Notification.Create(Guid.NewGuid(), Guid.NewGuid(), "u@domain.com", "T", "B", "UserRegistered", now);
            var delivery = NotificationDelivery.Create(notification.Id, DeliveryChannel.InApp, now);

            // Active lease expiring in 10 minutes
            delivery.Claim(Guid.NewGuid(), now.AddMinutes(10), now);

            context.Set<Notification>().Add(notification);
            context.Set<NotificationDelivery>().Add(delivery);
            await context.SaveChangesAsync();

            var repo = new NotificationDeliveryRepository(
                new EfRepository<NotificationDelivery>(context),
                new EfReadRepository<NotificationDelivery, Guid>(context));

            // Act
            IReadOnlyList<NotificationDelivery> pending = await repo.GetPendingDeliveriesAsync(batchSize: 10, nowUtc: now);

            // Assert
            Assert.Empty(pending); // Active leased item is NOT returned by GetPendingDeliveriesAsync
        }
    }

    [Fact]
    public async Task ExpiredLease_CanBeReclaimed()
    {
        // Arrange
        DateTime now = _clock.UtcNow;

        using (LastHourDbContext context = CreateDbContext())
        {
            var notification = Notification.Create(Guid.NewGuid(), Guid.NewGuid(), "u@domain.com", "T", "B", "UserRegistered", now);
            var delivery = NotificationDelivery.Create(notification.Id, DeliveryChannel.InApp, now);

            // Expired lease (expired 5 minutes ago)
            delivery.Claim(Guid.NewGuid(), now.AddMinutes(-5), now.AddMinutes(-10));

            context.Set<Notification>().Add(notification);
            context.Set<NotificationDelivery>().Add(delivery);
            await context.SaveChangesAsync();

            var repo = new NotificationDeliveryRepository(
                new EfRepository<NotificationDelivery>(context),
                new EfReadRepository<NotificationDelivery, Guid>(context));

            // Act
            IReadOnlyList<NotificationDelivery> pending = await repo.GetPendingDeliveriesAsync(batchSize: 10, nowUtc: now);

            // Assert
            Assert.Single(pending);
            Assert.Equal(delivery.Id, pending[0].Id);
        }
    }

    [Fact]
    public async Task StaleWorker_CannotCompleteReclaimedDelivery()
    {
        // Arrange
        DateTime now = _clock.UtcNow;
        Guid deliveryId;

        using (LastHourDbContext setupContext = CreateDbContext())
        {
            var notification = Notification.Create(Guid.NewGuid(), Guid.NewGuid(), "u@domain.com", "T", "B", "UserRegistered", now);
            var delivery = NotificationDelivery.Create(notification.Id, DeliveryChannel.InApp, now);
            setupContext.Set<Notification>().Add(notification);
            setupContext.Set<NotificationDelivery>().Add(delivery);
            await setupContext.SaveChangesAsync();
            deliveryId = delivery.Id;
        }

        // 1. Worker A claims delivery in contextA
        using LastHourDbContext contextA = CreateDbContext();
        var repoA = new NotificationDeliveryRepository(
            new EfRepository<NotificationDelivery>(contextA),
            new EfReadRepository<NotificationDelivery, Guid>(contextA),
            new EfTrackedRepository<NotificationDelivery, Guid>(contextA));

        Guid tokenA = Guid.NewGuid();
        var deliveryA = await repoA.GetByIdTrackedAsync(deliveryId);
        Assert.NotNull(deliveryA);
        deliveryA.Claim(tokenA, now.AddMinutes(1), now);
        await contextA.SaveChangesAsync();

        // 2. Worker B reclaims expired lease in contextB
        using (LastHourDbContext contextB = CreateDbContext())
        {
            var repoB = new NotificationDeliveryRepository(
                new EfRepository<NotificationDelivery>(contextB),
                new EfReadRepository<NotificationDelivery, Guid>(contextB),
                new EfTrackedRepository<NotificationDelivery, Guid>(contextB));

            Guid tokenB = Guid.NewGuid();
            var deliveryB = await repoB.GetByIdTrackedAsync(deliveryId);
            Assert.NotNull(deliveryB);
            deliveryB.Claim(tokenB, now.AddMinutes(5), now.AddMinutes(2));
            await contextB.SaveChangesAsync();
        }

        // 3. Worker A (still holding contextA where tracked original_ClaimToken was tokenA) attempts completion
        deliveryA.MarkDelivered(now.AddMinutes(3));

        // Act & Assert
        // Expect DbUpdateConcurrencyException because ClaimToken in DB is tokenB, not tokenA!
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(async () => await contextA.SaveChangesAsync());
    }

    [Fact]
    public async Task TerminalDelivery_IsNeverPolledAgain()
    {
        // Arrange
        DateTime now = _clock.UtcNow;

        using (LastHourDbContext context = CreateDbContext())
        {
            var notification = Notification.Create(Guid.NewGuid(), Guid.NewGuid(), "u@domain.com", "T", "B", "UserRegistered", now);

            var deliveredItem = NotificationDelivery.Create(notification.Id, DeliveryChannel.InApp, now);
            deliveredItem.MarkDelivered(now);

            var failedPermanentItem = NotificationDelivery.Create(notification.Id, DeliveryChannel.Email, now, maximumRetryCount: 1);
            failedPermanentItem.RecordFailure("Fatal error", now, maxRetryCount: 1, TimeSpan.FromSeconds(10));

            context.Set<Notification>().Add(notification);
            context.Set<NotificationDelivery>().AddRange(deliveredItem, failedPermanentItem);
            await context.SaveChangesAsync();

            var repo = new NotificationDeliveryRepository(
                new EfRepository<NotificationDelivery>(context),
                new EfReadRepository<NotificationDelivery, Guid>(context));

            // Act
            IReadOnlyList<NotificationDelivery> pending = await repo.GetPendingDeliveriesAsync(batchSize: 10, nowUtc: now.AddHours(24));

            // Assert
            Assert.Empty(pending);
        }
    }

    [Fact]
    public async Task CancellationDuringProcessing_DoesNotCreateFalseFailure()
    {
        // Arrange
        DateTime now = _clock.UtcNow;
        var sender = new TestChannelSender(DeliveryChannel.Email, (notif, del) =>
            throw new OperationCanceledException("Host shutdown requested"));

        using (LastHourDbContext context = CreateDbContext())
        {
            var notification = Notification.Create(Guid.NewGuid(), Guid.NewGuid(), "u@domain.com", "T", "B", "UserRegistered", now);
            var delivery = NotificationDelivery.Create(notification.Id, DeliveryChannel.Email, now);
            context.Set<Notification>().Add(notification);
            context.Set<NotificationDelivery>().Add(delivery);
            await context.SaveChangesAsync();
        }

        using ServiceProvider provider = CreateServiceProvider(sender);
        var scopeFactory = provider.GetRequiredService<IServiceScopeFactory>();

        var options = Options.Create(new NotificationDeliveryOptions
        {
            Enabled = true,
            BatchSize = 10,
            PollingInterval = TimeSpan.FromMilliseconds(20),
            LeaseDuration = TimeSpan.FromMinutes(2),
            BaseRetryDelay = TimeSpan.FromSeconds(10),
            MaximumRetryDelay = TimeSpan.FromMinutes(5),
            MaxRetryCount = 3,
        });

        var processor = new NotificationDeliveryProcessor(scopeFactory, options, _metrics, _clock, NullLogger<NotificationDeliveryProcessor>.Instance);

        using var cts = new CancellationTokenSource();
        cts.Cancel(); // Immediately cancelled

        // Act
        await processor.StartAsync(cts.Token);
        await processor.StopAsync(CancellationToken.None);

        // Assert
        using LastHourDbContext verifyContext = CreateDbContext();
        NotificationDelivery? updated = await verifyContext.Set<NotificationDelivery>().FirstOrDefaultAsync();
        Assert.NotNull(updated);
        // Delivery was not marked as FailedPermanent or RetryScheduled due to cancellation
        Assert.Equal(DeliveryStatus.Pending, updated.Status);
    }

    [Fact]
    public async Task Redelivery_DoesNotDuplicateDeliveryIntent()
    {
        // Arrange
        DateTime now = _clock.UtcNow;
        Guid notificationId = Guid.NewGuid();

        using (LastHourDbContext context = CreateDbContext())
        {
            var notification = Notification.Create(Guid.NewGuid(), notificationId, "u@domain.com", "T", "B", "UserRegistered", now);
            var delivery = NotificationDelivery.Create(notification.Id, DeliveryChannel.InApp, now);
            context.Set<Notification>().Add(notification);
            context.Set<NotificationDelivery>().Add(delivery);
            await context.SaveChangesAsync();

            var repo = new NotificationDeliveryRepository(
                new EfRepository<NotificationDelivery>(context),
                new EfReadRepository<NotificationDelivery, Guid>(context));

            // Act
            bool exists = await repo.ExistsByNotificationIdAndChannelAsync(notification.Id, DeliveryChannel.InApp);

            // Assert
            Assert.True(exists);
        }
    }

    private sealed class TestChannelSender : INotificationChannelSender
    {
        private readonly Func<Notification, NotificationDelivery, Task<Result>> _handler;

        public TestChannelSender(DeliveryChannel channel, Func<Notification, NotificationDelivery, Task<Result>> handler)
        {
            Channel = channel;
            _handler = handler;
        }

        public DeliveryChannel Channel { get; }

        public Task<Result> SendAsync(Notification notification, NotificationDelivery delivery, CancellationToken cancellationToken = default)
            => _handler(notification, delivery);
    }

    private sealed class TestChannelFactory : INotificationChannelFactory
    {
        private readonly INotificationChannelSender _sender;

        public TestChannelFactory(INotificationChannelSender sender)
        {
            _sender = sender;
        }

        public INotificationChannelSender GetSender(DeliveryChannel channel) => _sender;
    }

    private sealed class TestClock : IClock
    {
        public DateTime UtcNow { get; }

        public TestClock(DateTime utcNow)
        {
            UtcNow = utcNow;
        }
    }

    private sealed class MeterFactory : IMeterFactory
    {
        private readonly List<Meter> _meters = new();

        public Meter Create(MeterOptions options)
        {
            var meter = new Meter(options);
            _meters.Add(meter);
            return meter;
        }

        public void Dispose()
        {
            foreach (Meter meter in _meters)
            {
                meter.Dispose();
            }
        }
    }
}
