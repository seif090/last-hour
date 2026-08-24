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
using LastHour.Modules.Notifications.Application.Consumers;
using LastHour.Modules.Notifications.Application.Persistence;
using LastHour.Modules.Notifications.Domain.Notifications;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace LastHour.Modules.Notifications.Tests.Application.Consumers;

public class UserRegisteredNotificationHandlerTests : IDisposable
{
    private readonly DbConnection _connection;
    private readonly DbContextOptions<LastHourDbContext> _dbContextOptions;
    private readonly TestClock _clock;
    private readonly ConsumerIdempotencyMetrics _metrics;

    public UserRegisteredNotificationHandlerTests()
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
            });

    private (UserRegisteredNotificationHandler Handler, IConsumerIdempotencyService IdempotencyService, INotificationRepository NotificationRepo, LastHourDbContext DbContext) CreateSut()
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

        var handler = new UserRegisteredNotificationHandler(
            idempotencyService,
            notificationRepo,
            unitOfWork,
            _clock,
            NullLogger<UserRegisteredNotificationHandler>.Instance);

        return (handler, idempotencyService, notificationRepo, dbContext);
    }

    [Fact]
    public async Task UserRegistered_FirstDelivery_CreatesNotification()
    {
        var (handler, _, _, dbContext) = CreateSut();
        Guid userId = Guid.NewGuid();
        string email = "newuser@example.com";
        var domainEvent = new UserRegisteredDomainEvent(userId, email);
        var message = new NotificationMessage<UserRegisteredDomainEvent>(domainEvent);

        await handler.Handle(message, CancellationToken.None);

        List<Notification> notifications = await dbContext.Set<Notification>().ToListAsync();
        notifications.Should().ContainSingle();
        Notification notification = notifications.Single();
        notification.EventId.Should().Be(domainEvent.EventId);
        notification.RecipientId.Should().Be(userId);
        notification.Recipient.Should().Be(userId.ToString("D"));
        notification.Type.Should().Be("UserRegistered");
        notification.Title.Should().Be("Welcome to LastHour");
        notification.Body.Should().Be("Your account has been created successfully. Welcome to LastHour.");

        ConsumerInboxMessage? inboxMessage = await dbContext.ConsumerInboxMessages
            .FirstOrDefaultAsync(m => m.EventId == domainEvent.EventId && m.Consumer == UserRegisteredNotificationHandler.ConsumerName);
        inboxMessage.Should().NotBeNull();
        inboxMessage!.IsCompleted.Should().BeTrue();
    }

    [Fact]
    public async Task UserRegistered_DuplicateDelivery_DoesNotCreateSecondNotification()
    {
        var (handler, _, _, dbContext) = CreateSut();
        Guid userId = Guid.NewGuid();
        var domainEvent = new UserRegisteredDomainEvent(userId, "newuser@example.com");
        var message = new NotificationMessage<UserRegisteredDomainEvent>(domainEvent);

        await handler.Handle(message, CancellationToken.None);
        await handler.Handle(message, CancellationToken.None);

        List<Notification> notifications = await dbContext.Set<Notification>().ToListAsync();
        notifications.Should().ContainSingle();
    }

    [Fact]
    public async Task UserRegistered_SameEventAndType_DatabaseConstraintPreventsDuplicate()
    {
        using LastHourDbContext dbContext = CreateDbContext();
        Guid eventId = Guid.NewGuid();
        Guid userId = Guid.NewGuid();

        var first = Notification.Create(
            eventId,
            userId,
            userId.ToString("D"),
            "Welcome to LastHour",
            "Your account has been created successfully. Welcome to LastHour.",
            "UserRegistered",
            DateTime.UtcNow);

        var duplicate = Notification.Create(
            eventId,
            userId,
            userId.ToString("D"),
            "Welcome to LastHour",
            "Your account has been created successfully. Welcome to LastHour.",
            "UserRegistered",
            DateTime.UtcNow);

        dbContext.Set<Notification>().Add(first);
        await dbContext.SaveChangesAsync();

        dbContext.Set<Notification>().Add(duplicate);
        Func<Task> act = () => dbContext.SaveChangesAsync();

        await act.Should().ThrowAsync<DbUpdateException>();
    }

    [Fact]
    public async Task UserRegistered_SameEventDifferentNotificationTypes_AreAllowed()
    {
        using LastHourDbContext dbContext = CreateDbContext();
        Guid eventId = Guid.NewGuid();
        Guid userId = Guid.NewGuid();

        var notification1 = Notification.Create(
            eventId,
            userId,
            userId.ToString("D"),
            "Welcome to LastHour",
            "Your account has been created successfully. Welcome to LastHour.",
            "UserRegistered",
            DateTime.UtcNow);

        var notification2 = Notification.Create(
            eventId,
            userId,
            userId.ToString("D"),
            "Security Alert",
            "Account registered.",
            "SecurityAlert",
            DateTime.UtcNow);

        dbContext.Set<Notification>().AddRange(notification1, notification2);
        await dbContext.SaveChangesAsync();

        List<Notification> saved = await dbContext.Set<Notification>().Where(n => n.EventId == eventId).ToListAsync();
        saved.Should().HaveCount(2);
    }

    [Fact]
    public async Task UserRegistered_DifferentEvents_SameType_AreAllowed()
    {
        using LastHourDbContext dbContext = CreateDbContext();
        Guid eventId1 = Guid.NewGuid();
        Guid eventId2 = Guid.NewGuid();
        Guid userId = Guid.NewGuid();

        var notification1 = Notification.Create(
            eventId1,
            userId,
            userId.ToString("D"),
            "Welcome to LastHour",
            "Your account has been created successfully. Welcome to LastHour.",
            "UserRegistered",
            DateTime.UtcNow);

        var notification2 = Notification.Create(
            eventId2,
            userId,
            userId.ToString("D"),
            "Welcome to LastHour",
            "Your account has been created successfully. Welcome to LastHour.",
            "UserRegistered",
            DateTime.UtcNow);

        dbContext.Set<Notification>().AddRange(notification1, notification2);
        await dbContext.SaveChangesAsync();

        List<Notification> saved = await dbContext.Set<Notification>().ToListAsync();
        saved.Should().HaveCount(2);
    }

    [Fact]
    public async Task UserRegistered_CrashAfterBusinessCommitBeforeInboxCompletion_DoesNotDuplicateNotification()
    {
        // 1. Arrange: Worker 1 acquires inbox lease and commits notification, then crashes before completing inbox
        Guid eventId = Guid.NewGuid();
        Guid userId = Guid.NewGuid();
        var domainEvent = new UserRegisteredDomainEvent(userId, "newuser@example.com")
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

            bool acquired = await idempotency1.TryAcquireAsync(eventId, UserRegisteredNotificationHandler.ConsumerName);
            acquired.Should().BeTrue();

            var notification = Notification.Create(
                eventId,
                userId,
                userId.ToString("D"),
                "Welcome to LastHour",
                "Your account has been created successfully. Welcome to LastHour.",
                "UserRegistered",
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
            var handler = new UserRegisteredNotificationHandler(
                idempotency2,
                notificationRepo,
                unitOfWork,
                _clock,
                NullLogger<UserRegisteredNotificationHandler>.Instance);

            await handler.Handle(new NotificationMessage<UserRegisteredDomainEvent>(domainEvent), CancellationToken.None);
        }

        // 3. Assert
        using (LastHourDbContext verifyContext = CreateDbContext())
        {
            List<Notification> notifications = await verifyContext.Set<Notification>()
                .Where(n => n.EventId == eventId && n.Type == "UserRegistered")
                .ToListAsync();
            notifications.Should().ContainSingle();

            ConsumerInboxMessage? inboxMessage = await verifyContext.ConsumerInboxMessages
                .FirstOrDefaultAsync(m => m.EventId == eventId && m.Consumer == UserRegisteredNotificationHandler.ConsumerName);
            inboxMessage.Should().NotBeNull();
            inboxMessage!.IsCompleted.Should().BeTrue();
        }
    }

    [Fact]
    public async Task UserRegistered_ConcurrentRetries_ProduceSingleBusinessEffect()
    {
        var domainEvent = new UserRegisteredDomainEvent(Guid.NewGuid(), "concurrent@example.com");

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
            var handler = new UserRegisteredNotificationHandler(
                idempotency,
                notificationRepo,
                unitOfWork,
                _clock,
                NullLogger<UserRegisteredNotificationHandler>.Instance);

            await handler.Handle(new NotificationMessage<UserRegisteredDomainEvent>(domainEvent), CancellationToken.None);
        }).ToArray();

        await Task.WhenAll(tasks);

        using LastHourDbContext verifyContext = CreateDbContext();
        List<Notification> notifications = await verifyContext.Set<Notification>().ToListAsync();
        notifications.Should().ContainSingle();

        ConsumerInboxMessage inbox = await verifyContext.ConsumerInboxMessages.SingleAsync();
        inbox.IsCompleted.Should().BeTrue();
    }

    [Fact]
    public async Task UserRegistered_DuplicateUniqueConstraint_IsRecoveredAsSuccess()
    {
        var (handler, _, _, dbContext) = CreateSut();
        Guid eventId = Guid.NewGuid();
        Guid userId = Guid.NewGuid();
        var domainEvent = new UserRegisteredDomainEvent(userId, "race@example.com")
        {
            EventId = eventId,
        };

        var existingNotification = Notification.Create(
            eventId,
            userId,
            userId.ToString("D"),
            "Welcome to LastHour",
            "Your account has been created successfully. Welcome to LastHour.",
            "UserRegistered",
            _clock.UtcNow);

        dbContext.Set<Notification>().Add(existingNotification);
        await dbContext.SaveChangesAsync();

        Func<Task> act = () => handler.Handle(new NotificationMessage<UserRegisteredDomainEvent>(domainEvent), CancellationToken.None);
        await act.Should().NotThrowAsync();

        List<Notification> notifications = await dbContext.Set<Notification>().ToListAsync();
        notifications.Should().ContainSingle();

        ConsumerInboxMessage inbox = await dbContext.ConsumerInboxMessages.SingleAsync();
        inbox.IsCompleted.Should().BeTrue();
    }

    [Fact]
    public async Task UserRegistered_UnrelatedDatabaseFailure_IsNotSwallowed()
    {
        LastHourDbContext dbContext = CreateDbContext();
        var idempotencyService = new ConsumerIdempotencyService(
            dbContext,
            _clock,
            _metrics,
            Microsoft.Extensions.Options.Options.Create(new ConsumerIdempotencyOptions()),
            NullLogger<ConsumerIdempotencyService>.Instance);

        var domainEvent = new UserRegisteredDomainEvent(Guid.NewGuid(), "failure@example.com");
        var message = new NotificationMessage<UserRegisteredDomainEvent>(domainEvent);

        var failingRepo = new FailingNotificationRepository();
        var unitOfWork = new EfUnitOfWork(dbContext);
        var handler = new UserRegisteredNotificationHandler(
            idempotencyService,
            failingRepo,
            unitOfWork,
            _clock,
            NullLogger<UserRegisteredNotificationHandler>.Instance);

        Func<Task> act = () => handler.Handle(message, CancellationToken.None);
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("Database connection failure.");

        ConsumerInboxMessage? inboxMessage = await dbContext.ConsumerInboxMessages
            .FirstOrDefaultAsync(m => m.EventId == domainEvent.EventId && m.Consumer == UserRegisteredNotificationHandler.ConsumerName);
        inboxMessage.Should().BeNull();
    }

    [Fact]
    public async Task UserRegistered_Cancellation_DoesNotConvertIntoDuplicateSuccess()
    {
        var (handler, _, _, dbContext) = CreateSut();
        var domainEvent = new UserRegisteredDomainEvent(Guid.NewGuid(), "cancel@example.com");
        var message = new NotificationMessage<UserRegisteredDomainEvent>(domainEvent);

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        Func<Task> act = () => handler.Handle(message, cts.Token);
        await act.Should().ThrowAsync<OperationCanceledException>();

        ConsumerInboxMessage? inboxMessage = await dbContext.ConsumerInboxMessages
            .FirstOrDefaultAsync(m => m.EventId == domainEvent.EventId && m.Consumer == UserRegisteredNotificationHandler.ConsumerName);
        inboxMessage.Should().BeNull();
    }

    [Fact]
    public async Task UserRegistered_ConsumerMetrics_AreCorrect_WhenBusinessEffectAlreadyExists()
    {
        var (handler, _, _, dbContext) = CreateSut();
        var domainEvent = new UserRegisteredDomainEvent(Guid.NewGuid(), "metric@example.com");
        var message = new NotificationMessage<UserRegisteredDomainEvent>(domainEvent);

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

            if (consumer != UserRegisteredNotificationHandler.ConsumerName)
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
    public void UserRegistered_ConsumerIdentityIsStable()
    {
        UserRegisteredNotificationHandler.ConsumerName.Should().Be("Notifications.UserRegistered");
    }

    [Fact]
    public async Task UserRegistered_HandlerFailure_RemainsRetryable()
    {
        LastHourDbContext dbContext = CreateDbContext();
        var idempotencyService = new ConsumerIdempotencyService(
            dbContext,
            _clock,
            _metrics,
            NullLogger<ConsumerIdempotencyService>.Instance);

        var domainEvent = new UserRegisteredDomainEvent(Guid.NewGuid(), "retryable@example.com");
        var message = new NotificationMessage<UserRegisteredDomainEvent>(domainEvent);

        var failingRepo = new FailingNotificationRepository();
        var unitOfWork = new EfUnitOfWork(dbContext);
        var handler = new UserRegisteredNotificationHandler(
            idempotencyService,
            failingRepo,
            unitOfWork,
            _clock,
            NullLogger<UserRegisteredNotificationHandler>.Instance);

        Func<Task> act = () => handler.Handle(message, CancellationToken.None);
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("Database connection failure.");

        ConsumerInboxMessage? inboxMessage = await dbContext.ConsumerInboxMessages
            .FirstOrDefaultAsync(m => m.EventId == domainEvent.EventId && m.Consumer == UserRegisteredNotificationHandler.ConsumerName);
        inboxMessage.Should().BeNull();
    }

    [Fact]
    public async Task UserRegistered_RetryAfterFailure_Succeeds()
    {
        LastHourDbContext dbContext = CreateDbContext();
        var idempotencyService = new ConsumerIdempotencyService(
            dbContext,
            _clock,
            _metrics,
            NullLogger<ConsumerIdempotencyService>.Instance);

        var domainEvent = new UserRegisteredDomainEvent(Guid.NewGuid(), "retrysuccess@example.com");
        var message = new NotificationMessage<UserRegisteredDomainEvent>(domainEvent);

        var flakyRepo = new FlakyNotificationRepository(dbContext);
        var unitOfWork = new EfUnitOfWork(dbContext);
        var handler = new UserRegisteredNotificationHandler(
            idempotencyService,
            flakyRepo,
            unitOfWork,
            _clock,
            NullLogger<UserRegisteredNotificationHandler>.Instance);

        // Attempt 1: Fails
        Func<Task> act = () => handler.Handle(message, CancellationToken.None);
        await act.Should().ThrowAsync<InvalidOperationException>();

        // Attempt 2: Succeeds on retry
        await handler.Handle(message, CancellationToken.None);

        List<Notification> notifications = await dbContext.Set<Notification>().ToListAsync();
        notifications.Should().ContainSingle();
        ConsumerInboxMessage? inboxMessage = await dbContext.ConsumerInboxMessages
            .FirstOrDefaultAsync(m => m.EventId == domainEvent.EventId && m.Consumer == UserRegisteredNotificationHandler.ConsumerName);
        inboxMessage.Should().NotBeNull();
        inboxMessage!.IsCompleted.Should().BeTrue();
    }

    [Fact]
    public async Task UserRegistered_EventIdIsPreserved()
    {
        var (handler, _, _, dbContext) = CreateSut();
        Guid expectedEventId = Guid.NewGuid();
        var domainEvent = new UserRegisteredDomainEvent(Guid.NewGuid(), "preservecase@example.com")
        {
            EventId = expectedEventId,
        };

        await handler.Handle(new NotificationMessage<UserRegisteredDomainEvent>(domainEvent), CancellationToken.None);

        Notification notification = await dbContext.Set<Notification>().SingleAsync();
        notification.EventId.Should().Be(expectedEventId);

        ConsumerInboxMessage inbox = await dbContext.ConsumerInboxMessages.SingleAsync();
        inbox.EventId.Should().Be(expectedEventId);
    }

    [Fact]
    public async Task User_EndToEnd_OutboxToRegisteredNotification_ExecutesSuccessfully()
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
            cfg.RegisterServicesFromAssembly(typeof(UserRegisteredNotificationHandler).Assembly);
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
        Guid userId = Guid.NewGuid();
        var domainEvent = new UserRegisteredDomainEvent(userId, "registereduser@example.com");

        // 3. Write outbox message directly to DbContext
        using (IServiceScope scope = serviceProvider.CreateScope())
        {
            LastHourDbContext dbContext = scope.ServiceProvider.GetRequiredService<LastHourDbContext>();
            var outboxMessage = OutboxMessage.FromDomainEvent(domainEvent);
            dbContext.OutboxMessages.Add(outboxMessage);
            await dbContext.SaveChangesAsync();
        }

        // 4. Run OutboxProcessor for a single cycle
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
            inbox.Consumer.Should().Be(UserRegisteredNotificationHandler.ConsumerName);
            inbox.IsCompleted.Should().BeTrue();

            Notification notification = await dbContext.Set<Notification>().SingleAsync();
            notification.EventId.Should().Be(domainEvent.EventId);
            notification.RecipientId.Should().Be(userId);
            notification.Type.Should().Be("UserRegistered");
            notification.Title.Should().Be("Welcome to LastHour");
            notification.Body.Should().Be("Your account has been created successfully. Welcome to LastHour.");
        }
    }

    [Fact]
    public async Task User_EndToEnd_RedeliveryAfterBusinessEffectExists_CompletesInboxWithoutDuplicate()
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
            cfg.RegisterServicesFromAssembly(typeof(UserRegisteredNotificationHandler).Assembly);
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
        Guid userId = Guid.NewGuid();
        var domainEvent = new UserRegisteredDomainEvent(userId, "redelivery@example.com");

        // 3. Pre-populate existing business effect
        using (IServiceScope scope = serviceProvider.CreateScope())
        {
            LastHourDbContext dbContext = scope.ServiceProvider.GetRequiredService<LastHourDbContext>();

            var existingNotification = Notification.Create(
                domainEvent.EventId,
                domainEvent.UserId,
                domainEvent.UserId.ToString("D"),
                "Welcome to LastHour",
                "Your account has been created successfully. Welcome to LastHour.",
                "UserRegistered",
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

    [Fact]
    public async Task UserRegistered_SecurityAudit_ContainsNoSensitiveData()
    {
        var (handler, _, _, dbContext) = CreateSut();
        Guid userId = Guid.NewGuid();
        var domainEvent = new UserRegisteredDomainEvent(userId, "privacyuser@example.com");
        var message = new NotificationMessage<UserRegisteredDomainEvent>(domainEvent);

        await handler.Handle(message, CancellationToken.None);

        Notification notification = await dbContext.Set<Notification>().SingleAsync();
        notification.Title.Should().NotContain("Password");
        notification.Body.Should().NotContain("Password");
        notification.Body.Should().NotContain("privacyuser@example.com"); // Privacy requirement: Email not exposed in body
        notification.Body.Should().NotContain("Hash");
        notification.Body.Should().NotContain("Argon2");
        notification.Body.Should().NotContain("Token");
        notification.Body.Should().NotContain("Secret");
        notification.Recipient.Should().Be(userId.ToString("D"));
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
