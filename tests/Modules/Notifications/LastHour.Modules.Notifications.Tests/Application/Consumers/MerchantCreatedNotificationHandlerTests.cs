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

public class MerchantCreatedNotificationHandlerTests : IDisposable
{
    private readonly DbConnection _connection;
    private readonly DbContextOptions<LastHourDbContext> _dbContextOptions;
    private readonly TestClock _clock;
    private readonly ConsumerIdempotencyMetrics _metrics;

    public MerchantCreatedNotificationHandlerTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        _dbContextOptions = new DbContextOptionsBuilder<LastHourDbContext>()
            .UseSqlite(_connection)
            .Options;

        _clock = new TestClock(new DateTime(2026, 8, 19, 12, 0, 0, DateTimeKind.Utc));
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

    private (MerchantCreatedNotificationHandler Handler, IConsumerIdempotencyService IdempotencyService, INotificationRepository NotificationRepo, LastHourDbContext DbContext) CreateSut()
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

        var handler = new MerchantCreatedNotificationHandler(
            idempotencyService,
            notificationRepo,
            unitOfWork,
            _clock,
            NullLogger<MerchantCreatedNotificationHandler>.Instance);

        return (handler, idempotencyService, notificationRepo, dbContext);
    }

    [Fact]
    public async Task MerchantCreated_FirstDelivery_CreatesExactlyOneNotification()
    {
        var (handler, _, _, dbContext) = CreateSut();
        Guid merchantId = Guid.NewGuid();
        var domainEvent = new MerchantCreatedDomainEvent(merchantId, "Bakery Express", "123 Main St, City, Country", "+1234567890");
        var message = new NotificationMessage<MerchantCreatedDomainEvent>(domainEvent);

        await handler.Handle(message, CancellationToken.None);

        List<Notification> notifications = await dbContext.Set<Notification>().ToListAsync();
        notifications.Should().ContainSingle();
        Notification notification = notifications.Single();
        notification.EventId.Should().Be(domainEvent.EventId);
        notification.RecipientId.Should().Be(merchantId);
        notification.Recipient.Should().Be("+1234567890");
        notification.Type.Should().Be("MerchantCreated");
        notification.Title.Should().Be("Welcome to LastHour");
        notification.Body.Should().Contain("Bakery Express");

        ConsumerInboxMessage? inboxMessage = await dbContext.ConsumerInboxMessages
            .FirstOrDefaultAsync(m => m.EventId == domainEvent.EventId && m.Consumer == MerchantCreatedNotificationHandler.ConsumerName);
        inboxMessage.Should().NotBeNull();
        inboxMessage!.IsCompleted.Should().BeTrue();
    }

    [Fact]
    public async Task MerchantCreated_DuplicateDelivery_DoesNotCreateSecondNotification()
    {
        var (handler, _, _, dbContext) = CreateSut();
        Guid merchantId = Guid.NewGuid();
        var domainEvent = new MerchantCreatedDomainEvent(merchantId, "Acme Store", "456 Market St, City, Country", "+1987654321");
        var message = new NotificationMessage<MerchantCreatedDomainEvent>(domainEvent);

        // First delivery
        await handler.Handle(message, CancellationToken.None);

        // Second delivery (duplicate delivery under at-least-once)
        await handler.Handle(message, CancellationToken.None);

        List<Notification> notifications = await dbContext.Set<Notification>().ToListAsync();
        notifications.Should().ContainSingle();
    }

    [Fact]
    public async Task MerchantCreated_SameEventAndType_DatabaseConstraintPreventsDuplicate()
    {
        using LastHourDbContext dbContext = CreateDbContext();
        Guid eventId = Guid.NewGuid();
        Guid merchantId = Guid.NewGuid();

        var notification1 = Notification.Create(
            eventId,
            merchantId,
            "+123456789",
            "Welcome 1",
            "Message 1",
            "MerchantCreated",
            _clock.UtcNow);

        var notification2 = Notification.Create(
            eventId,
            merchantId,
            "+123456789",
            "Welcome 2",
            "Message 2",
            "MerchantCreated",
            _clock.UtcNow);

        dbContext.Set<Notification>().Add(notification1);
        await dbContext.SaveChangesAsync();

        dbContext.Set<Notification>().Add(notification2);
        Func<Task> act = () => dbContext.SaveChangesAsync();

        await act.Should().ThrowAsync<DbUpdateException>();
    }

    [Fact]
    public async Task MerchantCreated_SameEventDifferentNotificationTypes_AreAllowed()
    {
        using LastHourDbContext dbContext = CreateDbContext();
        Guid eventId = Guid.NewGuid();
        Guid merchantId = Guid.NewGuid();

        var notificationWelcome = Notification.Create(
            eventId,
            merchantId,
            "+123456789",
            "Welcome",
            "Welcome message",
            "MerchantCreated",
            _clock.UtcNow);

        var notificationSms = Notification.Create(
            eventId,
            merchantId,
            "+123456789",
            "SMS Verification",
            "Your code is 123456",
            "MerchantSmsVerification",
            _clock.UtcNow);

        dbContext.Set<Notification>().Add(notificationWelcome);
        dbContext.Set<Notification>().Add(notificationSms);
        await dbContext.SaveChangesAsync();

        List<Notification> saved = await dbContext.Set<Notification>().Where(n => n.EventId == eventId).ToListAsync();
        saved.Should().HaveCount(2);
    }

    [Fact]
    public async Task MerchantCreated_DifferentEvents_SameType_AreAllowed()
    {
        using LastHourDbContext dbContext = CreateDbContext();
        var notification1 = Notification.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "+111111111",
            "Welcome 1",
            "Message 1",
            "MerchantCreated",
            _clock.UtcNow);

        var notification2 = Notification.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "+222222222",
            "Welcome 2",
            "Message 2",
            "MerchantCreated",
            _clock.UtcNow);

        dbContext.Set<Notification>().Add(notification1);
        dbContext.Set<Notification>().Add(notification2);
        await dbContext.SaveChangesAsync();

        List<Notification> saved = await dbContext.Set<Notification>().ToListAsync();
        saved.Should().HaveCount(2);
    }

    [Fact]
    public async Task MerchantCreated_CrashAfterBusinessCommitBeforeInboxCompletion_DoesNotDuplicateNotification()
    {
        // 1. Arrange: First delivery starts and acquires inbox lease
        Guid eventId = Guid.NewGuid();
        Guid merchantId = Guid.NewGuid();
        var domainEvent = new MerchantCreatedDomainEvent(merchantId, "Crashed Worker Bakery", "100 Pine St", "+15550001111")
        {
            EventId = eventId,
        };

        // Worker 1 acquires consumer inbox lease
        using (LastHourDbContext context1 = CreateDbContext())
        {
            var idempotency1 = new ConsumerIdempotencyService(
                context1,
                _clock,
                _metrics,
                Microsoft.Extensions.Options.Options.Create(new ConsumerIdempotencyOptions { LeaseDuration = TimeSpan.FromMinutes(2) }),
                NullLogger<ConsumerIdempotencyService>.Instance);

            bool acquired = await idempotency1.TryAcquireAsync(eventId, MerchantCreatedNotificationHandler.ConsumerName);
            acquired.Should().BeTrue();

            // Worker 1 persists the business Notification directly to the database (simulating successful business transaction)
            var notification = Notification.Create(
                eventId,
                merchantId,
                "+15550001111",
                "Welcome to LastHour",
                "Your merchant account has been registered.",
                "MerchantCreated",
                _clock.UtcNow);

            context1.Set<Notification>().Add(notification);
            await context1.SaveChangesAsync();

            // SIMULATED CRASH: Worker 1 terminates here WITHOUT calling CompleteAsync!
            // The ConsumerInboxMessage row exists in the database with CompletedOnUtc == null and LockExpiresAtUtc == _clock.UtcNow + 2 minutes.
        }

        // 2. Act: Advance clock past lease expiration
        _clock.UtcNow = _clock.UtcNow.AddMinutes(3);

        // Worker 2 (retry/subsequent delivery) processes the message
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
            var handler = new MerchantCreatedNotificationHandler(
                idempotency2,
                notificationRepo,
                unitOfWork,
                _clock,
                NullLogger<MerchantCreatedNotificationHandler>.Instance);

            await handler.Handle(new NotificationMessage<MerchantCreatedDomainEvent>(domainEvent), CancellationToken.None);
        }

        // 3. Assert:
        using (LastHourDbContext verifyContext = CreateDbContext())
        {
            // Business effect was NOT duplicated
            List<Notification> notifications = await verifyContext.Set<Notification>()
                .Where(n => n.EventId == eventId && n.Type == "MerchantCreated")
                .ToListAsync();
            notifications.Should().ContainSingle();

            // Consumer inbox message was successfully completed
            ConsumerInboxMessage? inboxMessage = await verifyContext.ConsumerInboxMessages
                .FirstOrDefaultAsync(m => m.EventId == eventId && m.Consumer == MerchantCreatedNotificationHandler.ConsumerName);
            inboxMessage.Should().NotBeNull();
            inboxMessage!.IsCompleted.Should().BeTrue();
        }
    }

    [Fact]
    public async Task MerchantCreated_ConcurrentRetries_ProduceSingleBusinessEffect()
    {
        var domainEvent = new MerchantCreatedDomainEvent(Guid.NewGuid(), "Concurrent Merchant", "Address", "+1234");

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
            var handler = new MerchantCreatedNotificationHandler(
                idempotency,
                notificationRepo,
                unitOfWork,
                _clock,
                NullLogger<MerchantCreatedNotificationHandler>.Instance);

            await handler.Handle(new NotificationMessage<MerchantCreatedDomainEvent>(domainEvent), CancellationToken.None);
        }).ToArray();

        await Task.WhenAll(tasks);

        using LastHourDbContext verifyContext = CreateDbContext();
        List<Notification> notifications = await verifyContext.Set<Notification>().ToListAsync();
        notifications.Should().ContainSingle();

        ConsumerInboxMessage inbox = await verifyContext.ConsumerInboxMessages.SingleAsync();
        inbox.IsCompleted.Should().BeTrue();
    }

    [Fact]
    public async Task MerchantCreated_DuplicateUniqueConstraint_IsRecoveredAsSuccess()
    {
        var (handler, _, _, dbContext) = CreateSut();
        Guid eventId = Guid.NewGuid();
        Guid merchantId = Guid.NewGuid();
        var domainEvent = new MerchantCreatedDomainEvent(merchantId, "Race Winner Merchant", "Address", "+1234")
        {
            EventId = eventId,
        };

        // Insert notification directly simulating concurrent commit before this worker's SaveChangesAsync
        var existingNotification = Notification.Create(
            eventId,
            merchantId,
            "+1234",
            "Welcome",
            "Welcome message",
            "MerchantCreated",
            _clock.UtcNow);

        dbContext.Set<Notification>().Add(existingNotification);
        await dbContext.SaveChangesAsync();

        // Handler runs and encounters the existing business effect, completing cleanly without throwing
        Func<Task> act = () => handler.Handle(new NotificationMessage<MerchantCreatedDomainEvent>(domainEvent), CancellationToken.None);
        await act.Should().NotThrowAsync();

        List<Notification> notifications = await dbContext.Set<Notification>().ToListAsync();
        notifications.Should().ContainSingle();

        ConsumerInboxMessage inbox = await dbContext.ConsumerInboxMessages.SingleAsync();
        inbox.IsCompleted.Should().BeTrue();
    }

    [Fact]
    public async Task MerchantCreated_UnrelatedDatabaseFailure_IsNotSwallowed()
    {
        LastHourDbContext dbContext = CreateDbContext();
        var idempotencyService = new ConsumerIdempotencyService(
            dbContext,
            _clock,
            _metrics,
            Microsoft.Extensions.Options.Options.Create(new ConsumerIdempotencyOptions()),
            NullLogger<ConsumerIdempotencyService>.Instance);

        var domainEvent = new MerchantCreatedDomainEvent(Guid.NewGuid(), "Unrelated Failure", "Address", "+1234");
        var message = new NotificationMessage<MerchantCreatedDomainEvent>(domainEvent);

        var failingRepo = new FailingNotificationRepository();
        var unitOfWork = new EfUnitOfWork(dbContext);
        var handler = new MerchantCreatedNotificationHandler(
            idempotencyService,
            failingRepo,
            unitOfWork,
            _clock,
            NullLogger<MerchantCreatedNotificationHandler>.Instance);

        Func<Task> act = () => handler.Handle(message, CancellationToken.None);
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("Database connection failure.");

        // In-progress record was released, leaving it retryable
        ConsumerInboxMessage? inboxMessage = await dbContext.ConsumerInboxMessages
            .FirstOrDefaultAsync(m => m.EventId == domainEvent.EventId && m.Consumer == MerchantCreatedNotificationHandler.ConsumerName);
        inboxMessage.Should().BeNull();
    }

    [Fact]
    public async Task MerchantCreated_Cancellation_DoesNotConvertIntoDuplicateSuccess()
    {
        var (handler, _, _, dbContext) = CreateSut();
        var domainEvent = new MerchantCreatedDomainEvent(Guid.NewGuid(), "Cancelled Merchant", "Address", "+1234");
        var message = new NotificationMessage<MerchantCreatedDomainEvent>(domainEvent);

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        Func<Task> act = () => handler.Handle(message, cts.Token);
        await act.Should().ThrowAsync<OperationCanceledException>();

        ConsumerInboxMessage? inboxMessage = await dbContext.ConsumerInboxMessages
            .FirstOrDefaultAsync(m => m.EventId == domainEvent.EventId && m.Consumer == MerchantCreatedNotificationHandler.ConsumerName);
        inboxMessage.Should().BeNull();
    }

    [Fact]
    public void Notification_UniqueIndex_UsesEventIdAndType()
    {
        using LastHourDbContext context = CreateDbContext();
        var entityType = context.Model.FindEntityType(typeof(Notification));
        entityType.Should().NotBeNull();

        var index = entityType!.FindIndex(new[]
        {
            entityType.FindProperty(nameof(Notification.EventId))!,
            entityType.FindProperty(nameof(Notification.Type))!,
        });

        index.Should().NotBeNull();
        index!.IsUnique.Should().BeTrue();
        index.GetDatabaseName().Should().Be("IX_notifications_event_id_type");
    }

    [Fact]
    public async Task ManualOrNonEventNotificationBehavior_RemainsSupported()
    {
        using LastHourDbContext dbContext = CreateDbContext();

        // Multiple non-event notifications (EventId = null) are allowed and do not conflict
        var manualNotification1 = Notification.Create(
            null,
            Guid.NewGuid(),
            "+111111111",
            "Manual Broadcast 1",
            "System announcement",
            "SystemBroadcast",
            _clock.UtcNow);

        var manualNotification2 = Notification.Create(
            null,
            Guid.NewGuid(),
            "+222222222",
            "Manual Broadcast 2",
            "System announcement",
            "SystemBroadcast",
            _clock.UtcNow);

        dbContext.Set<Notification>().Add(manualNotification1);
        dbContext.Set<Notification>().Add(manualNotification2);
        await dbContext.SaveChangesAsync();

        List<Notification> saved = await dbContext.Set<Notification>().Where(n => n.EventId == null).ToListAsync();
        saved.Should().HaveCount(2);
    }

    [Fact]
    public async Task ConsumerMetrics_AreCorrect_WhenBusinessEffectAlreadyExists()
    {
        var (handler, _, _, dbContext) = CreateSut();
        var domainEvent = new MerchantCreatedDomainEvent(Guid.NewGuid(), "Metrics Merchant", "Address", "+1234");
        var message = new NotificationMessage<MerchantCreatedDomainEvent>(domainEvent);

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

            if (consumer != MerchantCreatedNotificationHandler.ConsumerName)
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
    public void MerchantCreated_ConsumerIdentityIsStable()
    {
        MerchantCreatedNotificationHandler.ConsumerName.Should().Be("Notifications.MerchantCreated");
    }

    [Fact]
    public async Task MerchantCreated_HandlerFailure_RemainsRetryable()
    {
        LastHourDbContext dbContext = CreateDbContext();
        var idempotencyService = new ConsumerIdempotencyService(
            dbContext,
            _clock,
            _metrics,
            NullLogger<ConsumerIdempotencyService>.Instance);

        var domainEvent = new MerchantCreatedDomainEvent(Guid.NewGuid(), "Failing Merchant", "Address", "+1234");
        var message = new NotificationMessage<MerchantCreatedDomainEvent>(domainEvent);

        var failingRepo = new FailingNotificationRepository();
        var unitOfWork = new EfUnitOfWork(dbContext);
        var handler = new MerchantCreatedNotificationHandler(
            idempotencyService,
            failingRepo,
            unitOfWork,
            _clock,
            NullLogger<MerchantCreatedNotificationHandler>.Instance);

        Func<Task> act = () => handler.Handle(message, CancellationToken.None);
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("Database connection failure.");

        // In-progress record was cleanly released
        ConsumerInboxMessage? inboxMessage = await dbContext.ConsumerInboxMessages
            .FirstOrDefaultAsync(m => m.EventId == domainEvent.EventId && m.Consumer == MerchantCreatedNotificationHandler.ConsumerName);
        inboxMessage.Should().BeNull();
    }

    [Fact]
    public async Task MerchantCreated_RetryAfterFailure_Succeeds()
    {
        LastHourDbContext dbContext = CreateDbContext();
        var idempotencyService = new ConsumerIdempotencyService(
            dbContext,
            _clock,
            _metrics,
            NullLogger<ConsumerIdempotencyService>.Instance);

        var domainEvent = new MerchantCreatedDomainEvent(Guid.NewGuid(), "Recovering Merchant", "Address", "+1234");
        var message = new NotificationMessage<MerchantCreatedDomainEvent>(domainEvent);

        var flakyRepo = new FlakyNotificationRepository(dbContext);
        var unitOfWork = new EfUnitOfWork(dbContext);
        var handler = new MerchantCreatedNotificationHandler(
            idempotencyService,
            flakyRepo,
            unitOfWork,
            _clock,
            NullLogger<MerchantCreatedNotificationHandler>.Instance);

        // Attempt 1: Fails
        Func<Task> act = () => handler.Handle(message, CancellationToken.None);
        await act.Should().ThrowAsync<InvalidOperationException>();

        // Attempt 2: Succeeds on retry
        await handler.Handle(message, CancellationToken.None);

        List<Notification> notifications = await dbContext.Set<Notification>().ToListAsync();
        notifications.Should().ContainSingle();
        ConsumerInboxMessage? inboxMessage = await dbContext.ConsumerInboxMessages
            .FirstOrDefaultAsync(m => m.EventId == domainEvent.EventId && m.Consumer == MerchantCreatedNotificationHandler.ConsumerName);
        inboxMessage.Should().NotBeNull();
        inboxMessage!.IsCompleted.Should().BeTrue();
    }

    [Fact]
    public async Task MerchantCreated_EventIdIsPreserved()
    {
        var (handler, _, _, dbContext) = CreateSut();
        Guid expectedEventId = Guid.NewGuid();
        var domainEvent = new MerchantCreatedDomainEvent(Guid.NewGuid(), "EventId Test", "Address", "+1234")
        {
            EventId = expectedEventId,
        };

        await handler.Handle(new NotificationMessage<MerchantCreatedDomainEvent>(domainEvent), CancellationToken.None);

        Notification notification = await dbContext.Set<Notification>().SingleAsync();
        notification.EventId.Should().Be(expectedEventId);

        ConsumerInboxMessage inbox = await dbContext.ConsumerInboxMessages.SingleAsync();
        inbox.EventId.Should().Be(expectedEventId);
    }

    [Fact]
    public async Task Merchant_EndToEnd_OutboxToNotification_ExecutesSuccessfully()
    {
        // 1. Setup DI container with real OutboxProcessor, EventDispatcher, MediatR, and Notifications module
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
            cfg.RegisterServicesFromAssembly(typeof(MerchantCreatedNotificationHandler).Assembly);
        });
        services.Configure<OutboxOptions>(opts =>
        {
            opts.BatchSize = 10;
            opts.ProcessingInterval = TimeSpan.FromMilliseconds(50);
            opts.LeaseDuration = TimeSpan.FromMinutes(2);
            opts.MaxRetryCount = 3;
        });

        IServiceProvider serviceProvider = services.BuildServiceProvider();

        // 2. Register a Merchant aggregate which raises MerchantCreatedDomainEvent
        Guid ownerId = Guid.NewGuid();
        MerchantAggregate merchant = MerchantAggregate.Register(
            ownerId,
            MerchantName.From("Artisan Bakery"),
            Address.Create("123 Bakery St", "Seattle", "WA", "98101", "USA"),
            PhoneNumber.From("+12065550100"));

        IReadOnlyCollection<IDomainEvent> domainEvents = merchant.GetDomainEvents();
        domainEvents.Should().ContainSingle();
        MerchantCreatedDomainEvent domainEvent = domainEvents.OfType<MerchantCreatedDomainEvent>().Single();

        // 3. Persist to outbox table (simulating DomainEventsInterceptor / OutboxPublisher)
        using (IServiceScope scope = serviceProvider.CreateScope())
        {
            LastHourDbContext dbContext = scope.ServiceProvider.GetRequiredService<LastHourDbContext>();
            var outboxMessage = OutboxMessage.FromDomainEvent(domainEvent);
            dbContext.OutboxMessages.Add(outboxMessage);
            await dbContext.SaveChangesAsync();
        }

        // 4. Run OutboxProcessor iteration to dispatch the pending outbox message
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

            // 5a. Outbox message was marked processed
            OutboxMessage processedOutbox = await dbContext.OutboxMessages.SingleAsync();
            processedOutbox.ProcessedOnUtc.Should().NotBeNull();

            // 5b. Consumer inbox record exists and is completed
            ConsumerInboxMessage inbox = await dbContext.ConsumerInboxMessages.SingleAsync();
            inbox.EventId.Should().Be(domainEvent.EventId);
            inbox.Consumer.Should().Be(MerchantCreatedNotificationHandler.ConsumerName);
            inbox.IsCompleted.Should().BeTrue();

            // 5c. Exactly one durable Notification was created
            Notification notification = await dbContext.Set<Notification>().SingleAsync();
            notification.EventId.Should().Be(domainEvent.EventId);
            notification.Recipient.Should().Be("+12065550100");
            notification.Type.Should().Be("MerchantCreated");
            notification.Body.Should().Contain("Artisan Bakery");
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
            cfg.RegisterServicesFromAssembly(typeof(MerchantCreatedNotificationHandler).Assembly);
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
        Guid ownerId = Guid.NewGuid();
        MerchantAggregate merchant = MerchantAggregate.Register(
            ownerId,
            MerchantName.From("Crash Recovery Bakery"),
            Address.Create("500 Pike St", "Seattle", "WA", "98101", "USA"),
            PhoneNumber.From("+12065559999"));

        MerchantCreatedDomainEvent domainEvent = merchant.GetDomainEvents().OfType<MerchantCreatedDomainEvent>().Single();

        // 3. Pre-populate existing business effect (simulating prior committed execution before crash)
        using (IServiceScope scope = serviceProvider.CreateScope())
        {
            LastHourDbContext dbContext = scope.ServiceProvider.GetRequiredService<LastHourDbContext>();

            var existingNotification = Notification.Create(
                domainEvent.EventId,
                domainEvent.MerchantId,
                domainEvent.Phone,
                "Welcome to LastHour",
                "Your merchant account has been registered.",
                "MerchantCreated",
                _clock.UtcNow);

            dbContext.Set<Notification>().Add(existingNotification);

            // Add outbox message eligible for retry
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

            // Outbox processed successfully
            OutboxMessage processedOutbox = await dbContext.OutboxMessages.SingleAsync();
            processedOutbox.ProcessedOnUtc.Should().NotBeNull();

            // Consumer inbox record completed
            ConsumerInboxMessage inbox = await dbContext.ConsumerInboxMessages.SingleAsync();
            inbox.EventId.Should().Be(domainEvent.EventId);
            inbox.IsCompleted.Should().BeTrue();

            // Notification count remains exactly 1 (no duplicate)
            List<Notification> notifications = await dbContext.Set<Notification>()
                .Where(n => n.EventId == domainEvent.EventId)
                .ToListAsync();
            notifications.Should().ContainSingle();
        }
    }

    [Fact]
    public async Task Merchant_OutboxLeaseExpiresWhileConsumerExecutes_SecondOutboxDeliveryIsSuppressedByActiveConsumerLease()
    {
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
            cfg.RegisterServicesFromAssembly(typeof(MerchantCreatedNotificationHandler).Assembly);
        });
        services.Configure<OutboxOptions>(opts =>
        {
            opts.BatchSize = 10;
            opts.ProcessingInterval = TimeSpan.FromMilliseconds(50);
            opts.LeaseDuration = TimeSpan.FromMinutes(1);
            opts.MaxRetryCount = 3;
        });

        IServiceProvider serviceProvider = services.BuildServiceProvider();

        Guid ownerId = Guid.NewGuid();
        MerchantAggregate merchant = MerchantAggregate.Register(
            ownerId,
            MerchantName.From("Long Running Merchant"),
            Address.Create("900 Pine St", "Seattle", "WA", "98101", "USA"),
            PhoneNumber.From("+12065558888"));

        MerchantCreatedDomainEvent domainEvent = merchant.GetDomainEvents().OfType<MerchantCreatedDomainEvent>().Single();

        // 1. Worker 1 acquires consumer inbox lease at T0 (consumer lease expires at T0 + 2m)
        using (IServiceScope scope1 = serviceProvider.CreateScope())
        {
            var idempotency = scope1.ServiceProvider.GetRequiredService<IConsumerIdempotencyService>();
            bool acquired = await idempotency.TryAcquireAsync(domainEvent.EventId, MerchantCreatedNotificationHandler.ConsumerName);
            acquired.Should().BeTrue();
        }

        // 2. Outbox lease expires at T0 + 1.5m (consumer lease still active until T0 + 2m)
        _clock.UtcNow = _clock.UtcNow.AddMinutes(1.5);

        // 3. Outbox processor tries to dispatch to consumer
        using (IServiceScope scope2 = serviceProvider.CreateScope())
        {
            var dispatcher = scope2.ServiceProvider.GetRequiredService<IEventDispatcher>();

            // Second delivery attempts to run while first consumer lease is still active
            await dispatcher.DispatchAsync(domainEvent, CancellationToken.None);
        }

        // 4. Verify no duplicate notification was created
        using (IServiceScope verifyScope = serviceProvider.CreateScope())
        {
            LastHourDbContext dbContext = verifyScope.ServiceProvider.GetRequiredService<LastHourDbContext>();
            List<Notification> notifications = await dbContext.Set<Notification>()
                .Where(n => n.EventId == domainEvent.EventId)
                .ToListAsync();
            notifications.Should().BeEmpty("Second delivery was suppressed while first lease was active");
        }

        // 5. Worker 1 finishes its work and completes
        using (IServiceScope scope1Complete = serviceProvider.CreateScope())
        {
            var notificationRepo = scope1Complete.ServiceProvider.GetRequiredService<INotificationRepository>();
            var unitOfWork = scope1Complete.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var idempotency = scope1Complete.ServiceProvider.GetRequiredService<IConsumerIdempotencyService>();

            var record = Notification.Create(
                domainEvent.EventId,
                domainEvent.MerchantId,
                domainEvent.Phone,
                "Welcome",
                "Body",
                "MerchantCreated",
                _clock.UtcNow);
            await notificationRepo.AddAsync(record);
            await unitOfWork.SaveChangesAsync();
            await idempotency.CompleteAsync(domainEvent.EventId, MerchantCreatedNotificationHandler.ConsumerName);
        }

        // 6. Verify exactly 1 notification and completed inbox
        using (IServiceScope finalVerifyScope = serviceProvider.CreateScope())
        {
            LastHourDbContext dbContext = finalVerifyScope.ServiceProvider.GetRequiredService<LastHourDbContext>();
            List<Notification> notifications = await dbContext.Set<Notification>()
                .Where(n => n.EventId == domainEvent.EventId)
                .ToListAsync();
            notifications.Should().ContainSingle();

            ConsumerInboxMessage inbox = await dbContext.ConsumerInboxMessages.SingleAsync(m => m.EventId == domainEvent.EventId);
            inbox.IsCompleted.Should().BeTrue();
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
