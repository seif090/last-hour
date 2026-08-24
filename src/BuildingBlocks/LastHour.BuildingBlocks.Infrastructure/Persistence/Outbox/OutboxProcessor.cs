using System.Diagnostics;
using System.Text.Json;
using LastHour.BuildingBlocks.Application.Contracts;
using LastHour.BuildingBlocks.Application.Events;
using LastHour.BuildingBlocks.Infrastructure.Observability.FailureClassification;
using LastHour.BuildingBlocks.Infrastructure.Persistence.Configuration;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LastHour.BuildingBlocks.Infrastructure.Persistence.Outbox;

/// <summary>
/// Background service that drains the outbox table: it loads a batch of pending messages,
/// rehydrates their payloads, publishes them to in-process handlers and marks them processed.
/// Failed messages are retried on subsequent cycles until the configured retry limit, at which
/// point they are abandoned with the last error preserved. Processing is at-least-once; exactly
/// once delivery is out of scope for the outbox processor itself.
/// </summary>
public sealed class OutboxProcessor : BackgroundService
{
    private static readonly Action<ILogger, Exception?> ProcessorDisabled =
        LoggerMessage.Define(
            LogLevel.Information,
            new EventId(1, nameof(ProcessorDisabled)),
            "Outbox processor is disabled; outbox messages will not be dispatched.");

    private static readonly Action<ILogger, Exception?> CycleFailed =
        LoggerMessage.Define(
            LogLevel.Error,
            new EventId(2, nameof(CycleFailed)),
            "Outbox processing cycle failed.");

    private static readonly Action<ILogger, Guid, string, double, Exception?> MessageProcessingCompleted =
        LoggerMessage.Define<Guid, string, double>(
            LogLevel.Information,
            new EventId(3, nameof(MessageProcessingCompleted)),
            "Completed outbox message {OutboxMessageId} of type {EventType} in {ElapsedMilliseconds:F2}ms.");

    private static readonly Action<ILogger, Guid, string, int, DateTime?, string, Exception?> MessageProcessingFailed =
        LoggerMessage.Define<Guid, string, int, DateTime?, string>(
            LogLevel.Warning,
            new EventId(4, nameof(MessageProcessingFailed)),
            "Failed processing outbox message {OutboxMessageId} of type {EventType} (attempt {RetryCount}, next attempt at {NextAttemptAtUtc}): {Error}");

    private static readonly Action<ILogger, Guid, string, int, string, Exception?> MessageAbandoned =
        LoggerMessage.Define<Guid, string, int, string>(
            LogLevel.Error,
            new EventId(5, nameof(MessageAbandoned)),
            "Abandoned poison outbox message {OutboxMessageId} of type {EventType} after {RetryCount} attempts: {Error}");

    private static readonly Action<ILogger, Guid, DateTime?, Exception?> ExpiredLeaseReclaimed =
        LoggerMessage.Define<Guid, DateTime?>(
            LogLevel.Information,
            new EventId(6, nameof(ExpiredLeaseReclaimed)),
            "Reclaimed expired processing lease for outbox message {OutboxMessageId} (previous lease expired at {PreviousLockExpiresAtUtc}).");

    private static readonly Action<ILogger, Guid, Exception?> MessageConcurrencyConflict =
        LoggerMessage.Define<Guid>(
            LogLevel.Information,
            new EventId(7, nameof(MessageConcurrencyConflict)),
            "Outbox message {OutboxMessageId} was updated concurrently by another processor instance; skipping.");

    private static readonly Action<ILogger, Guid, Exception?> MessageClaimConflict =
        LoggerMessage.Define<Guid>(
            LogLevel.Debug,
            new EventId(8, nameof(MessageClaimConflict)),
            "Outbox message {OutboxMessageId} claim conflict; claimed by another processor.");

    private static readonly Action<ILogger, Guid, string, int, DateTime?, double, Exception?> RetryScheduled =
        LoggerMessage.Define<Guid, string, int, DateTime?, double>(
            LogLevel.Information,
            new EventId(9, nameof(RetryScheduled)),
            "Scheduled retry for outbox message {OutboxMessageId} of type {EventType} (attempt {RetryCount}, next attempt at {NextAttemptAtUtc}, delay {DelayMs:F0}ms).");

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IOptions<OutboxOptions> _options;
    private readonly OutboxMetrics _metrics;
    private readonly IOutboxProcessorTracker _tracker;
    private readonly IClock _clock;
    private readonly ILogger<OutboxProcessor> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="OutboxProcessor"/> class.
    /// </summary>
    /// <param name="scopeFactory">The scope factory used to resolve per-cycle dependencies.</param>
    /// <param name="options">The outbox processor options.</param>
    /// <param name="metrics">The outbox metrics collector.</param>
    /// <param name="tracker">The in-memory processor state tracker.</param>
    /// <param name="clock">The system clock abstraction.</param>
    /// <param name="logger">The logger used to record processing diagnostics.</param>
    public OutboxProcessor(
        IServiceScopeFactory scopeFactory,
        IOptions<OutboxOptions> options,
        OutboxMetrics metrics,
        IOutboxProcessorTracker tracker,
        IClock clock,
        ILogger<OutboxProcessor> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options;
        _metrics = metrics;
        _tracker = tracker;
        _clock = clock;
        _logger = logger;
    }

    /// <summary>
    /// Calculates the exponential backoff delay for a retry attempt:
    /// <c>min(BaseRetryDelay * 2^(retryCount), MaximumRetryDelay)</c>.
    /// </summary>
    /// <param name="baseDelay">The configured base retry delay.</param>
    /// <param name="maxDelay">The configured maximum retry delay cap.</param>
    /// <param name="retryCount">The number of previous failures for the message.</param>
    /// <returns>The calculated retry delay duration.</returns>
    public static TimeSpan CalculateRetryDelay(TimeSpan baseDelay, TimeSpan maxDelay, int retryCount)
    {
        if (baseDelay <= TimeSpan.Zero)
        {
            return TimeSpan.Zero;
        }

        int exponent = Math.Min(30, Math.Max(0, retryCount));
        double multiplier = Math.Pow(2, exponent);
        double delayTicks = baseDelay.Ticks * multiplier;

        if (delayTicks >= maxDelay.Ticks || delayTicks < 0)
        {
            return maxDelay;
        }

        var delay = TimeSpan.FromTicks((long)delayTicks);
        if (delay < baseDelay)
        {
            return baseDelay;
        }

        return delay > maxDelay ? maxDelay : delay;
    }

    /// <inheritdoc/>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Value.Enabled)
        {
            ProcessorDisabled(_logger, null);
            return;
        }

        _tracker.SetRunning(true);
        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                _tracker.RecordHeartbeat(_clock.UtcNow);

                try
                {
                    await ProcessPendingMessagesAsync(stoppingToken).ConfigureAwait(false);
                    _tracker.RecordSuccess(_clock.UtcNow);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception exception)
                {
                    _tracker.RecordFailure(_clock.UtcNow);
                    CycleFailed(_logger, exception);
                }

                try
                {
                    await Task.Delay(_options.Value.ProcessingInterval, stoppingToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
            }
        }
        finally
        {
            _tracker.SetRunning(false);
        }
    }

    private static async Task DispatchAsync(IMediator mediator, OutboxMessage message, CancellationToken cancellationToken)
    {
        Type type = Type.GetType(message.Type)
                    ?? throw new InvalidOperationException($"Cannot resolve outbox message type '{message.Type}'.");

        object payload = JsonSerializer.Deserialize(message.Content, type, OutboxJson.SerializerOptions)
                         ?? throw new InvalidOperationException($"Outbox message '{message.Id}' has no content.");

        object notification = NotificationMessageFactory.Create(payload);
        await mediator.Publish(notification, cancellationToken).ConfigureAwait(false);
    }

    private async Task ProcessPendingMessagesAsync(CancellationToken cancellationToken)
    {
        using IServiceScope scope = _scopeFactory.CreateScope();
        LastHourDbContext dbContext = scope.ServiceProvider.GetRequiredService<LastHourDbContext>();
        IMediator mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        OutboxOptions options = _options.Value;
        DateTime now = _clock.UtcNow;

        List<OutboxMessage> messages = await dbContext.OutboxMessages
            .Where(message => message.ProcessedOnUtc == null
                           && (message.NextAttemptAtUtc == null || message.NextAttemptAtUtc <= now)
                           && (message.LockExpiresAtUtc == null || message.LockExpiresAtUtc < now))
            .OrderBy(message => message.NextAttemptAtUtc)
            .ThenBy(message => message.OccurredOnUtc)
            .Take(options.BatchSize)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        if (messages.Count == 0)
        {
            return;
        }

        foreach (OutboxMessage message in messages)
        {
            cancellationToken.ThrowIfCancellationRequested();

            bool wasExpiredLease = message.LockExpiresAtUtc.HasValue && message.LockExpiresAtUtc.Value < now;
            DateTime? previousLockExpiresAtUtc = message.LockExpiresAtUtc;

            Guid claimToken = Guid.NewGuid();
            DateTime leaseExpiresAtUtc = now.Add(options.LeaseDuration);
            message.Claim(claimToken, leaseExpiresAtUtc);

            try
            {
                await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (DbUpdateConcurrencyException exception)
            {
                _metrics.RecordClaimConflict();
                MessageClaimConflict(_logger, message.Id, exception);
                dbContext.Entry(message).State = EntityState.Detached;
                continue;
            }

            if (wasExpiredLease)
            {
                ExpiredLeaseReclaimed(_logger, message.Id, previousLockExpiresAtUtc, null);
                _metrics.RecordMessageReclaimed(message.Type);
            }

            var stopwatch = Stopwatch.StartNew();
            bool succeeded = false;
            string? failureError = null;
            string failureType = FailureClassifier.Unknown;

            using (Activity? activity = OutboxMetrics.ActivitySource.StartActivity("Outbox.Dispatch", ActivityKind.Consumer))
            {
                if (activity is not null)
                {
                    activity.SetTag("outbox.message_id", message.Id.ToString());
                    activity.SetTag("outbox.event_type", message.Type);
                    activity.SetTag("outbox.retry_count", message.RetryCount);
                }

                try
                {
                    await DispatchAsync(mediator, message, cancellationToken).ConfigureAwait(false);
                    message.MarkProcessed(_clock.UtcNow);
                    succeeded = true;
                    activity?.SetStatus(ActivityStatusCode.Ok);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    // Graceful host shutdown: do not mutate failure state, do not emit failure/retry telemetry, do not set Error status on activity.
                    throw;
                }
                catch (Exception exception)
                {
                    failureError = exception.Message;
                    failureType = FailureClassifier.Classify(exception);
                    activity?.SetStatus(ActivityStatusCode.Error, exception.Message);
                    activity?.SetTag("failure.type", failureType);
                    TimeSpan retryDelay = CalculateRetryDelay(options.BaseRetryDelay, options.MaximumRetryDelay, message.RetryCount);
                    message.RecordFailure(exception.Message, _clock.UtcNow, options.MaxRetryCount, retryDelay);
                }
            }

            try
            {
                await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                stopwatch.Stop();

                if (succeeded)
                {
                    double deliveryLatencyMs = Math.Max(0, (_clock.UtcNow - message.OccurredOnUtc).TotalMilliseconds);
                    MessageProcessingCompleted(_logger, message.Id, message.Type, stopwatch.Elapsed.TotalMilliseconds, null);
                    _metrics.RecordMessageProcessed(message.Type, stopwatch.Elapsed.TotalMilliseconds);
                    _metrics.RecordDeliveryLatency(message.Type, deliveryLatencyMs);
                }
                else
                {
                    _metrics.RecordMessageFailed(message.Type, failureType);
                    if (message.RetryCount >= options.MaxRetryCount)
                    {
                        MessageAbandoned(_logger, message.Id, message.Type, message.RetryCount, failureError ?? "Unknown", null);
                        _metrics.RecordMessageAbandoned(message.Type, failureType);
                    }
                    else
                    {
                        MessageProcessingFailed(_logger, message.Id, message.Type, message.RetryCount, message.NextAttemptAtUtc, failureError ?? "Unknown", null);
                        _metrics.RecordMessageRetried(message.Type, failureType);
                    }
                }
            }
            catch (DbUpdateConcurrencyException exception)
            {
                MessageConcurrencyConflict(_logger, message.Id, exception);
                dbContext.Entry(message).State = EntityState.Detached;
            }
        }
    }
}
