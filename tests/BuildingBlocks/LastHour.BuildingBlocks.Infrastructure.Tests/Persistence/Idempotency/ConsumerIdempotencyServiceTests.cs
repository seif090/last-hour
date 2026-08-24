using System.Diagnostics.Metrics;
using FluentAssertions;
using LastHour.BuildingBlocks.Application.Contracts;
using LastHour.BuildingBlocks.Infrastructure.Persistence;
using LastHour.BuildingBlocks.Infrastructure.Persistence.Idempotency;
using LastHour.BuildingBlocks.Infrastructure.Tests.Persistence.TestSupport;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace LastHour.BuildingBlocks.Infrastructure.Tests.Persistence.Idempotency;

[Collection("Events")]
public class ConsumerIdempotencyServiceTests
{
    private readonly DateTime _baseTime = new DateTime(2026, 8, 19, 10, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task FirstDelivery_IsProcessed()
    {
        var clock = new FixedClock { UtcNow = _baseTime };
        (ServiceProvider provider, SqliteConnection connection) = BuildSqliteProvider(clock);
        using (connection)
        using (provider)
        {
            var service = provider.GetRequiredService<IConsumerIdempotencyService>();
            Guid eventId = Guid.NewGuid();
            const string consumer = "Notifications.WelcomeEmail";
            bool handlerExecuted = false;

            bool result = await service.ExecuteAsync(
                eventId,
                consumer,
                _ =>
                {
                    handlerExecuted = true;
                    return Task.CompletedTask;
                });

            result.Should().BeTrue();
            handlerExecuted.Should().BeTrue();

            using IServiceScope scope = provider.CreateScope();
            LastHourDbContext dbContext = scope.ServiceProvider.GetRequiredService<LastHourDbContext>();
            ConsumerInboxMessage record = await dbContext.ConsumerInboxMessages.SingleAsync();

            record.EventId.Should().Be(eventId);
            record.Consumer.Should().Be(consumer);
            record.StartedOnUtc.Should().Be(_baseTime);
            record.CompletedOnUtc.Should().Be(_baseTime);
            record.IsCompleted.Should().BeTrue();
            record.LockExpiresAtUtc.Should().BeNull();
        }
    }

    [Fact]
    public async Task DuplicateDelivery_IsSkipped()
    {
        var clock = new FixedClock { UtcNow = _baseTime };
        (ServiceProvider provider, SqliteConnection connection) = BuildSqliteProvider(clock);
        using (connection)
        using (provider)
        {
            var service = provider.GetRequiredService<IConsumerIdempotencyService>();
            Guid eventId = Guid.NewGuid();
            const string consumer = "Notifications.WelcomeEmail";
            int executionCount = 0;

            bool firstResult = await service.ExecuteAsync(eventId, consumer, _ => { executionCount++; return Task.CompletedTask; });
            bool secondResult = await service.ExecuteAsync(eventId, consumer, _ => { executionCount++; return Task.CompletedTask; });

            firstResult.Should().BeTrue();
            secondResult.Should().BeFalse();
            executionCount.Should().Be(1);
        }
    }

    [Fact]
    public async Task SameEvent_DifferentConsumers_AreIndependent()
    {
        var clock = new FixedClock { UtcNow = _baseTime };
        (ServiceProvider provider, SqliteConnection connection) = BuildSqliteProvider(clock);
        using (connection)
        using (provider)
        {
            var service = provider.GetRequiredService<IConsumerIdempotencyService>();
            Guid eventId = Guid.NewGuid();
            bool notificationsExecuted = false;
            bool analyticsExecuted = false;

            bool res1 = await service.ExecuteAsync(eventId, "Notifications.WelcomeEmail", _ => { notificationsExecuted = true; return Task.CompletedTask; });
            bool res2 = await service.ExecuteAsync(eventId, "Analytics.MerchantRegistration", _ => { analyticsExecuted = true; return Task.CompletedTask; });

            res1.Should().BeTrue();
            res2.Should().BeTrue();
            notificationsExecuted.Should().BeTrue();
            analyticsExecuted.Should().BeTrue();

            using IServiceScope scope = provider.CreateScope();
            LastHourDbContext dbContext = scope.ServiceProvider.GetRequiredService<LastHourDbContext>();
            List<ConsumerInboxMessage> records = await dbContext.ConsumerInboxMessages.OrderBy(m => m.Consumer).ToListAsync();

            records.Should().HaveCount(2);
            records[0].Consumer.Should().Be("Analytics.MerchantRegistration");
            records[0].IsCompleted.Should().BeTrue();
            records[1].Consumer.Should().Be("Notifications.WelcomeEmail");
            records[1].IsCompleted.Should().BeTrue();
        }
    }

    [Fact]
    public async Task HandlerFailure_DoesNotMarkCompleted_AndAllowsRetry()
    {
        var clock = new FixedClock { UtcNow = _baseTime };
        (ServiceProvider provider, SqliteConnection connection) = BuildSqliteProvider(clock);
        using (connection)
        using (provider)
        {
            var service = provider.GetRequiredService<IConsumerIdempotencyService>();
            Guid eventId = Guid.NewGuid();
            const string consumer = "Payments.Capture";

            Func<Task> failedAttempt = async () =>
            {
                await service.ExecuteAsync(
                    eventId,
                    consumer,
                    _ => throw new InvalidOperationException("Payment gateway timeout"));
            };

            await failedAttempt.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("Payment gateway timeout");

            using (IServiceScope verifyScope = provider.CreateScope())
            {
                LastHourDbContext dbContext = verifyScope.ServiceProvider.GetRequiredService<LastHourDbContext>();
                ConsumerInboxMessage? record = await dbContext.ConsumerInboxMessages
                    .FirstOrDefaultAsync(m => m.EventId == eventId && m.Consumer == consumer);
                record.Should().BeNull("Uncompleted record should be released after failure.");
            }

            // Retry attempt
            clock.UtcNow = _baseTime.AddSeconds(10);
            bool retryExecuted = false;
            bool retryResult = await service.ExecuteAsync(
                eventId,
                consumer,
                _ => { retryExecuted = true; return Task.CompletedTask; });

            retryResult.Should().BeTrue();
            retryExecuted.Should().BeTrue();

            using (IServiceScope finalScope = provider.CreateScope())
            {
                LastHourDbContext dbContext = finalScope.ServiceProvider.GetRequiredService<LastHourDbContext>();
                ConsumerInboxMessage record = await dbContext.ConsumerInboxMessages.SingleAsync(m => m.EventId == eventId);
                record.IsCompleted.Should().BeTrue();
                record.CompletedOnUtc.Should().Be(_baseTime.AddSeconds(10));
            }
        }
    }

    [Fact]
    public async Task CompletedEvent_CannotBeProcessedAgain()
    {
        var clock = new FixedClock { UtcNow = _baseTime };
        (ServiceProvider provider, SqliteConnection connection) = BuildSqliteProvider(clock);
        using (connection)
        using (provider)
        {
            var service = provider.GetRequiredService<IConsumerIdempotencyService>();
            Guid eventId = Guid.NewGuid();
            const string consumer = "Inventory.Reservation";

            await service.ExecuteAsync(eventId, consumer, _ => Task.CompletedTask);

            bool acquireResult = await service.TryAcquireAsync(eventId, consumer);
            acquireResult.Should().BeFalse();
        }
    }

    [Fact]
    public async Task CancellationDuringHandler_DoesNotMarkCompleted_AndPropagates()
    {
        var clock = new FixedClock { UtcNow = _baseTime };
        (ServiceProvider provider, SqliteConnection connection) = BuildSqliteProvider(clock);
        using (connection)
        using (provider)
        {
            var service = provider.GetRequiredService<IConsumerIdempotencyService>();
            Guid eventId = Guid.NewGuid();
            const string consumer = "Notifications.Email";

            using var cts = new CancellationTokenSource();

            Func<Task> cancelledAction = async () =>
            {
                await service.ExecuteAsync(
                    eventId,
                    consumer,
                    async ct =>
                    {
                        cts.Cancel();
                        await Task.Delay(10, ct);
                        ct.ThrowIfCancellationRequested();
                    },
                    cts.Token);
            };

            await cancelledAction.Should().ThrowAsync<OperationCanceledException>();

            using IServiceScope scope = provider.CreateScope();
            LastHourDbContext dbContext = scope.ServiceProvider.GetRequiredService<LastHourDbContext>();
            ConsumerInboxMessage? record = await dbContext.ConsumerInboxMessages
                .FirstOrDefaultAsync(m => m.EventId == eventId && m.Consumer == consumer);

            record.Should().BeNull("Uncompleted record should be released on cancellation.");
        }
    }

    [Fact]
    public async Task InternalCancellation_WhenStoppingTokenNotCancelled_IsTreatedAsRealFailure()
    {
        var clock = new FixedClock { UtcNow = _baseTime };
        (ServiceProvider provider, SqliteConnection connection) = BuildSqliteProvider(clock);
        using (connection)
        using (provider)
        {
            var service = provider.GetRequiredService<IConsumerIdempotencyService>();
            Guid eventId = Guid.NewGuid();
            const string consumer = "Catalog.Sync";

            using var callerCts = new CancellationTokenSource();

            Func<Task> action = async () =>
            {
                await service.ExecuteAsync(
                    eventId,
                    consumer,
                    _ =>
                    {
                        using var internalCts = new CancellationTokenSource();
                        internalCts.Cancel();
                        throw new OperationCanceledException("Internal timeout", internalCts.Token);
                    },
                    callerCts.Token);
            };

            await action.Should().ThrowAsync<OperationCanceledException>()
                .WithMessage("Internal timeout*");

            using IServiceScope scope = provider.CreateScope();
            LastHourDbContext dbContext = scope.ServiceProvider.GetRequiredService<LastHourDbContext>();
            ConsumerInboxMessage? record = await dbContext.ConsumerInboxMessages
                .FirstOrDefaultAsync(m => m.EventId == eventId && m.Consumer == consumer);

            record.Should().BeNull("Record should be released so the event remains retryable.");
        }
    }

    [Fact]
    public async Task ExpiredLease_FromCrashedWorker_IsReclaimedAndCompleted()
    {
        var clock = new FixedClock { UtcNow = _baseTime };
        (ServiceProvider provider, SqliteConnection connection) = BuildSqliteProvider(clock);
        using (connection)
        using (provider)
        {
            Guid eventId = Guid.NewGuid();
            const string consumer = "Merchant.Webhook";

            using (IServiceScope initScope = provider.CreateScope())
            {
                LastHourDbContext initContext = initScope.ServiceProvider.GetRequiredService<LastHourDbContext>();
                var staleMessage = ConsumerInboxMessage.Create(eventId, consumer, _baseTime.AddMinutes(-10), TimeSpan.FromMinutes(2));
                initContext.ConsumerInboxMessages.Add(staleMessage);
                await initContext.SaveChangesAsync();
            }

            var service = provider.GetRequiredService<IConsumerIdempotencyService>();
            bool handlerExecuted = false;

            bool result = await service.ExecuteAsync(
                eventId,
                consumer,
                _ =>
                {
                    handlerExecuted = true;
                    return Task.CompletedTask;
                });

            result.Should().BeTrue();
            handlerExecuted.Should().BeTrue();

            using (IServiceScope verifyScope = provider.CreateScope())
            {
                LastHourDbContext verifyContext = verifyScope.ServiceProvider.GetRequiredService<LastHourDbContext>();
                ConsumerInboxMessage record = await verifyContext.ConsumerInboxMessages.SingleAsync(m => m.EventId == eventId);
                record.IsCompleted.Should().BeTrue();
                record.CompletedOnUtc.Should().Be(_baseTime);
                record.LockExpiresAtUtc.Should().BeNull();
            }
        }
    }

    [Fact]
    public async Task ActiveLease_DuplicateDoesNotExecuteHandler()
    {
        var clock = new FixedClock { UtcNow = _baseTime };
        (ServiceProvider provider, SqliteConnection connection) = BuildSqliteProvider(clock);
        using (connection)
        using (provider)
        {
            Guid eventId = Guid.NewGuid();
            const string consumer = "Merchant.Webhook";

            using (IServiceScope initScope = provider.CreateScope())
            {
                LastHourDbContext initContext = initScope.ServiceProvider.GetRequiredService<LastHourDbContext>();
                var activeMessage = ConsumerInboxMessage.Create(eventId, consumer, _baseTime, TimeSpan.FromMinutes(2));
                initContext.ConsumerInboxMessages.Add(activeMessage);
                await initContext.SaveChangesAsync();
            }

            var service = provider.GetRequiredService<IConsumerIdempotencyService>();
            bool handlerExecuted = false;

            bool result = await service.ExecuteAsync(
                eventId,
                consumer,
                _ =>
                {
                    handlerExecuted = true;
                    return Task.CompletedTask;
                });

            result.Should().BeFalse();
            handlerExecuted.Should().BeFalse();
        }
    }

    [Fact]
    public async Task ConcurrentSameEventAndConsumer_AllowsOnlyOneWinner()
    {
        var clock = new FixedClock { UtcNow = _baseTime };
        (ServiceProvider provider, SqliteConnection connection) = BuildSqliteProvider(clock);
        using (connection)
        using (provider)
        {
            Guid eventId = Guid.NewGuid();
            const string consumer = "Ordering.OrderPlaced";
            int totalExecutions = 0;

            Task<bool> task1 = Task.Run(async () =>
            {
                using IServiceScope scope1 = provider.CreateScope();
                var service1 = scope1.ServiceProvider.GetRequiredService<IConsumerIdempotencyService>();
                return await service1.ExecuteAsync(
                    eventId,
                    consumer,
                    async ct =>
                    {
                        Interlocked.Increment(ref totalExecutions);
                        await Task.Delay(25, ct);
                    });
            });

            Task<bool> task2 = Task.Run(async () =>
            {
                using IServiceScope scope2 = provider.CreateScope();
                var service2 = scope2.ServiceProvider.GetRequiredService<IConsumerIdempotencyService>();
                return await service2.ExecuteAsync(
                    eventId,
                    consumer,
                    async ct =>
                    {
                        Interlocked.Increment(ref totalExecutions);
                        await Task.Delay(25, ct);
                    });
            });

            bool[] results = await Task.WhenAll(task1, task2);

            results.Count(r => r).Should().Be(1);
            results.Count(r => !r).Should().Be(1);
            totalExecutions.Should().Be(1);
        }
    }

    [Fact]
    public async Task DomainEventOverload_UsesEventId()
    {
        var clock = new FixedClock { UtcNow = _baseTime };
        (ServiceProvider provider, SqliteConnection connection) = BuildSqliteProvider(clock);
        using (connection)
        using (provider)
        {
            var service = provider.GetRequiredService<IConsumerIdempotencyService>();
            var domainEvent = new TestDomainEvent("sample-data");
            const string consumer = "Audit.Log";

            bool result = await service.ExecuteAsync(domainEvent, consumer, _ => Task.CompletedTask);
            result.Should().BeTrue();

            using IServiceScope scope = provider.CreateScope();
            LastHourDbContext dbContext = scope.ServiceProvider.GetRequiredService<LastHourDbContext>();
            ConsumerInboxMessage record = await dbContext.ConsumerInboxMessages.SingleAsync();
            record.EventId.Should().Be(domainEvent.EventId);
        }
    }

    [Fact]
    public async Task EmptyOrInvalidParameters_AreRejected()
    {
        var clock = new FixedClock { UtcNow = _baseTime };
        (ServiceProvider provider, SqliteConnection connection) = BuildSqliteProvider(clock);
        using (connection)
        using (provider)
        {
            var service = provider.GetRequiredService<IConsumerIdempotencyService>();

            Func<Task> emptyEventId = () => service.ExecuteAsync(Guid.Empty, "consumer", _ => Task.CompletedTask);
            await emptyEventId.Should().ThrowAsync<ArgumentException>();

            Func<Task> emptyConsumer = () => service.ExecuteAsync(Guid.NewGuid(), "   ", _ => Task.CompletedTask);
            await emptyConsumer.Should().ThrowAsync<ArgumentException>();

            string longConsumer = new string('x', 257);
            Func<Task> longConsumerAction = () => service.ExecuteAsync(Guid.NewGuid(), longConsumer, _ => Task.CompletedTask);
            await longConsumerAction.Should().ThrowAsync<ArgumentException>();
        }
    }

    [Fact]
    public async Task Metrics_AreEmittedCorrectly()
    {
        var clock = new FixedClock { UtcNow = _baseTime };
        (ServiceProvider provider, SqliteConnection connection) = BuildSqliteProvider(clock);
        using (connection)
        using (provider)
        {
            var service = provider.GetRequiredService<IConsumerIdempotencyService>();
            Guid eventId = Guid.NewGuid();
            const string consumer = "MetricsTest.Consumer";

            long processedCount = 0;
            long duplicateCount = 0;
            long failedCount = 0;

            using var listener = new MeterListener();
            listener.InstrumentPublished = (instrument, meterListener) =>
            {
                if (instrument.Meter.Name == ConsumerIdempotencyMetrics.MeterName)
                {
                    meterListener.EnableMeasurementEvents(instrument);
                }
            };

            listener.SetMeasurementEventCallback<long>((instrument, measurement, _, _) =>
            {
                if (instrument.Name == "last_hour.consumer.processed")
                {
                    processedCount += measurement;
                }

                if (instrument.Name == "last_hour.consumer.duplicate")
                {
                    duplicateCount += measurement;
                }

                if (instrument.Name == "last_hour.consumer.failed")
                {
                    failedCount += measurement;
                }
            });

            listener.Start();

            // 1. Success
            await service.ExecuteAsync(eventId, consumer, _ => Task.CompletedTask);
            processedCount.Should().Be(1);

            // 2. Duplicate
            await service.ExecuteAsync(eventId, consumer, _ => Task.CompletedTask);
            duplicateCount.Should().Be(1);

            // 3. Failure on another event
            Guid failedEventId = Guid.NewGuid();
            Func<Task> failAction = () => service.ExecuteAsync(failedEventId, consumer, _ => throw new InvalidOperationException("boom"));
            await failAction.Should().ThrowAsync<InvalidOperationException>();
            failedCount.Should().Be(1);
        }
    }

    [Fact]
    public async Task CancellationDuringBegin_DoesNotCreateCompletedState()
    {
        var clock = new FixedClock { UtcNow = _baseTime };
        (ServiceProvider provider, SqliteConnection connection) = BuildSqliteProvider(clock);
        using (connection)
        using (provider)
        {
            var service = provider.GetRequiredService<IConsumerIdempotencyService>();
            Guid eventId = Guid.NewGuid();
            const string consumer = "Orders.CancellationTest";

            using var cts = new CancellationTokenSource();
            cts.Cancel(); // pre-cancelled

            Func<Task> act = () => service.ExecuteAsync(eventId, consumer, _ => Task.CompletedTask, cts.Token);
            await act.Should().ThrowAsync<OperationCanceledException>();

            using IServiceScope scope = provider.CreateScope();
            LastHourDbContext dbContext = scope.ServiceProvider.GetRequiredService<LastHourDbContext>();
            ConsumerInboxMessage? record = await dbContext.ConsumerInboxMessages
                .FirstOrDefaultAsync(m => m.EventId == eventId && m.Consumer == consumer);

            record.Should().BeNull();
        }
    }

    [Fact]
    public void DatabaseModel_EnforcesUniqueCompositeIndexOnEventIdAndConsumer()
    {
        var options = new DbContextOptionsBuilder<LastHourDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        using var dbContext = new LastHourDbContext(options);
        var entityType = dbContext.Model.FindEntityType(typeof(ConsumerInboxMessage));
        entityType.Should().NotBeNull();

        var index = entityType!.FindIndex(new[]
        {
            entityType.FindProperty(nameof(ConsumerInboxMessage.EventId))!,
            entityType.FindProperty(nameof(ConsumerInboxMessage.Consumer))!,
        });

        index.Should().NotBeNull();
        index!.IsUnique.Should().BeTrue();
        index.GetDatabaseName().Should().Be("IX_consumer_inbox_messages_event_id_consumer");
    }

    [Fact]
    public async Task CompletedRecordCannotBeReclaimed_EvenIfClockAdvancesFarInFuture()
    {
        var clock = new FixedClock { UtcNow = _baseTime };
        (ServiceProvider provider, SqliteConnection connection) = BuildSqliteProvider(clock);
        using (connection)
        using (provider)
        {
            var service = provider.GetRequiredService<IConsumerIdempotencyService>();
            Guid eventId = Guid.NewGuid();
            const string consumer = "Loyalty.Points";

            await service.ExecuteAsync(eventId, consumer, _ => Task.CompletedTask);

            // Advance clock by 30 days
            clock.UtcNow = _baseTime.AddDays(30);

            bool acquireResult = await service.TryAcquireAsync(eventId, consumer);
            acquireResult.Should().BeFalse("A completed record should never expire or become reclaimable.");
        }
    }

    [Fact]
    public async Task ExpiredLeaseReclaimUsesCurrentClock()
    {
        var clock = new FixedClock { UtcNow = _baseTime };
        (ServiceProvider provider, SqliteConnection connection) = BuildSqliteProvider(clock);
        using (connection)
        using (provider)
        {
            Guid eventId = Guid.NewGuid();
            const string consumer = "Inventory.Deplete";

            using (IServiceScope initScope = provider.CreateScope())
            {
                LastHourDbContext initContext = initScope.ServiceProvider.GetRequiredService<LastHourDbContext>();
                var stale = ConsumerInboxMessage.Create(eventId, consumer, _baseTime.AddHours(-1), TimeSpan.FromMinutes(2));
                initContext.ConsumerInboxMessages.Add(stale);
                await initContext.SaveChangesAsync();
            }

            DateTime reclaimTime = _baseTime.AddHours(2);
            clock.UtcNow = reclaimTime;

            var service = provider.GetRequiredService<IConsumerIdempotencyService>();
            bool result = await service.ExecuteAsync(eventId, consumer, _ => Task.CompletedTask);

            result.Should().BeTrue();

            using (IServiceScope verifyScope = provider.CreateScope())
            {
                LastHourDbContext verifyContext = verifyScope.ServiceProvider.GetRequiredService<LastHourDbContext>();
                ConsumerInboxMessage record = await verifyContext.ConsumerInboxMessages.SingleAsync(m => m.EventId == eventId);
                record.IsCompleted.Should().BeTrue();
                record.CompletedOnUtc.Should().Be(reclaimTime);
            }
        }
    }

    [Fact]
    public async Task ConcurrentDifferentConsumers_DoNotBlockEachOther()
    {
        var clock = new FixedClock { UtcNow = _baseTime };
        (ServiceProvider provider, SqliteConnection connection) = BuildSqliteProvider(clock);
        using (connection)
        using (provider)
        {
            Guid eventId = Guid.NewGuid();
            int executions = 0;

            Task<bool> task1 = Task.Run(async () =>
            {
                using IServiceScope scope1 = provider.CreateScope();
                var s1 = scope1.ServiceProvider.GetRequiredService<IConsumerIdempotencyService>();
                return await s1.ExecuteAsync(eventId, "Consumer.A", async ct =>
                {
                    Interlocked.Increment(ref executions);
                    await Task.Delay(15, ct);
                });
            });

            Task<bool> task2 = Task.Run(async () =>
            {
                using IServiceScope scope2 = provider.CreateScope();
                var s2 = scope2.ServiceProvider.GetRequiredService<IConsumerIdempotencyService>();
                return await s2.ExecuteAsync(eventId, "Consumer.B", async ct =>
                {
                    Interlocked.Increment(ref executions);
                    await Task.Delay(15, ct);
                });
            });

            bool[] results = await Task.WhenAll(task1, task2);

            results.Should().AllBeEquivalentTo(true);
            executions.Should().Be(2);
        }
    }

    [Fact]
    public async Task SlowWorkerA_ExceedsLease_WorkerBReclaims_WorkerACompletionFailsWithConcurrencyException_WorkerBCompletes()
    {
        var clock = new FixedClock { UtcNow = _baseTime };
        (ServiceProvider provider, SqliteConnection connection) = BuildSqliteProvider(clock);
        using (connection)
        using (provider)
        {
            Guid eventId = Guid.NewGuid();
            const string consumer = "Orders.Fulfillment";

            // 1. Worker A acquires lease at T0 (expires at T0 + 2 min)
            using IServiceScope scopeA = provider.CreateScope();
            var serviceA = scopeA.ServiceProvider.GetRequiredService<IConsumerIdempotencyService>();
            bool acquiredA = await serviceA.TryAcquireAsync(eventId, consumer);
            acquiredA.Should().BeTrue();

            // 2. Time passes past Worker A's lease (advance by 3 minutes)
            clock.UtcNow = _baseTime.AddMinutes(3);

            // 3. Worker B detects expired lease and reclaims (new lease until T0 + 5 min)
            using IServiceScope scopeB = provider.CreateScope();
            var serviceB = scopeB.ServiceProvider.GetRequiredService<IConsumerIdempotencyService>();
            bool acquiredB = await serviceB.TryAcquireAsync(eventId, consumer);
            acquiredB.Should().BeTrue();

            // 4. Stale Worker A finishes and attempts CompleteAsync
            // Because Worker B renewed the lease (changing LockExpiresAtUtc), Worker A's concurrency token fails!
            Func<Task> staleWorkerAComplete = () => serviceA.CompleteAsync(eventId, consumer);
            await staleWorkerAComplete.Should().ThrowAsync<DbUpdateConcurrencyException>();

            // 5. Active Worker B completes successfully
            Func<Task> activeWorkerBComplete = () => serviceB.CompleteAsync(eventId, consumer);
            await activeWorkerBComplete.Should().NotThrowAsync();

            // 6. Verification: Message is completed and owned by Worker B
            using IServiceScope verifyScope = provider.CreateScope();
            LastHourDbContext verifyContext = verifyScope.ServiceProvider.GetRequiredService<LastHourDbContext>();
            ConsumerInboxMessage record = await verifyContext.ConsumerInboxMessages.SingleAsync(m => m.EventId == eventId);
            record.IsCompleted.Should().BeTrue();
            record.CompletedOnUtc.Should().Be(_baseTime.AddMinutes(3));
        }
    }

    [Fact]
    public async Task SlowWorkerA_ExceedsLease_WorkerBReclaims_WorkerAFailureRelease_DoesNotDeleteWorkerBActiveLease()
    {
        var clock = new FixedClock { UtcNow = _baseTime };
        (ServiceProvider provider, SqliteConnection connection) = BuildSqliteProvider(clock);
        using (connection)
        using (provider)
        {
            Guid eventId = Guid.NewGuid();
            const string consumer = "Orders.Fulfillment";

            // 1. Worker A acquires lease at T0
            using IServiceScope scopeA = provider.CreateScope();
            var serviceA = scopeA.ServiceProvider.GetRequiredService<IConsumerIdempotencyService>();
            bool acquiredA = await serviceA.TryAcquireAsync(eventId, consumer);
            acquiredA.Should().BeTrue();

            // 2. Time passes past Worker A's lease (advance by 3 minutes)
            clock.UtcNow = _baseTime.AddMinutes(3);

            // 3. Worker B reclaims lease at T0 + 3 min
            using IServiceScope scopeB = provider.CreateScope();
            var serviceB = scopeB.ServiceProvider.GetRequiredService<IConsumerIdempotencyService>();
            bool acquiredB = await serviceB.TryAcquireAsync(eventId, consumer);
            acquiredB.Should().BeTrue();

            // 4. Stale Worker A fails its handler and attempts ReleaseAsync
            // ReleaseAsync fails with DbUpdateConcurrencyException because LockExpiresAtUtc was mutated by Worker B
            Func<Task> staleWorkerARelease = () => serviceA.ReleaseAsync(eventId, consumer);
            await staleWorkerARelease.Should().ThrowAsync<DbUpdateConcurrencyException>();

            // 5. Worker B's active lease is NOT deleted
            using (IServiceScope verifyScope = provider.CreateScope())
            {
                LastHourDbContext verifyContext = verifyScope.ServiceProvider.GetRequiredService<LastHourDbContext>();
                ConsumerInboxMessage activeRecord = await verifyContext.ConsumerInboxMessages.SingleAsync(m => m.EventId == eventId);
                activeRecord.IsCompleted.Should().BeFalse();
                activeRecord.LockExpiresAtUtc.Should().Be(_baseTime.AddMinutes(5));
            }

            // 6. Worker B completes its work successfully
            await serviceB.CompleteAsync(eventId, consumer);

            using (IServiceScope verifyScope = provider.CreateScope())
            {
                LastHourDbContext verifyContext = verifyScope.ServiceProvider.GetRequiredService<LastHourDbContext>();
                ConsumerInboxMessage completedRecord = await verifyContext.ConsumerInboxMessages.SingleAsync(m => m.EventId == eventId);
                completedRecord.IsCompleted.Should().BeTrue();
            }
        }
    }

    [Fact]
    public async Task ConcurrentExpiredLeaseReclaim_AllowsOnlyOneWorkerToReclaim()
    {
        var clock = new FixedClock { UtcNow = _baseTime };
        (ServiceProvider provider, SqliteConnection connection) = BuildSqliteProvider(clock);
        using (connection)
        using (provider)
        {
            Guid eventId = Guid.NewGuid();
            const string consumer = "Reclaim.Race";

            // Pre-seed an expired in-progress lease
            using (IServiceScope seedScope = provider.CreateScope())
            {
                LastHourDbContext seedContext = seedScope.ServiceProvider.GetRequiredService<LastHourDbContext>();
                var expiredMessage = ConsumerInboxMessage.Create(eventId, consumer, _baseTime.AddMinutes(-10), TimeSpan.FromMinutes(2));
                seedContext.ConsumerInboxMessages.Add(expiredMessage);
                await seedContext.SaveChangesAsync();
            }

            // 10 concurrent workers try to reclaim the expired lease
            Task<bool>[] reclaimTasks = Enumerable.Range(0, 10).Select(_ => Task.Run(async () =>
            {
                using IServiceScope scope = provider.CreateScope();
                var service = scope.ServiceProvider.GetRequiredService<IConsumerIdempotencyService>();
                return await service.TryAcquireAsync(eventId, consumer);
            })).ToArray();

            bool[] results = await Task.WhenAll(reclaimTasks);

            // Exactly 1 winner reclaims; 9 losers receive false via concurrency control
            results.Count(r => r).Should().Be(1);
            results.Count(r => !r).Should().Be(9);
        }
    }

    private static (ServiceProvider Provider, SqliteConnection Connection) BuildSqliteProvider(IClock clock)
    {
        string dbName = "memdb_" + Guid.NewGuid().ToString("N");
        string connectionString = $"Data Source={dbName};Mode=Memory;Cache=Shared";
        var keepAliveConnection = new SqliteConnection(connectionString);
        keepAliveConnection.Open();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDbContext<LastHourDbContext>(options => options.UseSqlite(connectionString));
        services.AddSingleton(clock);
        services.AddSingleton<ConsumerIdempotencyMetrics>();
        services.AddScoped<IConsumerIdempotencyService, ConsumerIdempotencyService>();

        ServiceProvider provider = services.BuildServiceProvider();

        using (IServiceScope scope = provider.CreateScope())
        {
            LastHourDbContext dbContext = scope.ServiceProvider.GetRequiredService<LastHourDbContext>();
            dbContext.Database.EnsureCreated();
        }

        return (provider, keepAliveConnection);
    }
}
