using System.Data.Common;
using System.Diagnostics.Metrics;
using FluentAssertions;
using LastHour.BuildingBlocks.Application.Contracts;
using LastHour.BuildingBlocks.Application.Events;
using LastHour.BuildingBlocks.Contracts.Events;
using LastHour.BuildingBlocks.Infrastructure.Events;
using LastHour.BuildingBlocks.Infrastructure.Persistence;
using LastHour.BuildingBlocks.Infrastructure.Persistence.Configuration;
using LastHour.BuildingBlocks.Infrastructure.Persistence.Idempotency;
using LastHour.BuildingBlocks.Infrastructure.Persistence.Outbox;
using LastHour.BuildingBlocks.Infrastructure.Persistence.Repositories;
using LastHour.BuildingBlocks.Infrastructure.Persistence.UnitOfWork;
using LastHour.BuildingBlocks.SharedKernel.Domain.Events;
using LastHour.Modules.Merchant.Domain.Merchants;
using LastHour.Modules.Notifications.Application.Consumers;
using LastHour.Modules.Notifications.Application.Persistence;
using LastHour.Modules.Notifications.Domain.Notifications;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using MerchantAggregate = LastHour.Modules.Merchant.Domain.Merchants.Merchant;

namespace LastHour.Modules.Notifications.Tests.Application.Consumers;

public class MerchantProfileUpdatedNotificationHandlerTests : IDisposable
{
    private readonly DbConnection _connection;
    private readonly DbContextOptions<LastHourDbContext> _dbContextOptions;
    private readonly TestClock _clock;
    private readonly ConsumerIdempotencyMetrics _metrics;

    public MerchantProfileUpdatedNotificationHandlerTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        _dbContextOptions = new DbContextOptionsBuilder<LastHourDbContext>()
            .UseSqlite(_connection)
            .Options;

        _clock = new TestClock(new DateTime(2026, 8, 20, 12, 0, 0, DateTimeKind.Utc));
        _metrics = new ConsumerIdempotencyMetrics();

        using LastHourDbContext context = CreateDbContext();
        context.Database.EnsureCreated();
    }

    public void Dispose()
    {
        _connection.Dispose();
        _metrics.Dispose();
        GC.SuppressFinalize(this);
    }

    private LastHourDbContext CreateDbContext()
        => new(
            _dbContextOptions,
            new[]
            {
                typeof(Notification).Assembly,
                typeof(MerchantAggregate).Assembly,
            });

    private (MerchantProfileUpdatedNotificationHandler Handler, IConsumerIdempotencyService IdempotencyService, INotificationRepository NotificationRepo, LastHourDbContext DbContext) CreateSut()
    {
        LastHourDbContext dbContext = CreateDbContext();
        var idempotencyService = new ConsumerIdempotencyService(
            dbContext,
            _clock,
            _metrics,
            Microsoft.Extensions.Options.Options.Create(new ConsumerIdempotencyOptions { LeaseDuration = TimeSpan.FromMinutes(2) }),
            NullLogger<ConsumerIdempotencyService>.Instance);

        var repo = new EfRepository<Notification>(dbContext);
        var readRepo = new EfReadRepository<Notification, Guid>(dbContext);
        var notificationRepo = new NotificationRepository(repo, readRepo);
        var unitOfWork = new EfUnitOfWork(dbContext);

        var handler = new MerchantProfileUpdatedNotificationHandler(
            idempotencyService,
            notificationRepo,
            unitOfWork,
            _clock,
            NullLogger<MerchantProfileUpdatedNotificationHandler>.Instance);

        return (handler, idempotencyService, notificationRepo, dbContext);
    }

    [Fact]
    public async Task MerchantProfileUpdated_FirstDelivery_CreatesNotification()
    {
        var (handler, _, _, dbContext) = CreateSut();
        Guid merchantId = Guid.NewGuid();
        var domainEvent = new MerchantProfileUpdatedDomainEvent(merchantId, "New Store Name");
        var message = new NotificationMessage<MerchantProfileUpdatedDomainEvent>(domainEvent);

        await handler.Handle(message, CancellationToken.None);

        List<Notification> notifications = await dbContext.Set<Notification>().ToListAsync();
        notifications.Should().ContainSingle();
        Notification notification = notifications.Single();
        notification.EventId.Should().Be(domainEvent.EventId);
        notification.RecipientId.Should().Be(merchantId);
        notification.Recipient.Should().Be(merchantId.ToString("D"));
        notification.Type.Should().Be("MerchantProfileUpdated");
        notification.Title.Should().Be("Merchant Profile Updated");
        notification.Body.Should().Be("Your merchant profile has been updated successfully.");

        ConsumerInboxMessage? inboxMessage = await dbContext.ConsumerInboxMessages
            .FirstOrDefaultAsync(m => m.EventId == domainEvent.EventId && m.Consumer == MerchantProfileUpdatedNotificationHandler.ConsumerName);
        inboxMessage.Should().NotBeNull();
        inboxMessage!.IsCompleted.Should().BeTrue();
    }

    [Fact]
    public async Task MerchantProfileUpdated_DuplicateDelivery_DoesNotCreateSecondNotification()
    {
        var (handler, _, _, dbContext) = CreateSut();
        Guid merchantId = Guid.NewGuid();
        var domainEvent = new MerchantProfileUpdatedDomainEvent(merchantId, "New Store Name");
        var message = new NotificationMessage<MerchantProfileUpdatedDomainEvent>(domainEvent);

        // First delivery
        await handler.Handle(message, CancellationToken.None);

        // Second delivery (duplicate delivery under at-least-once)
        await handler.Handle(message, CancellationToken.None);

        List<Notification> notifications = await dbContext.Set<Notification>().ToListAsync();
        notifications.Should().ContainSingle();
    }

    [Fact]
    public async Task MerchantProfileUpdated_SameEventAndType_DatabaseConstraintPreventsDuplicate()
    {
        using LastHourDbContext dbContext = CreateDbContext();
        Guid eventId = Guid.NewGuid();
        Guid merchantId = Guid.NewGuid();

        var notification1 = Notification.Create(
            eventId,
            merchantId,
            merchantId.ToString("D"),
            "Title 1",
            "Message 1",
            "MerchantProfileUpdated",
            _clock.UtcNow);

        var notification2 = Notification.Create(
            eventId,
            merchantId,
            merchantId.ToString("D"),
            "Title 2",
            "Message 2",
            "MerchantProfileUpdated",
            _clock.UtcNow);

        dbContext.Set<Notification>().Add(notification1);
        await dbContext.SaveChangesAsync();

        dbContext.Set<Notification>().Add(notification2);
        Func<Task> act = () => dbContext.SaveChangesAsync();

        await act.Should().ThrowAsync<DbUpdateException>();
    }

    [Fact]
    public async Task MerchantProfileUpdated_SameEventDifferentNotificationTypes_AreAllowed()
    {
        using LastHourDbContext dbContext = CreateDbContext();
        Guid eventId = Guid.NewGuid();
        Guid merchantId = Guid.NewGuid();

        var notificationProfile = Notification.Create(
            eventId,
            merchantId,
            merchantId.ToString("D"),
            "Profile Updated",
            "Profile updated message",
            "MerchantProfileUpdated",
            _clock.UtcNow);

        var notificationStatus = Notification.Create(
            eventId,
            merchantId,
            merchantId.ToString("D"),
            "Account Activated",
            "Account activated message",
            "MerchantStatusChanged",
            _clock.UtcNow);

        dbContext.Set<Notification>().Add(notificationProfile);
        dbContext.Set<Notification>().Add(notificationStatus);
        await dbContext.SaveChangesAsync();

        List<Notification> saved = await dbContext.Set<Notification>().Where(n => n.EventId == eventId).ToListAsync();
        saved.Should().HaveCount(2);
    }

    [Fact]
    public async Task MerchantProfileUpdated_DifferentEvents_SameType_AreAllowed()
    {
        using LastHourDbContext dbContext = CreateDbContext();
        var notification1 = Notification.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "merchant:1",
            "Profile Updated 1",
            "Message 1",
            "MerchantProfileUpdated",
            _clock.UtcNow);

        var notification2 = Notification.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "merchant:2",
            "Profile Updated 2",
            "Message 2",
            "MerchantProfileUpdated",
            _clock.UtcNow);

        dbContext.Set<Notification>().Add(notification1);
        dbContext.Set<Notification>().Add(notification2);
        await dbContext.SaveChangesAsync();

        List<Notification> saved = await dbContext.Set<Notification>().ToListAsync();
        saved.Should().HaveCount(2);
    }

    [Fact]
    public async Task MerchantProfileUpdated_CrashAfterBusinessCommitBeforeInboxCompletion_DoesNotDuplicateNotification()
    {
        // 1. Arrange: Worker 1 acquires inbox lease and commits notification, then crashes before completing inbox
        Guid eventId = Guid.NewGuid();
        Guid merchantId = Guid.NewGuid();
        var domainEvent = new MerchantProfileUpdatedDomainEvent(merchantId, "New Store Name")
        {
            EventId = eventId,
        };

        using (LastHourDbContext context1 = CreateDbContext())
        {
            var idempotency1 = new ConsumerIdempotencyService(
                context1,
                _clock,
                _metrics,
                Microsoft.Extensions.Options.Options.Create(new ConsumerIdempotencyOptions { LeaseDuration = TimeSpan.FromMinutes(2) }),
                NullLogger<ConsumerIdempotencyService>.Instance);

            bool acquired = await idempotency1.TryAcquireAsync(eventId, MerchantProfileUpdatedNotificationHandler.ConsumerName);
            acquired.Should().BeTrue();

            var notification = Notification.Create(
                eventId,
                merchantId,
                merchantId.ToString("D"),
                "Merchant Profile Updated",
                "Your merchant profile has been updated successfully.",
                "MerchantProfileUpdated",
                _clock.UtcNow);

            context1.Set<Notification>().Add(notification);
            await context1.SaveChangesAsync();
            // CRASH: Worker 1 terminates here WITHOUT calling CompleteAsync!
        }

        // 2. Act: Advance clock past lease expiration, Worker 2 processes the retry
        _clock.UtcNow = _clock.UtcNow.AddMinutes(3);

        using (LastHourDbContext context2 = CreateDbContext())
        {
            var idempotency2 = new ConsumerIdempotencyService(
                context2,
                _clock,
                _metrics,
                Microsoft.Extensions.Options.Options.Create(new ConsumerIdempotencyOptions { LeaseDuration = TimeSpan.FromMinutes(2) }),
                NullLogger<ConsumerIdempotencyService>.Instance);
            var repo = new EfRepository<Notification>(context2);
            var readRepo = new EfReadRepository<Notification, Guid>(context2);
            var notificationRepo = new NotificationRepository(repo, readRepo);
            var unitOfWork = new EfUnitOfWork(context2);
            var handler = new MerchantProfileUpdatedNotificationHandler(
                idempotency2,
                notificationRepo,
                unitOfWork,
                _clock,
                NullLogger<MerchantProfileUpdatedNotificationHandler>.Instance);

            await handler.Handle(new NotificationMessage<MerchantProfileUpdatedDomainEvent>(domainEvent), CancellationToken.None);
        }

        // 3. Assert
        using (LastHourDbContext verifyContext = CreateDbContext())
        {
            List<Notification> notifications = await verifyContext.Set<Notification>()
                .Where(n => n.EventId == eventId && n.Type == "MerchantProfileUpdated")
                .ToListAsync();
            notifications.Should().ContainSingle();

            ConsumerInboxMessage? inboxMessage = await verifyContext.ConsumerInboxMessages
                .FirstOrDefaultAsync(m => m.EventId == eventId && m.Consumer == MerchantProfileUpdatedNotificationHandler.ConsumerName);
            inboxMessage.Should().NotBeNull();
            inboxMessage!.IsCompleted.Should().BeTrue();
        }
    }

    [Fact]
    public async Task MerchantProfileUpdated_ConcurrentRetries_ProduceSingleBusinessEffect()
    {
        var domainEvent = new MerchantProfileUpdatedDomainEvent(Guid.NewGuid(), "New Store Name");

        Task[] tasks = Enumerable.Range(0, 10).Select(async _ =>
        {
            using LastHourDbContext context = CreateDbContext();
            var idempotency = new ConsumerIdempotencyService(
                context,
                _clock,
                _metrics,
                Microsoft.Extensions.Options.Options.Create(new ConsumerIdempotencyOptions { LeaseDuration = TimeSpan.FromMinutes(2) }),
                NullLogger<ConsumerIdempotencyService>.Instance);
            var repo = new EfRepository<Notification>(context);
            var readRepo = new EfReadRepository<Notification, Guid>(context);
            var notificationRepo = new NotificationRepository(repo, readRepo);
            var unitOfWork = new EfUnitOfWork(context);
            var handler = new MerchantProfileUpdatedNotificationHandler(
                idempotency,
                notificationRepo,
                unitOfWork,
                _clock,
                NullLogger<MerchantProfileUpdatedNotificationHandler>.Instance);

            await handler.Handle(new NotificationMessage<MerchantProfileUpdatedDomainEvent>(domainEvent), CancellationToken.None);
        }).ToArray();

        await Task.WhenAll(tasks);

        using LastHourDbContext verifyContext = CreateDbContext();
        List<Notification> notifications = await verifyContext.Set<Notification>().ToListAsync();
        notifications.Should().ContainSingle();

        ConsumerInboxMessage inbox = await verifyContext.ConsumerInboxMessages.SingleAsync();
        inbox.IsCompleted.Should().BeTrue();
    }

    [Fact]
    public async Task MerchantProfileUpdated_DuplicateUniqueConstraint_IsRecoveredAsSuccess()
    {
        var (handler, _, _, dbContext) = CreateSut();
        Guid eventId = Guid.NewGuid();
        Guid merchantId = Guid.NewGuid();
        var domainEvent = new MerchantProfileUpdatedDomainEvent(merchantId, "New Store Name")
        {
            EventId = eventId,
        };

        var existingNotification = Notification.Create(
            eventId,
            merchantId,
            merchantId.ToString("D"),
            "Merchant Profile Updated",
            "Your merchant profile has been updated successfully.",
            "MerchantProfileUpdated",
            _clock.UtcNow);

        dbContext.Set<Notification>().Add(existingNotification);
        await dbContext.SaveChangesAsync();

        Func<Task> act = () => handler.Handle(new NotificationMessage<MerchantProfileUpdatedDomainEvent>(domainEvent), CancellationToken.None);
        await act.Should().NotThrowAsync();

        List<Notification> notifications = await dbContext.Set<Notification>().ToListAsync();
        notifications.Should().ContainSingle();

        ConsumerInboxMessage inbox = await dbContext.ConsumerInboxMessages.SingleAsync();
        inbox.IsCompleted.Should().BeTrue();
    }

    [Fact]
    public async Task MerchantProfileUpdated_UnrelatedDatabaseFailure_IsNotSwallowed()
    {
        LastHourDbContext dbContext = CreateDbContext();
        var idempotencyService = new ConsumerIdempotencyService(
            dbContext,
            _clock,
            _metrics,
            Microsoft.Extensions.Options.Options.Create(new ConsumerIdempotencyOptions()),
            NullLogger<ConsumerIdempotencyService>.Instance);

        var domainEvent = new MerchantProfileUpdatedDomainEvent(Guid.NewGuid(), "New Store Name");
        var message = new NotificationMessage<MerchantProfileUpdatedDomainEvent>(domainEvent);

        var failingRepo = new FailingNotificationRepository();
        var unitOfWork = new EfUnitOfWork(dbContext);
        var handler = new MerchantProfileUpdatedNotificationHandler(
            idempotencyService,
            failingRepo,
            unitOfWork,
            _clock,
            NullLogger<MerchantProfileUpdatedNotificationHandler>.Instance);

        Func<Task> act = () => handler.Handle(message, CancellationToken.None);
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("Database connection failure.");

        ConsumerInboxMessage? inboxMessage = await dbContext.ConsumerInboxMessages
            .FirstOrDefaultAsync(m => m.EventId == domainEvent.EventId && m.Consumer == MerchantProfileUpdatedNotificationHandler.ConsumerName);
        inboxMessage.Should().BeNull();
    }

    [Fact]
    public async Task MerchantProfileUpdated_Cancellation_DoesNotConvertIntoDuplicateSuccess()
    {
        var (handler, _, _, dbContext) = CreateSut();
        var domainEvent = new MerchantProfileUpdatedDomainEvent(Guid.NewGuid(), "New Store Name");
        var message = new NotificationMessage<MerchantProfileUpdatedDomainEvent>(domainEvent);

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        Func<Task> act = () => handler.Handle(message, cts.Token);
        await act.Should().ThrowAsync<OperationCanceledException>();

        ConsumerInboxMessage? inboxMessage = await dbContext.ConsumerInboxMessages
            .FirstOrDefaultAsync(m => m.EventId == domainEvent.EventId && m.Consumer == MerchantProfileUpdatedNotificationHandler.ConsumerName);
        inboxMessage.Should().BeNull();
    }

    [Fact]
    public async Task MerchantProfileUpdated_ConsumerMetrics_AreCorrect_WhenBusinessEffectAlreadyExists()
    {
        var (handler, _, _, dbContext) = CreateSut();
        var domainEvent = new MerchantProfileUpdatedDomainEvent(Guid.NewGuid(), "New Store Name");
        var message = new NotificationMessage<MerchantProfileUpdatedDomainEvent>(domainEvent);

        int processedCount = 0;
        int failedCount = 0;
        int duplicateCount = 0;

        using var listener = new MeterListener();
        listener.InstrumentPublished = (instrument, l) =>
        {
            if (instrument.Meter.Name == "LastHour.ConsumerIdempotency")
            {
                l.EnableMeasurementEvents(instrument);
            }
        };
        listener.SetMeasurementEventCallback<long>((instrument, measurement, tags, state) =>
        {
            string? consumer = null;
            foreach (KeyValuePair<string, object?> tag in tags)
            {
                if (tag.Key == "consumer")
                {
                    consumer = tag.Value as string;
                }
            }

            if (consumer != MerchantProfileUpdatedNotificationHandler.ConsumerName)
            {
                return;
            }

            if (instrument.Name == "last_hour.consumer.processed")
            {
                Interlocked.Increment(ref processedCount);
            }
            else if (instrument.Name == "last_hour.consumer.failed")
            {
                Interlocked.Increment(ref failedCount);
            }
            else if (instrument.Name == "last_hour.consumer.duplicate")
            {
                Interlocked.Increment(ref duplicateCount);
            }
        });
        listener.Start();

        // Attempt 1: Normal processing
        await handler.Handle(message, CancellationToken.None);

        // Attempt 2: Duplicate delivery (skipped by ConsumerIdempotencyService)
        await handler.Handle(message, CancellationToken.None);

        processedCount.Should().Be(1);
        failedCount.Should().Be(0);
        duplicateCount.Should().Be(1);
    }

    [Fact]
    public void MerchantProfileUpdated_ConsumerIdentityIsStable()
    {
        MerchantProfileUpdatedNotificationHandler.ConsumerName.Should().Be("Notifications.MerchantProfileUpdated");
    }

    [Fact]
    public async Task MerchantProfileUpdated_HandlerFailure_RemainsRetryable()
    {
        LastHourDbContext dbContext = CreateDbContext();
        var idempotencyService = new ConsumerIdempotencyService(
            dbContext,
            _clock,
            _metrics,
            NullLogger<ConsumerIdempotencyService>.Instance);

        var domainEvent = new MerchantProfileUpdatedDomainEvent(Guid.NewGuid(), "New Store Name");
        var message = new NotificationMessage<MerchantProfileUpdatedDomainEvent>(domainEvent);

        var failingRepo = new FailingNotificationRepository();
        var unitOfWork = new EfUnitOfWork(dbContext);
        var handler = new MerchantProfileUpdatedNotificationHandler(
            idempotencyService,
            failingRepo,
            unitOfWork,
            _clock,
            NullLogger<MerchantProfileUpdatedNotificationHandler>.Instance);

        Func<Task> act = () => handler.Handle(message, CancellationToken.None);
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("Database connection failure.");

        ConsumerInboxMessage? inboxMessage = await dbContext.ConsumerInboxMessages
            .FirstOrDefaultAsync(m => m.EventId == domainEvent.EventId && m.Consumer == MerchantProfileUpdatedNotificationHandler.ConsumerName);
        inboxMessage.Should().BeNull();
    }

    [Fact]
    public async Task MerchantProfileUpdated_RetryAfterFailure_Succeeds()
    {
        LastHourDbContext dbContext = CreateDbContext();
        var idempotencyService = new ConsumerIdempotencyService(
            dbContext,
            _clock,
            _metrics,
            NullLogger<ConsumerIdempotencyService>.Instance);

        var domainEvent = new MerchantProfileUpdatedDomainEvent(Guid.NewGuid(), "New Store Name");
        var message = new NotificationMessage<MerchantProfileUpdatedDomainEvent>(domainEvent);

        var flakyRepo = new FlakyNotificationRepository(dbContext);
        var unitOfWork = new EfUnitOfWork(dbContext);
        var handler = new MerchantProfileUpdatedNotificationHandler(
            idempotencyService,
            flakyRepo,
            unitOfWork,
            _clock,
            NullLogger<MerchantProfileUpdatedNotificationHandler>.Instance);

        // Attempt 1: Fails
        Func<Task> act = () => handler.Handle(message, CancellationToken.None);
        await act.Should().ThrowAsync<InvalidOperationException>();

        // Attempt 2: Succeeds on retry
        await handler.Handle(message, CancellationToken.None);

        List<Notification> notifications = await dbContext.Set<Notification>().ToListAsync();
        notifications.Should().ContainSingle();
        ConsumerInboxMessage? inboxMessage = await dbContext.ConsumerInboxMessages
            .FirstOrDefaultAsync(m => m.EventId == domainEvent.EventId && m.Consumer == MerchantProfileUpdatedNotificationHandler.ConsumerName);
        inboxMessage.Should().NotBeNull();
        inboxMessage!.IsCompleted.Should().BeTrue();
    }

    [Fact]
    public async Task MerchantProfileUpdated_EventIdIsPreserved()
    {
        var (handler, _, _, dbContext) = CreateSut();
        Guid expectedEventId = Guid.NewGuid();
        var domainEvent = new MerchantProfileUpdatedDomainEvent(Guid.NewGuid(), "New Store Name")
        {
            EventId = expectedEventId,
        };

        await handler.Handle(new NotificationMessage<MerchantProfileUpdatedDomainEvent>(domainEvent), CancellationToken.None);

        Notification notification = await dbContext.Set<Notification>().SingleAsync();
        notification.EventId.Should().Be(expectedEventId);

        ConsumerInboxMessage inbox = await dbContext.ConsumerInboxMessages.SingleAsync();
        inbox.EventId.Should().Be(expectedEventId);
    }

    [Fact]
    public async Task Merchant_EndToEnd_OutboxToProfileUpdatedNotification_ExecutesSuccessfully()
    {
        // 1. Setup DI container
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IClock>(_clock);
        services.AddSingleton(_metrics);
        services.AddScoped(_ => CreateDbContext());
        services.AddScoped<IUnitOfWork, EfUnitOfWork>();
        services.AddScoped(typeof(IRepository<>), typeof(EfRepository<>));
        services.AddScoped(typeof(IReadRepository<,>), typeof(EfReadRepository<,>));
        services.AddScoped<INotificationRepository, NotificationRepository>();
        services.AddScoped<IConsumerIdempotencyService, ConsumerIdempotencyService>();
        services.AddScoped<IEventDispatcher, EventDispatcher>();
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(MerchantProfileUpdatedNotificationHandler).Assembly);
        });
        services.Configure<OutboxOptions>(opts =>
        {
            opts.BatchSize = 10;
            opts.ProcessingInterval = TimeSpan.FromMilliseconds(50);
            opts.LeaseDuration = TimeSpan.FromMinutes(2);
            opts.MaxRetryCount = 3;
        });

        IServiceProvider serviceProvider = services.BuildServiceProvider();

        // 2. Register and Update Profile on Merchant aggregate
        Guid ownerId = Guid.NewGuid();
        MerchantAggregate merchant = MerchantAggregate.Register(
            ownerId,
            MerchantName.From("Original Store"),
            Address.Create("123 Bakery St", "Seattle", "WA", "98101", "USA"),
            PhoneNumber.From("+12065550100"));

        merchant.ClearDomainEvents();
        merchant.UpdateProfile(
            MerchantName.From("Updated Profile Store"),
            Address.Create("456 Market St", "Seattle", "WA", "98101", "USA"),
            PhoneNumber.From("+12065550200"));

        IReadOnlyCollection<IDomainEvent> domainEvents = merchant.GetDomainEvents();
        domainEvents.Should().ContainSingle();
        MerchantProfileUpdatedDomainEvent domainEvent = domainEvents.OfType<MerchantProfileUpdatedDomainEvent>().Single();

        // 3. Persist to outbox table
        using (IServiceScope scope = serviceProvider.CreateScope())
        {
            LastHourDbContext dbContext = scope.ServiceProvider.GetRequiredService<LastHourDbContext>();
            var outboxMessage = OutboxMessage.FromDomainEvent(domainEvent);
            dbContext.OutboxMessages.Add(outboxMessage);
            await dbContext.SaveChangesAsync();
        }

        // 4. Run OutboxProcessor iteration
        var tracker = new OutboxProcessorTracker();
        var outboxProcessor = new OutboxProcessor(
            serviceProvider.GetRequiredService<IServiceScopeFactory>(),
            serviceProvider.GetRequiredService<IOptions<OutboxOptions>>(),
            new OutboxMetrics(),
            tracker,
            _clock,
            NullLogger<OutboxProcessor>.Instance);

        using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2)))
        {
            Task processorTask = outboxProcessor.StartAsync(cts.Token);
            await Task.Delay(200);
            await outboxProcessor.StopAsync(CancellationToken.None);
        }

        // 5. Verification
        using (IServiceScope verifyScope = serviceProvider.CreateScope())
        {
            LastHourDbContext dbContext = verifyScope.ServiceProvider.GetRequiredService<LastHourDbContext>();

            OutboxMessage processedOutbox = await dbContext.OutboxMessages.SingleAsync();
            processedOutbox.ProcessedOnUtc.Should().NotBeNull();

            ConsumerInboxMessage inbox = await dbContext.ConsumerInboxMessages.SingleAsync();
            inbox.EventId.Should().Be(domainEvent.EventId);
            inbox.Consumer.Should().Be(MerchantProfileUpdatedNotificationHandler.ConsumerName);
            inbox.IsCompleted.Should().BeTrue();

            Notification notification = await dbContext.Set<Notification>().SingleAsync();
            notification.EventId.Should().Be(domainEvent.EventId);
            notification.RecipientId.Should().Be(merchant.Id.Value);
            notification.Type.Should().Be("MerchantProfileUpdated");
            notification.Title.Should().Be("Merchant Profile Updated");
            notification.Body.Should().Be("Your merchant profile has been updated successfully.");
        }
    }

    [Fact]
    public async Task Merchant_EndToEnd_RedeliveryAfterBusinessEffectExists_CompletesInboxWithoutDuplicate()
    {
        // 1. Setup DI container
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IClock>(_clock);
        services.AddSingleton(_metrics);
        services.AddScoped(_ => CreateDbContext());
        services.AddScoped<IUnitOfWork, EfUnitOfWork>();
        services.AddScoped(typeof(IRepository<>), typeof(EfRepository<>));
        services.AddScoped(typeof(IReadRepository<,>), typeof(EfReadRepository<,>));
        services.AddScoped<INotificationRepository, NotificationRepository>();
        services.AddScoped<IConsumerIdempotencyService, ConsumerIdempotencyService>();
        services.AddScoped<IEventDispatcher, EventDispatcher>();
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(MerchantProfileUpdatedNotificationHandler).Assembly);
        });
        services.Configure<OutboxOptions>(opts =>
        {
            opts.BatchSize = 10;
            opts.ProcessingInterval = TimeSpan.FromMilliseconds(50);
            opts.LeaseDuration = TimeSpan.FromMinutes(2);
            opts.MaxRetryCount = 3;
        });

        IServiceProvider serviceProvider = services.BuildServiceProvider();

        // 2. Prepare domain event
        Guid merchantId = Guid.NewGuid();
        var domainEvent = new MerchantProfileUpdatedDomainEvent(merchantId, "Updated Profile Store");

        // 3. Pre-populate existing business effect
        using (IServiceScope scope = serviceProvider.CreateScope())
        {
            LastHourDbContext dbContext = scope.ServiceProvider.GetRequiredService<LastHourDbContext>();

            var existingNotification = Notification.Create(
                domainEvent.EventId,
                domainEvent.MerchantId,
                domainEvent.MerchantId.ToString("D"),
                "Merchant Profile Updated",
                "Your merchant profile has been updated successfully.",
                "MerchantProfileUpdated",
                _clock.UtcNow);

            dbContext.Set<Notification>().Add(existingNotification);

            var outboxMessage = OutboxMessage.FromDomainEvent(domainEvent);
            dbContext.OutboxMessages.Add(outboxMessage);

            await dbContext.SaveChangesAsync();
        }

        // 4. Run OutboxProcessor
        var tracker = new OutboxProcessorTracker();
        var outboxProcessor = new OutboxProcessor(
            serviceProvider.GetRequiredService<IServiceScopeFactory>(),
            serviceProvider.GetRequiredService<IOptions<OutboxOptions>>(),
            new OutboxMetrics(),
            tracker,
            _clock,
            NullLogger<OutboxProcessor>.Instance);

        using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2)))
        {
            Task processorTask = outboxProcessor.StartAsync(cts.Token);
            await Task.Delay(200);
            await outboxProcessor.StopAsync(CancellationToken.None);
        }

        // 5. Verification
        using (IServiceScope verifyScope = serviceProvider.CreateScope())
        {
            LastHourDbContext dbContext = verifyScope.ServiceProvider.GetRequiredService<LastHourDbContext>();

            OutboxMessage processedOutbox = await dbContext.OutboxMessages.SingleAsync();
            processedOutbox.ProcessedOnUtc.Should().NotBeNull();

            ConsumerInboxMessage inbox = await dbContext.ConsumerInboxMessages.SingleAsync();
            inbox.EventId.Should().Be(domainEvent.EventId);
            inbox.IsCompleted.Should().BeTrue();

            List<Notification> notifications = await dbContext.Set<Notification>()
                .Where(n => n.EventId == domainEvent.EventId)
                .ToListAsync();
            notifications.Should().ContainSingle();
        }
    }

    private sealed class FailingNotificationRepository : INotificationRepository
    {
        public Task AddAsync(Notification notification, CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("Database connection failure.");

        public Task<Notification?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
            => Task.FromResult<Notification?>(null);

        public Task<Notification?> GetByEventIdAsync(Guid eventId, CancellationToken cancellationToken = default)
            => Task.FromResult<Notification?>(null);

        public Task<Notification?> GetByEventIdAndTypeAsync(Guid eventId, string type, CancellationToken cancellationToken = default)
            => Task.FromResult<Notification?>(null);

        public Task<bool> ExistsByEventIdAndTypeAsync(Guid eventId, string type, CancellationToken cancellationToken = default)
            => Task.FromResult(false);

        public Task<IReadOnlyList<Notification>> GetByRecipientIdAsync(Guid recipientId, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<Notification>>(Array.Empty<Notification>());
    }

    private sealed class FlakyNotificationRepository : INotificationRepository
    {
        private readonly NotificationRepository _inner;
        private int _attempts;

        public FlakyNotificationRepository(LastHourDbContext dbContext)
        {
            _inner = new NotificationRepository(
                new EfRepository<Notification>(dbContext),
                new EfReadRepository<Notification, Guid>(dbContext));
        }

        public Task AddAsync(Notification notification, CancellationToken cancellationToken = default)
        {
            if (Interlocked.Increment(ref _attempts) == 1)
            {
                throw new InvalidOperationException("Transient failure on attempt 1.");
            }

            return _inner.AddAsync(notification, cancellationToken);
        }

        public Task<Notification?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
            => _inner.GetByIdAsync(id, cancellationToken);

        public Task<Notification?> GetByEventIdAsync(Guid eventId, CancellationToken cancellationToken = default)
            => _inner.GetByEventIdAsync(eventId, cancellationToken);

        public Task<Notification?> GetByEventIdAndTypeAsync(Guid eventId, string type, CancellationToken cancellationToken = default)
            => _inner.GetByEventIdAndTypeAsync(eventId, type, cancellationToken);

        public Task<bool> ExistsByEventIdAndTypeAsync(Guid eventId, string type, CancellationToken cancellationToken = default)
            => _inner.ExistsByEventIdAndTypeAsync(eventId, type, cancellationToken);

        public Task<IReadOnlyList<Notification>> GetByRecipientIdAsync(Guid recipientId, CancellationToken cancellationToken = default)
            => _inner.GetByRecipientIdAsync(recipientId, cancellationToken);
    }

    private sealed class TestClock : IClock
    {
        public TestClock(DateTime utcNow)
        {
            UtcNow = utcNow;
        }

        public DateTime UtcNow { get; set; }
    }
}
