namespace LastHour.BuildingBlocks.Infrastructure.Persistence.Outbox.Queries;

/// <summary>
/// Provides unified operational status classification for outbox messages.
/// </summary>
public static class OutboxStatusClassifier
{
    /// <summary>
    /// Derives the operational status of an outbox message from its lifecycle fields and the current UTC timestamp.
    /// </summary>
    /// <param name="processedOnUtc">The timestamp when the message was marked processed or abandoned, if any.</param>
    /// <param name="error">The recorded error diagnostic, if any.</param>
    /// <param name="nextAttemptAtUtc">The scheduled next retry attempt timestamp, if any.</param>
    /// <param name="lockExpiresAtUtc">The active lease expiration timestamp, if any.</param>
    /// <param name="now">The current UTC reference timestamp.</param>
    /// <returns>The derived <see cref="OutboxMessageStatus"/>.</returns>
    public static OutboxMessageStatus Derive(
        DateTime? processedOnUtc,
        string? error,
        DateTime? nextAttemptAtUtc,
        DateTime? lockExpiresAtUtc,
        DateTime now)
    {
        if (processedOnUtc is not null)
        {
            return error is not null ? OutboxMessageStatus.Abandoned : OutboxMessageStatus.Processed;
        }

        if (lockExpiresAtUtc is not null && lockExpiresAtUtc >= now)
        {
            return OutboxMessageStatus.Processing;
        }

        if (nextAttemptAtUtc is not null && nextAttemptAtUtc > now)
        {
            return OutboxMessageStatus.Scheduled;
        }

        return OutboxMessageStatus.Pending;
    }
}
