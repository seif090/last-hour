using System.Data.Common;
using FluentAssertions;
using LastHour.BuildingBlocks.Application.Contracts;
using LastHour.BuildingBlocks.Contracts.Events;
using LastHour.BuildingBlocks.Infrastructure.Events;
using LastHour.BuildingBlocks.Infrastructure.Persistence;
using LastHour.BuildingBlocks.Infrastructure.Persistence.Configuration;
using LastHour.BuildingBlocks.Infrastructure.Persistence.Idempotency;
using LastHour.BuildingBlocks.Infrastructure.Persistence.Outbox;
using LastHour.BuildingBlocks.Infrastructure.Persistence.Repositories;
using LastHour.BuildingBlocks.Infrastructure.Persistence.UnitOfWork;
using LastHour.Modules.Notifications.Application.Consumers;
using LastHour.Modules.Notifications.Application.Persistence;
using LastHour.Modules.Notifications.Domain.Notifications;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace LastHour.Modules.Notifications.Tests.Application.Consumers;

public class EventProcessingEndToEndHardeningTests : IDisposable
{
    private readonly DbConnection _connection;
    private readonly DbContextOptions<LastHourDbContext> _dbContextOptions;
    private readonly E2EHardeningClock _clock;
    private readonly OutboxMetrics _outboxMetrics;
    private readonly ConsumerIdempotencyMetrics _consumerMetrics;

    public EventProcessingEndToEndHardeningTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        _dbContextOptions = new DbContextOptionsBuilder<LastHourDbContext>()
            .UseSqlite(_connection)
            .Options;

        _clock = new E2EHardeningClock(new DateTime(2026, 8, 20, 10, 0, 0, DateTimeKind.Utc));
        _outboxMetrics = new OutboxMetrics();
        _consumerMetrics = new ConsumerIdempotencyMetrics();

        using LastHourDbContext context = CreateDbContext();
        context.Database.EnsureCreated();
    }

    public void Dispose()
    {
        _connection.Dispose();
        _outboxMetrics.Dispose();
        _consumerMetrics.Dispose();
        GC.SuppressFinalize(this);
    }

    private LastHourDbContext CreateDbContext()
        => new(
            _dbContextOptions,
            new[]
            {
                typeof(Notification).Assembly,
            });

    private ServiceProvider BuildServiceProvider()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IClock>(_clock);
        services.AddSingleton(_outboxMetrics);
        services.AddSingleton(_consumerMetrics);
        services.AddSingleton<IOutboxProcessorTracker, OutboxProcessorTracker>();
        services.AddScoped(_ => CreateDbContext());
        services.AddScoped<IUnitOfWork, EfUnitOfWork>();
        services.AddScoped(typeof(IRepository<>), typeof(EfRepository<>));
        services.AddScoped(typeof(IReadRepository<,>), typeof(EfReadRepository<,>));
        services.AddScoped<INotificationRepository, NotificationRepository>();
        services.AddScoped<IConsumerIdempotencyService, ConsumerIdempotencyService>();
        services.AddScoped<IEventDispatcher, EventDispatcher>();
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(UserRegisteredNotificationHandler).Assembly);
        });

        services.Configure<OutboxOptions>(opts =>
        {
            opts.Enabled = true;
            opts.BatchSize = 10;
            opts.ProcessingInterval = TimeSpan.FromMilliseconds(50);
            opts.LeaseDuration = TimeSpan.FromMinutes(2);
            opts.BaseRetryDelay = TimeSpan.FromSeconds(2);
            opts.MaximumRetryDelay = TimeSpan.FromMinutes(5);
            opts.MaxRetryCount = 3;
        });

        services.Configure<ConsumerIdempotencyOptions>(opts =>
        {
            opts.LeaseDuration = TimeSpan.FromMinutes(2);
        });

        return services.BuildServiceProvider();
    }

    [Fact]
    public async Task EndToEnd_AllThreeIdentityEvents_ExecuteAcrossFullPipeline_Successfully()
    {
        using ServiceProvider provider = BuildServiceProvider();

        Guid userId1 = Guid.NewGuid();
        Guid userId2 = Guid.NewGuid();
        Guid userId3 = Guid.NewGuid();

        var regEvent = new UserRegisteredDomainEvent(userId1, "identity1@example.com");
        var pwdEvent = new UserPasswordChangedDomainEvent(userId2);
        var lockEvent = new UserLockedOutDomainEvent(userId3, _clock.UtcNow.AddMinutes(30));

        // 1. Stage domain events into outbox
        using (IServiceScope scope = provider.CreateScope())
        {
            LastHourDbContext db = scope.ServiceProvider.GetRequiredService<LastHourDbContext>();
            db.OutboxMessages.AddRange(
                OutboxMessage.FromDomainEvent(regEvent),
                OutboxMessage.FromDomainEvent(pwdEvent),
                OutboxMessage.FromDomainEvent(lockEvent));
            await db.SaveChangesAsync();
        }

        // 2. Run processor cycle
        var tracker = provider.GetRequiredService<IOutboxProcessorTracker>();
        var processor = new OutboxProcessor(
            provider.GetRequiredService<IServiceScopeFactory>(),
            provider.GetRequiredService<IOptions<OutboxOptions>>(),
            _outboxMetrics,
            tracker,
            _clock,
            NullLogger<OutboxProcessor>.Instance);

        using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2)))
        {
            await processor.StartAsync(cts.Token);
            await Task.Delay(250);
            await processor.StopAsync(CancellationToken.None);
        }

        // 3. Verify all 3 events produced their specific business effect notifications and completed inbox
        using (IServiceScope verifyScope = provider.CreateScope())
        {
            LastHourDbContext db = verifyScope.ServiceProvider.GetRequiredService<LastHourDbContext>();

            List<OutboxMessage> outboxList = await db.OutboxMessages.ToListAsync();
            outboxList.Should().HaveCount(3);
            outboxList.Should().OnlyContain(m => m.ProcessedOnUtc != null);

            List<ConsumerInboxMessage> inboxList = await db.ConsumerInboxMessages.ToListAsync();
            inboxList.Should().HaveCount(3);
            inboxList.Should().OnlyContain(m => m.IsCompleted);

            List<Notification> notifications = await db.Set<Notification>().ToListAsync();
            notifications.Should().HaveCount(3);

            Notification regNotif = notifications.Single(n => n.EventId == regEvent.EventId);
            regNotif.Type.Should().Be("UserRegistered");
            regNotif.Title.Should().Be("Welcome to LastHour");
            regNotif.RecipientId.Should().Be(userId1);

            Notification pwdNotif = notifications.Single(n => n.EventId == pwdEvent.EventId);
            pwdNotif.Type.Should().Be("UserPasswordChanged");
            pwdNotif.Title.Should().Be("Password Changed");
            pwdNotif.RecipientId.Should().Be(userId2);

            Notification lockNotif = notifications.Single(n => n.EventId == lockEvent.EventId);
            lockNotif.Type.Should().Be("UserLockedOut");
            lockNotif.Title.Should().Be("Account Temporarily Locked");
            lockNotif.RecipientId.Should().Be(userId3);
        }
    }

    [Fact]
    public async Task EndToEnd_RedeliveryOfAllIdentityEvents_IsCompletelyIdempotent()
    {
        using ServiceProvider provider = BuildServiceProvider();

        Guid userId1 = Guid.NewGuid();
        Guid userId2 = Guid.NewGuid();
        Guid userId3 = Guid.NewGuid();

        var regEvent = new UserRegisteredDomainEvent(userId1, "identity1@example.com");
        var pwdEvent = new UserPasswordChangedDomainEvent(userId2);
        var lockEvent = new UserLockedOutDomainEvent(userId3, _clock.UtcNow.AddMinutes(30));

        // 1. Stage domain events and initial run
        using (IServiceScope scope = provider.CreateScope())
        {
            LastHourDbContext db = scope.ServiceProvider.GetRequiredService<LastHourDbContext>();
            db.OutboxMessages.AddRange(
                OutboxMessage.FromDomainEvent(regEvent),
                OutboxMessage.FromDomainEvent(pwdEvent),
                OutboxMessage.FromDomainEvent(lockEvent));
            await db.SaveChangesAsync();
        }

        var tracker = provider.GetRequiredService<IOutboxProcessorTracker>();
        var processor = new OutboxProcessor(
            provider.GetRequiredService<IServiceScopeFactory>(),
            provider.GetRequiredService<IOptions<OutboxOptions>>(),
            _outboxMetrics,
            tracker,
            _clock,
            NullLogger<OutboxProcessor>.Instance);

        using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2)))
        {
            await processor.StartAsync(cts.Token);
            await Task.Delay(200);
            await processor.StopAsync(CancellationToken.None);
        }

        // 2. Stage duplicate outbox messages with the same EventIds (simulating duplicated outbox writes/retries)
        using (IServiceScope scope = provider.CreateScope())
        {
            LastHourDbContext db = scope.ServiceProvider.GetRequiredService<LastHourDbContext>();
            db.OutboxMessages.AddRange(
                OutboxMessage.FromDomainEvent(regEvent),
                OutboxMessage.FromDomainEvent(pwdEvent),
                OutboxMessage.FromDomainEvent(lockEvent));
            await db.SaveChangesAsync();
        }

        // 3. Run processor cycle again
        using (var cts2 = new CancellationTokenSource(TimeSpan.FromSeconds(2)))
        {
            await processor.StartAsync(cts2.Token);
            await Task.Delay(200);
            await processor.StopAsync(CancellationToken.None);
        }

        // 4. Assert: Still exactly 3 notifications exist
        using (IServiceScope verifyScope = provider.CreateScope())
        {
            LastHourDbContext db = verifyScope.ServiceProvider.GetRequiredService<LastHourDbContext>();

            List<Notification> notifications = await db.Set<Notification>().ToListAsync();
            notifications.Should().HaveCount(3, "Duplicate deliveries must be completely suppressed by consumer idempotency");
        }
    }

    private sealed class E2EHardeningClock : IClock
    {
        public E2EHardeningClock(DateTime utcNow)
        {
            UtcNow = utcNow;
        }

        public DateTime UtcNow { get; set; }
    }
}
