using LastHour.BuildingBlocks.Application.Contracts;
using LastHour.Modules.Notifications.Application.Persistence;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LastHour.Modules.Notifications.Application.Retention;

/// <summary>
/// Executes bounded historical operational data cleanup according to retention options.
/// </summary>
public sealed class NotificationOperationsRetentionService : INotificationOperationsRetentionService
{
    private static readonly Action<ILogger, DateTime, DateTime, int, Exception?> CleanupStarted =
        LoggerMessage.Define<DateTime, DateTime, int>(
            LogLevel.Information,
            new EventId(301, nameof(CleanupStarted)),
            "Starting notification retention cleanup (Attempt Cutoff: {AttemptCutoffUtc}, Audit Cutoff: {AuditCutoffUtc}, Batch Size: {BatchSize}).");

    private static readonly Action<ILogger, int, int, double, Exception?> CleanupCompleted =
        LoggerMessage.Define<int, int, double>(
            LogLevel.Information,
            new EventId(302, nameof(CleanupCompleted)),
            "Notification retention cleanup completed: deleted {AttemptsDeleted} attempts and {AuditEntriesDeleted} audit entries in {ElapsedMilliseconds:F2}ms.");

    private static readonly Action<ILogger, Exception?> CleanupFailed =
        LoggerMessage.Define(
            LogLevel.Error,
            new EventId(303, nameof(CleanupFailed)),
            "Notification retention cleanup execution failed.");

    private readonly INotificationDeliveryAttemptRepository _attemptRepository;
    private readonly INotificationDeliveryAuditRepository _auditRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly NotificationOperationsRetentionOptions _options;
    private readonly INotificationOperationsRetentionMetrics? _metrics;
    private readonly ILogger<NotificationOperationsRetentionService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="NotificationOperationsRetentionService"/> class.
    /// </summary>
    public NotificationOperationsRetentionService(
        INotificationDeliveryAttemptRepository attemptRepository,
        INotificationDeliveryAuditRepository auditRepository,
        IUnitOfWork unitOfWork,
        IOptions<NotificationOperationsRetentionOptions> options,
        ILogger<NotificationOperationsRetentionService> logger,
        INotificationOperationsRetentionMetrics? metrics = null)
    {
        _attemptRepository = attemptRepository ?? throw new ArgumentNullException(nameof(attemptRepository));
        _auditRepository = auditRepository ?? throw new ArgumentNullException(nameof(auditRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        ArgumentNullException.ThrowIfNull(options);
        _options = options.Value;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _metrics = metrics;
    }

    /// <inheritdoc/>
    public async Task<RetentionCleanupResult> CleanupAsync(DateTime utcNow, CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled)
        {
            return new RetentionCleanupResult(0, 0, utcNow);
        }

        _metrics?.RecordCleanupStarted();

        DateTime attemptCutoffUtc = utcNow.AddDays(-_options.AttemptRetentionDays);
        DateTime auditCutoffUtc = utcNow.AddDays(-_options.AuditRetentionDays);

        CleanupStarted(_logger, attemptCutoffUtc, auditCutoffUtc, _options.CleanupBatchSize, null);
        long startTimestamp = System.Diagnostics.Stopwatch.GetTimestamp();

        try
        {
            int totalAttemptsDeleted = 0;
            int deletedAttemptsInBatch;
            do
            {
                cancellationToken.ThrowIfCancellationRequested();
                deletedAttemptsInBatch = await _attemptRepository.DeleteOlderThanAsync(attemptCutoffUtc, _options.CleanupBatchSize, cancellationToken);
                if (deletedAttemptsInBatch > 0)
                {
                    await _unitOfWork.SaveChangesAsync(cancellationToken);
                }

                totalAttemptsDeleted += deletedAttemptsInBatch;
            }
            while (deletedAttemptsInBatch >= _options.CleanupBatchSize);

            int totalAuditEntriesDeleted = 0;
            int deletedAuditInBatch;
            do
            {
                cancellationToken.ThrowIfCancellationRequested();
                deletedAuditInBatch = await _auditRepository.DeleteOlderThanAsync(auditCutoffUtc, _options.CleanupBatchSize, cancellationToken);
                if (deletedAuditInBatch > 0)
                {
                    await _unitOfWork.SaveChangesAsync(cancellationToken);
                }

                totalAuditEntriesDeleted += deletedAuditInBatch;
            }
            while (deletedAuditInBatch >= _options.CleanupBatchSize);

            double elapsedMs = System.Diagnostics.Stopwatch.GetElapsedTime(startTimestamp).TotalMilliseconds;
            CleanupCompleted(_logger, totalAttemptsDeleted, totalAuditEntriesDeleted, elapsedMs, null);

            _metrics?.RecordCleanupCompleted(totalAttemptsDeleted, totalAuditEntriesDeleted);

            return new RetentionCleanupResult(totalAttemptsDeleted, totalAuditEntriesDeleted, utcNow);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            CleanupFailed(_logger, ex);
            _metrics?.RecordCleanupFailed();
            throw;
        }
    }
}
