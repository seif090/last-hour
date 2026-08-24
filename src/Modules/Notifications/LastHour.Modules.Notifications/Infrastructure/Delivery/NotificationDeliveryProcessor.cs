using System.Diagnostics;
using LastHour.BuildingBlocks.Application.Contracts;
using LastHour.BuildingBlocks.SharedKernel.Results;
using LastHour.Modules.Notifications.Application.Delivery;
using LastHour.Modules.Notifications.Application.Persistence;
using LastHour.Modules.Notifications.Domain.Notifications;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LastHour.Modules.Notifications.Infrastructure.Delivery;

/// <summary>
/// Dedicated background worker that polls eligible <see cref="NotificationDelivery"/> records,
/// claims worker leases, dispatches deliveries through channel adapters, handles exponential retry backoff,
/// and updates delivery states.
/// </summary>
public sealed class NotificationDeliveryProcessor : BackgroundService
{
    private static readonly Action<ILogger, Exception?> ProcessorDisabled =
        LoggerMessage.Define(
            LogLevel.Information,
            new EventId(201, nameof(ProcessorDisabled)),
            "Notification delivery processor is disabled; deliveries will not be processed.");

    private static readonly Action<ILogger, Exception?> CycleFailed =
        LoggerMessage.Define(
            LogLevel.Error,
            new EventId(202, nameof(CycleFailed)),
            "Notification delivery processing cycle failed.");

    private static readonly Action<ILogger, Guid, string, double, Exception?> DeliveryCompleted =
        LoggerMessage.Define<Guid, string, double>(
            LogLevel.Information,
            new EventId(203, nameof(DeliveryCompleted)),
            "Completed notification delivery {DeliveryId} for channel {Channel} in {ElapsedMilliseconds:F2}ms.");

    private static readonly Action<ILogger, Guid, string, int, DateTime?, string, Exception?> DeliveryFailed =
        LoggerMessage.Define<Guid, string, int, DateTime?, string>(
            LogLevel.Warning,
            new EventId(204, nameof(DeliveryFailed)),
            "Failed notification delivery {DeliveryId} for channel {Channel} (attempt {RetryCount}, next attempt at {NextAttemptAtUtc}): {Error}");

    private static readonly Action<ILogger, Guid, string, int, string, Exception?> DeliveryAbandoned =
        LoggerMessage.Define<Guid, string, int, string>(
            LogLevel.Error,
            new EventId(205, nameof(DeliveryAbandoned)),
            "Abandoned notification delivery {DeliveryId} for channel {Channel} after {RetryCount} attempts: {Error}");

    private static readonly Action<ILogger, Guid, string, DateTime?, Exception?> ExpiredLeaseReclaimed =
        LoggerMessage.Define<Guid, string, DateTime?>(
            LogLevel.Information,
            new EventId(206, nameof(ExpiredLeaseReclaimed)),
            "Reclaimed expired processing lease for delivery {DeliveryId} on channel {Channel} (previous lease expired at {PreviousLockExpiresAtUtc}).");

    private static readonly Action<ILogger, Guid, Exception?> DeliveryClaimConflict =
        LoggerMessage.Define<Guid>(
            LogLevel.Debug,
            new EventId(207, nameof(DeliveryClaimConflict)),
            "Notification delivery {DeliveryId} claim conflict; claimed by another processor.");

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IOptions<NotificationDeliveryOptions> _options;
    private readonly NotificationDeliveryMetrics _metrics;
    private readonly IClock _clock;
    private readonly ILogger<NotificationDeliveryProcessor> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="NotificationDeliveryProcessor"/> class.
    /// </summary>
    public NotificationDeliveryProcessor(
        IServiceScopeFactory scopeFactory,
        IOptions<NotificationDeliveryOptions> options,
        NotificationDeliveryMetrics metrics,
        IClock clock,
        ILogger<NotificationDeliveryProcessor> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options;
        _metrics = metrics;
        _clock = clock;
        _logger = logger;
    }

    /// <summary>
    /// Calculates the exponential backoff delay for a delivery retry attempt:
    /// <c>min(BaseRetryDelay * 2^(retryCount), MaximumRetryDelay)</c>.
    /// </summary>
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

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessPendingDeliveriesAsync(stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                CycleFailed(_logger, exception);
            }

            try
            {
                await Task.Delay(_options.Value.PollingInterval, stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }
    }

    private async Task ProcessPendingDeliveriesAsync(CancellationToken cancellationToken)
    {
        using IServiceScope scope = _scopeFactory.CreateScope();
        IUnitOfWork unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        INotificationDeliveryRepository deliveryRepository = scope.ServiceProvider.GetRequiredService<INotificationDeliveryRepository>();
        INotificationRepository notificationRepository = scope.ServiceProvider.GetRequiredService<INotificationRepository>();
        INotificationChannelFactory channelFactory = scope.ServiceProvider.GetRequiredService<INotificationChannelFactory>();

        NotificationDeliveryOptions options = _options.Value;
        DateTime now = _clock.UtcNow;

        IReadOnlyList<NotificationDelivery> deliveries = await deliveryRepository.GetPendingDeliveriesAsync(options.BatchSize, now, cancellationToken).ConfigureAwait(false);

        if (deliveries.Count == 0)
        {
            return;
        }

        foreach (NotificationDelivery summaryItem in deliveries)
        {
            cancellationToken.ThrowIfCancellationRequested();

            NotificationDelivery? delivery = await deliveryRepository.GetByIdTrackedAsync(summaryItem.Id, cancellationToken).ConfigureAwait(false);
            if (delivery is null || delivery.Status == DeliveryStatus.Delivered)
            {
                continue;
            }

            bool wasExpiredLease = delivery.LockExpiresAtUtc.HasValue && delivery.LockExpiresAtUtc.Value < now;
            DateTime? previousLockExpiresAtUtc = delivery.LockExpiresAtUtc;

            Guid claimToken = Guid.NewGuid();
            DateTime leaseExpiresAtUtc = now.Add(options.LeaseDuration);
            delivery.Claim(claimToken, leaseExpiresAtUtc, now);

            try
            {
                await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex.GetType().Name.Contains("Concurrency", StringComparison.OrdinalIgnoreCase))
            {
                _metrics.RecordClaimConflict();
                DeliveryClaimConflict(_logger, delivery.Id, null);
                continue;
            }

            string channelName = delivery.Channel.ToString();

            if (wasExpiredLease)
            {
                ExpiredLeaseReclaimed(_logger, delivery.Id, channelName, previousLockExpiresAtUtc, null);
                _metrics.RecordReclaimed(channelName);
            }

            var stopwatch = Stopwatch.StartNew();
            bool succeeded = false;
            string? failureError = null;
            string failureType = NotificationDeliveryFailureClassifier.Unknown;

            using (Activity? activity = NotificationDeliveryMetrics.ActivitySource.StartActivity("NotificationDelivery.Dispatch", ActivityKind.Consumer))
            {
                if (activity is not null)
                {
                    activity.SetTag("delivery.id", delivery.Id.ToString());
                    activity.SetTag("delivery.channel", channelName);
                    activity.SetTag("delivery.retry_count", delivery.RetryCount);
                }

                try
                {
                    Notification? notification = await notificationRepository.GetByIdAsync(delivery.NotificationId, cancellationToken).ConfigureAwait(false);
                    if (notification is null)
                    {
                        throw new InvalidOperationException($"Parent notification '{delivery.NotificationId}' not found.");
                    }

                    INotificationChannelSender sender = channelFactory.GetSender(delivery.Channel);
                    Result result = await sender.SendAsync(notification, delivery, cancellationToken).ConfigureAwait(false);

                    if (result.IsSuccess)
                    {
                        delivery.MarkDelivered(_clock.UtcNow);
                        succeeded = true;
                        activity?.SetStatus(ActivityStatusCode.Ok);
                    }
                    else
                    {
                        Error error = result.FirstError ?? Error.Failure("Delivery.Error", "Delivery attempt failed.");
                        failureError = error.Description;
                        failureType = NotificationDeliveryFailureClassifier.Classify(error);
                        activity?.SetStatus(ActivityStatusCode.Error, failureError);
                        activity?.SetTag("failure.type", failureType);

                        TimeSpan retryDelay = CalculateRetryDelay(options.BaseRetryDelay, options.MaximumRetryDelay, delivery.RetryCount);
                        delivery.RecordFailure(failureError, _clock.UtcNow, options.MaxRetryCount, retryDelay);
                    }
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception exception)
                {
                    failureError = exception.Message;
                    failureType = NotificationDeliveryFailureClassifier.Classify(exception);
                    activity?.SetStatus(ActivityStatusCode.Error, exception.Message);
                    activity?.SetTag("failure.type", failureType);

                    TimeSpan retryDelay = CalculateRetryDelay(options.BaseRetryDelay, options.MaximumRetryDelay, delivery.RetryCount);
                    delivery.RecordFailure(exception.Message, _clock.UtcNow, options.MaxRetryCount, retryDelay);
                }
            }

            try
            {
                await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                stopwatch.Stop();

                _metrics.RecordProcessed(channelName, stopwatch.Elapsed.TotalMilliseconds);

                if (succeeded)
                {
                    DeliveryCompleted(_logger, delivery.Id, channelName, stopwatch.Elapsed.TotalMilliseconds, null);
                    _metrics.RecordDelivered(channelName);
                }
                else
                {
                    _metrics.RecordFailed(channelName, failureType);
                    if (delivery.Status == DeliveryStatus.FailedPermanent)
                    {
                        DeliveryAbandoned(_logger, delivery.Id, channelName, delivery.RetryCount, failureError ?? "Unknown", null);
                        _metrics.RecordAbandoned(channelName, failureType);
                    }
                    else
                    {
                        DeliveryFailed(_logger, delivery.Id, channelName, delivery.RetryCount, delivery.NextAttemptAtUtc, failureError ?? "Unknown", null);
                        _metrics.RecordRetried(channelName, failureType);
                    }
                }
            }
            catch (Exception ex) when (ex.GetType().Name.Contains("Concurrency", StringComparison.OrdinalIgnoreCase))
            {
                DeliveryClaimConflict(_logger, delivery.Id, ex);
            }
        }
    }
}
