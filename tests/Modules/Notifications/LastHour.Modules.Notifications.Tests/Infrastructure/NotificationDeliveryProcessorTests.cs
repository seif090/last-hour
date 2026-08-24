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

public sealed class NotificationDeliveryProcessorTests : IDisposable
{
    private readonly DbConnection _connection;
    private readonly DbContextOptions<LastHourDbContext> _dbContextOptions;
    private readonly TestClock _clock;
    private readonly MeterFactory _meterFactory;
    private readonly NotificationDeliveryMetrics _metrics;

    public NotificationDeliveryProcessorTests()
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
        services.AddSingleton<INotificationChannelFactory>(new FakeNotificationChannelFactory(channelSender));

        return services.BuildServiceProvider();
    }

    [Fact]
    public async Task ProcessPendingDeliveries_WhenSenderSucceeds_ShouldMarkDeliveryDelivered()
    {
        // Arrange
        Guid notifId = Guid.NewGuid();
        DateTime now = _clock.UtcNow;

        using (LastHourDbContext context = CreateDbContext())
        {
            var notification = Notification.Create(Guid.NewGuid(), notifId, "recipient@lasthour.local", "Subject", "Body", "UserRegistered", now);
            var delivery = NotificationDelivery.Create(notification.Id, DeliveryChannel.InApp, now);
            context.Set<Notification>().Add(notification);
            context.Set<NotificationDelivery>().Add(delivery);
            await context.SaveChangesAsync();
        }

        var sender = new FakeChannelSender(DeliveryChannel.InApp, Result.Success());
        using ServiceProvider provider = CreateServiceProvider(sender);
        var scopeFactory = provider.GetRequiredService<IServiceScopeFactory>();

        var options = Options.Create(new NotificationDeliveryOptions
        {
            Enabled = true,
            BatchSize = 10,
            PollingInterval = TimeSpan.FromMilliseconds(50),
            LeaseDuration = TimeSpan.FromMinutes(2),
            BaseRetryDelay = TimeSpan.FromSeconds(10),
            MaximumRetryDelay = TimeSpan.FromMinutes(5),
            MaxRetryCount = 3,
        });

        var processor = new NotificationDeliveryProcessor(scopeFactory, options, _metrics, _clock, NullLogger<NotificationDeliveryProcessor>.Instance);

        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(200));

        // Act
        await processor.StartAsync(cts.Token);
        await Task.Delay(100);
        await processor.StopAsync(CancellationToken.None);

        // Assert
        using LastHourDbContext verifyContext = CreateDbContext();
        NotificationDelivery? updated = await verifyContext.Set<NotificationDelivery>().FirstOrDefaultAsync();
        Assert.NotNull(updated);
        Assert.Equal(DeliveryStatus.Delivered, updated.Status);
        Assert.NotNull(updated.DeliveredAtUtc);
    }

    [Fact]
    public async Task ProcessPendingDeliveries_WhenSenderFails_ShouldScheduleRetry()
    {
        // Arrange
        Guid notifId = Guid.NewGuid();
        DateTime now = _clock.UtcNow;

        using (LastHourDbContext context = CreateDbContext())
        {
            var notification = Notification.Create(Guid.NewGuid(), notifId, "recipient@lasthour.local", "Subject", "Body", "UserRegistered", now);
            var delivery = NotificationDelivery.Create(notification.Id, DeliveryChannel.Email, now, maximumRetryCount: 3);
            context.Set<Notification>().Add(notification);
            context.Set<NotificationDelivery>().Add(delivery);
            await context.SaveChangesAsync();
        }

        var sender = new FakeChannelSender(DeliveryChannel.Email, Result.Failure(Error.Failure("Email.TransportError", "Failed to connect to SMTP server")));
        using ServiceProvider provider = CreateServiceProvider(sender);
        var scopeFactory = provider.GetRequiredService<IServiceScopeFactory>();

        var options = Options.Create(new NotificationDeliveryOptions
        {
            Enabled = true,
            BatchSize = 10,
            PollingInterval = TimeSpan.FromMilliseconds(50),
            LeaseDuration = TimeSpan.FromMinutes(2),
            BaseRetryDelay = TimeSpan.FromSeconds(10),
            MaximumRetryDelay = TimeSpan.FromMinutes(5),
            MaxRetryCount = 3,
        });

        var processor = new NotificationDeliveryProcessor(scopeFactory, options, _metrics, _clock, NullLogger<NotificationDeliveryProcessor>.Instance);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        // Act
        await processor.StartAsync(cts.Token);
        await Task.Delay(300);
        await processor.StopAsync(CancellationToken.None);

        // Assert
        using LastHourDbContext verifyContext = CreateDbContext();
        NotificationDelivery? updated = await verifyContext.Set<NotificationDelivery>().FirstOrDefaultAsync();
        Assert.NotNull(updated);
        Assert.Equal(DeliveryStatus.RetryScheduled, updated.Status);
        Assert.Equal(1, updated.RetryCount);
        Assert.Contains("Failed to connect to SMTP server", updated.LastError);
    }

    private sealed class FakeChannelSender : INotificationChannelSender
    {
        private readonly Result _result;

        public FakeChannelSender(DeliveryChannel channel, Result result)
        {
            Channel = channel;
            _result = result;
        }

        public DeliveryChannel Channel { get; }

        public Task<Result> SendAsync(Notification notification, NotificationDelivery delivery, CancellationToken cancellationToken = default)
            => Task.FromResult(_result);
    }

    private sealed class FakeNotificationChannelFactory : INotificationChannelFactory
    {
        private readonly INotificationChannelSender _sender;

        public FakeNotificationChannelFactory(INotificationChannelSender sender)
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
