using System.Data.Common;
using FluentAssertions;
using LastHour.BuildingBlocks.Application.Contracts;
using LastHour.BuildingBlocks.Infrastructure.Observability.FailureClassification;
using LastHour.BuildingBlocks.Infrastructure.Persistence;
using LastHour.BuildingBlocks.Infrastructure.Persistence.Configuration;
using LastHour.BuildingBlocks.Infrastructure.Persistence.Idempotency;
using LastHour.BuildingBlocks.Infrastructure.Persistence.Outbox;
using LastHour.BuildingBlocks.Infrastructure.Tests.Persistence.TestSupport;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace LastHour.BuildingBlocks.Infrastructure.Tests.Events;

[Collection("Events")]
public class EventProcessingHardeningTests : IDisposable
{
    private readonly DbConnection _connection;
    private readonly DbContextOptions<LastHourDbContext> _dbContextOptions;
    private readonly HardeningTestClock _clock;
    private readonly OutboxMetrics _outboxMetrics;
    private readonly ConsumerIdempotencyMetrics _consumerMetrics;

    public EventProcessingHardeningTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        _dbContextOptions = new DbContextOptionsBuilder<LastHourDbContext>()
            .UseSqlite(_connection)
            .Options;

        _clock = new HardeningTestClock(new DateTime(2026, 8, 20, 10, 0, 0, DateTimeKind.Utc));
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
                typeof(TestAggregate).Assembly,
            });

    private ServiceProvider BuildServiceProvider(OutboxOptions? outboxOptions = null, ConsumerIdempotencyOptions? inboxOptions = null)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IClock>(_clock);
        services.AddSingleton(_outboxMetrics);
        services.AddSingleton(_consumerMetrics);
        services.AddSingleton<IOutboxProcessorTracker, OutboxProcessorTracker>();
        services.AddScoped(_ => CreateDbContext());
        services.AddScoped<IConsumerIdempotencyService, ConsumerIdempotencyService>();
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(TestAggregate).Assembly);
        });

        services.Configure<OutboxOptions>(opts =>
        {
            opts.Enabled = true;
            opts.BatchSize = outboxOptions?.BatchSize ?? 10;
            opts.ProcessingInterval = outboxOptions?.ProcessingInterval ?? TimeSpan.FromMilliseconds(50);
            opts.LeaseDuration = outboxOptions?.LeaseDuration ?? TimeSpan.FromMinutes(2);
            opts.BaseRetryDelay = outboxOptions?.BaseRetryDelay ?? TimeSpan.FromSeconds(5);
            opts.MaximumRetryDelay = outboxOptions?.MaximumRetryDelay ?? TimeSpan.FromMinutes(10);
            opts.MaxRetryCount = outboxOptions?.MaxRetryCount ?? 3;
        });

        services.Configure<ConsumerIdempotencyOptions>(opts =>
        {
            opts.LeaseDuration = inboxOptions?.LeaseDuration ?? TimeSpan.FromMinutes(2);
        });

        services.AddHostedService<OutboxProcessor>();

        return services.BuildServiceProvider();
    }

    [Fact]
    public async Task Outbox_PoisonEvent_ExhaustsRetries_AndTransitionsToAbandoned_WithoutHotLooping()
    {
        // 1. Arrange
        using ServiceProvider provider = BuildServiceProvider(new OutboxOptions
        {
            MaxRetryCount = 3,
            BaseRetryDelay = TimeSpan.FromSeconds(1),
            MaximumRetryDelay = TimeSpan.FromSeconds(10),
            ProcessingInterval = TimeSpan.FromMilliseconds(20),
        });

        Guid messageId;
        using (IServiceScope scope = provider.CreateScope())
        {
            LastHourDbContext db = scope.ServiceProvider.GetRequiredService<LastHourDbContext>();
            var poisonMessage = OutboxMessage.Create(new ThrowingTestMessage("poison-payload"));
            db.OutboxMessages.Add(poisonMessage);
            await db.SaveChangesAsync();
            messageId = poisonMessage.Id;
        }

        var tracker = provider.GetRequiredService<IOutboxProcessorTracker>();
        var processor = new OutboxProcessor(
            provider.GetRequiredService<IServiceScopeFactory>(),
            provider.GetRequiredService<IOptions<OutboxOptions>>(),
            _outboxMetrics,
            tracker,
            _clock,
            NullLogger<OutboxProcessor>.Instance);

        // 2. Cycle 1: Attempt 1
        using (var cts1 = new CancellationTokenSource(TimeSpan.FromMilliseconds(300)))
        {
            await processor.StartAsync(cts1.Token);
            await Task.Delay(100);
            await processor.StopAsync(CancellationToken.None);
        }

        using (IServiceScope scope = provider.CreateScope())
        {
            LastHourDbContext db = scope.ServiceProvider.GetRequiredService<LastHourDbContext>();
            OutboxMessage? msg = await db.OutboxMessages.FindAsync(messageId);
            msg.Should().NotBeNull();
            msg!.RetryCount.Should().Be(1);
            msg.ProcessedOnUtc.Should().BeNull();
            msg.NextAttemptAtUtc.Should().NotBeNull();
            msg.NextAttemptAtUtc.Should().BeAfter(_clock.UtcNow);
        }

        // 3. Advance clock and cycle 2: Attempt 2
        _clock.UtcNow = _clock.UtcNow.AddSeconds(5);
        using (var cts2 = new CancellationTokenSource(TimeSpan.FromMilliseconds(300)))
        {
            await processor.StartAsync(cts2.Token);
            await Task.Delay(100);
            await processor.StopAsync(CancellationToken.None);
        }

        // 4. Advance clock and cycle 3: Attempt 3 (Final attempt, reaches MaxRetryCount=3)
        _clock.UtcNow = _clock.UtcNow.AddSeconds(10);
        using (var cts3 = new CancellationTokenSource(TimeSpan.FromMilliseconds(300)))
        {
            await processor.StartAsync(cts3.Token);
            await Task.Delay(100);
            await processor.StopAsync(CancellationToken.None);
        }

        // 5. Assert: Message is now abandoned
        using (IServiceScope scope = provider.CreateScope())
        {
            LastHourDbContext db = scope.ServiceProvider.GetRequiredService<LastHourDbContext>();
            OutboxMessage? msg = await db.OutboxMessages.FindAsync(messageId);
            msg.Should().NotBeNull();
            msg!.RetryCount.Should().Be(3);
            msg.ProcessedOnUtc.Should().NotBeNull("Message must be marked processed (abandoned) upon reaching MaxRetryCount");
            msg.NextAttemptAtUtc.Should().BeNull("Abandoned message must not have NextAttemptAtUtc to prevent hot looping");
            msg.Error.Should().NotBeNull();
            msg.CanRequeue.Should().BeTrue();

            // Requeue test
            msg.Requeue(_clock.UtcNow);
            msg.RetryCount.Should().Be(0);
            msg.ProcessedOnUtc.Should().BeNull();
            msg.Error.Should().BeNull();
            msg.NextAttemptAtUtc.Should().Be(_clock.UtcNow);
        }
    }

    [Fact]
    public void Outbox_ExponentialBackoff_CalculatesDeterministicBoundedDelays()
    {
        TimeSpan baseDelay = TimeSpan.FromSeconds(2);
        TimeSpan maxDelay = TimeSpan.FromSeconds(60);

        OutboxProcessor.CalculateRetryDelay(baseDelay, maxDelay, 0).Should().Be(TimeSpan.FromSeconds(2));
        OutboxProcessor.CalculateRetryDelay(baseDelay, maxDelay, 1).Should().Be(TimeSpan.FromSeconds(4));
        OutboxProcessor.CalculateRetryDelay(baseDelay, maxDelay, 2).Should().Be(TimeSpan.FromSeconds(8));
        OutboxProcessor.CalculateRetryDelay(baseDelay, maxDelay, 3).Should().Be(TimeSpan.FromSeconds(16));
        OutboxProcessor.CalculateRetryDelay(baseDelay, maxDelay, 4).Should().Be(TimeSpan.FromSeconds(32));
        OutboxProcessor.CalculateRetryDelay(baseDelay, maxDelay, 5).Should().Be(TimeSpan.FromSeconds(60)); // Capped at maxDelay
        OutboxProcessor.CalculateRetryDelay(baseDelay, maxDelay, 20).Should().Be(TimeSpan.FromSeconds(60)); // Overflow protected
    }

    [Fact]
    public async Task ConsumerInbox_LeaseExpiration_AllowsSafeReclaim_WithoutDuplicateExecution()
    {
        Guid eventId = Guid.NewGuid();
        const string consumerName = "TestModule.TestOperation";

        // 1. Worker 1 acquires lease for 2 minutes
        using (LastHourDbContext db1 = CreateDbContext())
        {
            var idempotency1 = new ConsumerIdempotencyService(
                db1,
                _clock,
                _consumerMetrics,
                Microsoft.Extensions.Options.Options.Create(new ConsumerIdempotencyOptions { LeaseDuration = TimeSpan.FromMinutes(2) }),
                NullLogger<ConsumerIdempotencyService>.Instance);

            bool acquired = await idempotency1.TryAcquireAsync(eventId, consumerName);
            acquired.Should().BeTrue();
        }

        // 2. Active lease cannot be stolen before expiration
        _clock.UtcNow = _clock.UtcNow.AddMinutes(1); // 1 minute elapsed (lease still active for 1 more min)
        using (LastHourDbContext db2 = CreateDbContext())
        {
            var idempotency2 = new ConsumerIdempotencyService(
                db2,
                _clock,
                _consumerMetrics,
                Microsoft.Extensions.Options.Options.Create(new ConsumerIdempotencyOptions { LeaseDuration = TimeSpan.FromMinutes(2) }),
                NullLogger<ConsumerIdempotencyService>.Instance);

            bool stolen = await idempotency2.TryAcquireAsync(eventId, consumerName);
            stolen.Should().BeFalse("Active lease must not be acquired by another worker");
        }

        // 3. Advance past lease expiration (3 minutes elapsed) -> worker 3 reclaims lease
        _clock.UtcNow = _clock.UtcNow.AddMinutes(2);
        using (LastHourDbContext db3 = CreateDbContext())
        {
            var idempotency3 = new ConsumerIdempotencyService(
                db3,
                _clock,
                _consumerMetrics,
                Microsoft.Extensions.Options.Options.Create(new ConsumerIdempotencyOptions { LeaseDuration = TimeSpan.FromMinutes(2) }),
                NullLogger<ConsumerIdempotencyService>.Instance);

            bool reclaimed = await idempotency3.TryAcquireAsync(eventId, consumerName);
            reclaimed.Should().BeTrue("Expired lease must be reclaimable");

            await idempotency3.CompleteAsync(eventId, consumerName);
        }

        // 4. Verify inbox is completed
        using (LastHourDbContext verifyDb = CreateDbContext())
        {
            ConsumerInboxMessage inbox = await verifyDb.ConsumerInboxMessages
                .SingleAsync(m => m.EventId == eventId && m.Consumer == consumerName);
            inbox.IsCompleted.Should().BeTrue();
        }
    }

    [Fact]
    public void FailureClassification_ClassifiesExceptionsDeterministically_WithLowCardinality()
    {
        FailureClassifier.Classify(new OperationCanceledException()).Should().Be(FailureClassifier.Cancellation);
        FailureClassifier.Classify(new TaskCanceledException()).Should().Be(FailureClassifier.Cancellation);
        FailureClassifier.Classify(new DbUpdateConcurrencyException("Concurrency mismatch")).Should().Be(FailureClassifier.Concurrency);
        FailureClassifier.Classify(new DbUpdateException("DB write failed")).Should().Be(FailureClassifier.Database);
        FailureClassifier.Classify(new TimeoutException("Operation timed out")).Should().Be(FailureClassifier.Transient);
        FailureClassifier.Classify(new ArgumentException("Invalid input")).Should().Be(FailureClassifier.Validation);
        FailureClassifier.Classify(new HttpRequestException("Gateway timeout")).Should().Be(FailureClassifier.Infrastructure);
        FailureClassifier.Classify(new InvalidOperationException("Generic error")).Should().Be(FailureClassifier.Unknown);
        FailureClassifier.Classify(null).Should().Be(FailureClassifier.Unknown);
    }

    [Fact]
    public async Task Outbox_SuccessfulProcessing_MarksCompletedAndEmitsMetrics()
    {
        TestSink.Reset();
        using ServiceProvider provider = BuildServiceProvider();

        var domainEvent = new TestDomainEvent("sample-data");
        using (IServiceScope scope = provider.CreateScope())
        {
            LastHourDbContext db = scope.ServiceProvider.GetRequiredService<LastHourDbContext>();
            db.OutboxMessages.Add(OutboxMessage.FromDomainEvent(domainEvent));
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

        TestSink.Messages.Should().ContainSingle();

        using (IServiceScope scope = provider.CreateScope())
        {
            LastHourDbContext db = scope.ServiceProvider.GetRequiredService<LastHourDbContext>();
            OutboxMessage msg = await db.OutboxMessages.SingleAsync();
            msg.ProcessedOnUtc.Should().NotBeNull();
            msg.Error.Should().BeNull();
        }
    }

    private sealed class HardeningTestClock : IClock
    {
        public HardeningTestClock(DateTime utcNow)
        {
            UtcNow = utcNow;
        }

        public DateTime UtcNow { get; set; }
    }
}
