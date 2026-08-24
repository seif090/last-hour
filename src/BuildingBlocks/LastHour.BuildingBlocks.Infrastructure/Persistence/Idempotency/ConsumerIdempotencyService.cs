using System.Diagnostics;
using LastHour.BuildingBlocks.Application.Contracts;
using LastHour.BuildingBlocks.Infrastructure.Observability.FailureClassification;
using LastHour.BuildingBlocks.Infrastructure.Persistence.Configuration;
using LastHour.BuildingBlocks.SharedKernel.Domain.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LastHour.BuildingBlocks.Infrastructure.Persistence.Idempotency;

/// <summary>
/// Implements consumer-scoped event idempotency controls using PostgreSQL database uniqueness
/// and lease expiration to safely suppress duplicate event deliveries under at-least-once delivery.
/// </summary>
public sealed class ConsumerIdempotencyService : IConsumerIdempotencyService
{
    private static readonly Action<ILogger, Guid, string, Exception?> DuplicateEventSuppressed =
        LoggerMessage.Define<Guid, string>(
            LogLevel.Debug,
            new EventId(1, nameof(DuplicateEventSuppressed)),
            "Suppressed duplicate event {EventId} for consumer {Consumer}.");

    private static readonly Action<ILogger, Guid, string, Exception?> CancellationCleanupFailed =
        LoggerMessage.Define<Guid, string>(
            LogLevel.Warning,
            new EventId(2, nameof(CancellationCleanupFailed)),
            "Failed to release in-progress idempotency record for event {EventId} and consumer {Consumer} during cancellation.");

    private static readonly Action<ILogger, string, Guid, string, Exception?> ConsumerExecutionFailed =
        LoggerMessage.Define<string, Guid, string>(
            LogLevel.Error,
            new EventId(3, nameof(ConsumerExecutionFailed)),
            "Consumer {Consumer} failed while processing event {EventId} (failure type: {FailureType}).");

    private static readonly Action<ILogger, Guid, string, Exception?> FailureCleanupFailed =
        LoggerMessage.Define<Guid, string>(
            LogLevel.Warning,
            new EventId(4, nameof(FailureCleanupFailed)),
            "Failed to release in-progress idempotency record for event {EventId} and consumer {Consumer} after handler failure.");

    private static readonly Action<ILogger, Guid, string, double, Exception?> LeaseAcquired =
        LoggerMessage.Define<Guid, string, double>(
            LogLevel.Debug,
            new EventId(5, nameof(LeaseAcquired)),
            "Acquired processing lease for event {EventId} and consumer {Consumer} for {LeaseDurationMs:F0}ms.");

    private static readonly Action<ILogger, Guid, string, DateTime?, Exception?> LeaseReclaimed =
        LoggerMessage.Define<Guid, string, DateTime?>(
            LogLevel.Information,
            new EventId(6, nameof(LeaseReclaimed)),
            "Reclaimed expired processing lease for event {EventId} and consumer {Consumer} (previous lease expired at {PreviousLockExpiresAtUtc}).");

    private static readonly Action<ILogger, Guid, string, double, Exception?> ConsumerExecutionCompleted =
        LoggerMessage.Define<Guid, string, double>(
            LogLevel.Information,
            new EventId(7, nameof(ConsumerExecutionCompleted)),
            "Consumer {Consumer} completed processing event {EventId} in {ElapsedMilliseconds:F2}ms.");

    private readonly LastHourDbContext _dbContext;
    private readonly IClock _clock;
    private readonly ConsumerIdempotencyMetrics _metrics;
    private readonly IOptions<ConsumerIdempotencyOptions> _options;
    private readonly ILogger<ConsumerIdempotencyService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConsumerIdempotencyService"/> class.
    /// </summary>
    /// <param name="dbContext">The database context used for idempotency persistence.</param>
    /// <param name="clock">The time provider for UTC timestamps.</param>
    /// <param name="metrics">The consumer idempotency metrics instrumentation.</param>
    /// <param name="options">The consumer idempotency options.</param>
    /// <param name="logger">The logger instance.</param>
    public ConsumerIdempotencyService(
        LastHourDbContext dbContext,
        IClock clock,
        ConsumerIdempotencyMetrics metrics,
        IOptions<ConsumerIdempotencyOptions> options,
        ILogger<ConsumerIdempotencyService> logger)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        _metrics = metrics ?? throw new ArgumentNullException(nameof(metrics));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConsumerIdempotencyService"/> class with default options.
    /// </summary>
    /// <param name="dbContext">The database context used for idempotency persistence.</param>
    /// <param name="clock">The time provider for UTC timestamps.</param>
    /// <param name="metrics">The consumer idempotency metrics instrumentation.</param>
    /// <param name="logger">The logger instance.</param>
    public ConsumerIdempotencyService(
        LastHourDbContext dbContext,
        IClock clock,
        ConsumerIdempotencyMetrics metrics,
        ILogger<ConsumerIdempotencyService> logger)
        : this(dbContext, clock, metrics, Microsoft.Extensions.Options.Options.Create(new ConsumerIdempotencyOptions()), logger)
    {
    }

    /// <inheritdoc/>
    public async Task<bool> ExecuteAsync(
        Guid eventId,
        string consumer,
        Func<CancellationToken, Task> handler,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(handler);
        ValidateParameters(eventId, consumer);

        string normalizedConsumer = consumer.Trim();
        bool acquired = await TryAcquireAsync(eventId, normalizedConsumer, cancellationToken).ConfigureAwait(false);
        if (!acquired)
        {
            _metrics.RecordDuplicate(normalizedConsumer);
            DuplicateEventSuppressed(_logger, eventId, normalizedConsumer, null);
            return false;
        }

        var stopwatch = Stopwatch.StartNew();

        using (Activity? activity = ConsumerIdempotencyMetrics.ActivitySource.StartActivity("Consumer.Handle", ActivityKind.Consumer))
        {
            if (activity is not null)
            {
                activity.SetTag("consumer.name", normalizedConsumer);
                activity.SetTag("event.id", eventId.ToString());
            }

            try
            {
                await handler(cancellationToken).ConfigureAwait(false);
                await CompleteAsync(eventId, normalizedConsumer, cancellationToken).ConfigureAwait(false);
                stopwatch.Stop();

                activity?.SetStatus(ActivityStatusCode.Ok);
                ConsumerExecutionCompleted(_logger, eventId, normalizedConsumer, stopwatch.Elapsed.TotalMilliseconds, null);
                _metrics.RecordProcessed(normalizedConsumer, stopwatch.Elapsed.TotalMilliseconds);
                return true;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                // Host graceful shutdown: release lease immediately for subsequent retry, do not record failure metric.
                try
                {
                    await ReleaseAsync(eventId, normalizedConsumer, CancellationToken.None).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    CancellationCleanupFailed(_logger, eventId, normalizedConsumer, ex);
                }

                throw;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                string failureType = FailureClassifier.Classify(ex);

                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                activity?.SetTag("failure.type", failureType);

                _metrics.RecordFailed(normalizedConsumer, failureType);
                ConsumerExecutionFailed(_logger, normalizedConsumer, eventId, failureType, ex);

                try
                {
                    await ReleaseAsync(eventId, normalizedConsumer, CancellationToken.None).ConfigureAwait(false);
                }
                catch (Exception releaseEx)
                {
                    FailureCleanupFailed(_logger, eventId, normalizedConsumer, releaseEx);
                }

                throw;
            }
        }
    }

    /// <inheritdoc/>
    public Task<bool> ExecuteAsync(
        IDomainEvent domainEvent,
        string consumer,
        Func<CancellationToken, Task> handler,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);
        return ExecuteAsync(domainEvent.EventId, consumer, handler, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<bool> TryAcquireAsync(
        Guid eventId,
        string consumer,
        CancellationToken cancellationToken = default)
    {
        ValidateParameters(eventId, consumer);

        string normalizedConsumer = consumer.Trim();
        DateTime now = _clock.UtcNow;

        ConsumerInboxMessage? existing = await _dbContext.ConsumerInboxMessages
            .FirstOrDefaultAsync(m => m.EventId == eventId && m.Consumer == normalizedConsumer, cancellationToken)
            .ConfigureAwait(false);

        if (existing is not null)
        {
            if (existing.IsCompleted)
            {
                return false;
            }

            if (existing.LockExpiresAtUtc.HasValue && existing.LockExpiresAtUtc.Value < now)
            {
                DateTime? previousLockExpiresAtUtc = existing.LockExpiresAtUtc;
                existing.RenewLease(now, _options.Value.LeaseDuration);
                try
                {
                    await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                    LeaseReclaimed(_logger, eventId, normalizedConsumer, previousLockExpiresAtUtc, null);
                    _metrics.RecordLeaseReclaimed(normalizedConsumer);
                    return true;
                }
                catch (DbUpdateConcurrencyException)
                {
                    _dbContext.Entry(existing).State = EntityState.Detached;
                    return false;
                }
            }

            return false;
        }

        var message = ConsumerInboxMessage.Create(eventId, normalizedConsumer, now, _options.Value.LeaseDuration);
        _dbContext.ConsumerInboxMessages.Add(message);
        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            LeaseAcquired(_logger, eventId, normalizedConsumer, _options.Value.LeaseDuration.TotalMilliseconds, null);
            _metrics.RecordLeaseAcquired(normalizedConsumer);
            return true;
        }
        catch (DbUpdateException)
        {
            _dbContext.Entry(message).State = EntityState.Detached;
            return false;
        }
    }

    /// <inheritdoc/>
    public async Task CompleteAsync(
        Guid eventId,
        string consumer,
        CancellationToken cancellationToken = default)
    {
        ValidateParameters(eventId, consumer);

        string normalizedConsumer = consumer.Trim();
        ConsumerInboxMessage? existing = await _dbContext.ConsumerInboxMessages
            .FirstOrDefaultAsync(m => m.EventId == eventId && m.Consumer == normalizedConsumer, cancellationToken)
            .ConfigureAwait(false);

        if (existing is not null && !existing.IsCompleted)
        {
            existing.Complete(_clock.UtcNow);
            await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    /// <inheritdoc/>
    public async Task ReleaseAsync(
        Guid eventId,
        string consumer,
        CancellationToken cancellationToken = default)
    {
        ValidateParameters(eventId, consumer);

        string normalizedConsumer = consumer.Trim();
        ConsumerInboxMessage? existing = await _dbContext.ConsumerInboxMessages
            .FirstOrDefaultAsync(m => m.EventId == eventId && m.Consumer == normalizedConsumer, cancellationToken)
            .ConfigureAwait(false);

        if (existing is not null && !existing.IsCompleted)
        {
            _dbContext.ConsumerInboxMessages.Remove(existing);
            await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    private static void ValidateParameters(Guid eventId, string consumer)
    {
        if (eventId == Guid.Empty)
        {
            throw new ArgumentException("The event identifier cannot be empty.", nameof(eventId));
        }

        if (string.IsNullOrWhiteSpace(consumer))
        {
            throw new ArgumentException("The consumer identifier cannot be null or whitespace.", nameof(consumer));
        }

        if (consumer.Length > ConsumerInboxMessage.MaxConsumerLength)
        {
            throw new ArgumentException(
                $"The consumer identifier cannot exceed {ConsumerInboxMessage.MaxConsumerLength} characters.",
                nameof(consumer));
        }
    }
}
