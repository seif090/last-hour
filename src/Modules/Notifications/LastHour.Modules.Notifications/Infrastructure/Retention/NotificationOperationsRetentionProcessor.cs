using LastHour.BuildingBlocks.Application.Contracts;
using LastHour.Modules.Notifications.Application.Retention;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LastHour.Modules.Notifications.Infrastructure.Retention;

/// <summary>
/// Dedicated background worker executing historical operational record retention cleanup cycles.
/// </summary>
public sealed class NotificationOperationsRetentionProcessor : BackgroundService
{
    private static readonly Action<ILogger, Exception?> WorkerDisabled =
        LoggerMessage.Define(
            LogLevel.Information,
            new EventId(310, nameof(WorkerDisabled)),
            "Notification operations retention processor is disabled; cleanup cycles will not run.");

    private static readonly Action<ILogger, int, Exception?> CycleStarting =
        LoggerMessage.Define<int>(
            LogLevel.Information,
            new EventId(311, nameof(CycleStarting)),
            "Starting notification retention cleanup cycle (Execution Interval: {IntervalMinutes} minutes).");

    private static readonly Action<ILogger, Exception?> CycleFailed =
        LoggerMessage.Define(
            LogLevel.Error,
            new EventId(312, nameof(CycleFailed)),
            "Notification retention cleanup background cycle failed.");

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly NotificationOperationsRetentionOptions _options;
    private readonly ILogger<NotificationOperationsRetentionProcessor> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="NotificationOperationsRetentionProcessor"/> class.
    /// </summary>
    public NotificationOperationsRetentionProcessor(
        IServiceScopeFactory scopeFactory,
        IOptions<NotificationOperationsRetentionOptions> options,
        ILogger<NotificationOperationsRetentionProcessor> logger)
    {
        _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
        ArgumentNullException.ThrowIfNull(options);
        _options = options.Value;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            WorkerDisabled(_logger, null);
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                CycleStarting(_logger, _options.ExecutionIntervalMinutes, null);

                using (IServiceScope scope = _scopeFactory.CreateScope())
                {
                    var retentionService = scope.ServiceProvider.GetRequiredService<INotificationOperationsRetentionService>();
                    var clock = scope.ServiceProvider.GetRequiredService<IClock>();

                    await retentionService.CleanupAsync(clock.UtcNow, stoppingToken).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                CycleFailed(_logger, ex);
            }

            try
            {
                await Task.Delay(TimeSpan.FromMinutes(_options.ExecutionIntervalMinutes), stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }
    }
}
