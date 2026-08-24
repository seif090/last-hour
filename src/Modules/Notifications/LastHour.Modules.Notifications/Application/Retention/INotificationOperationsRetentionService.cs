namespace LastHour.Modules.Notifications.Application.Retention;

/// <summary>
/// Service contract for executing historical operational record cleanup according to retention policy settings.
/// </summary>
public interface INotificationOperationsRetentionService
{
    /// <summary>
    /// Deletes expired historical attempt records and administrative audit log entries in bounded batches.
    /// </summary>
    /// <param name="utcNow">The current UTC instant for retention cutoff evaluation.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A summary result detailing deleted record counts and execution timestamp.</returns>
    Task<RetentionCleanupResult> CleanupAsync(DateTime utcNow, CancellationToken cancellationToken = default);
}
