using Microsoft.EntityFrameworkCore;

namespace LastHour.Modules.Notifications.Persistence;

/// <summary>
/// Provides utility methods to detect whether a <see cref="DbUpdateException"/> was caused by
/// a unique constraint violation on the <c>notifications</c> business effect idempotency index.
/// </summary>
public static class NotificationDbExceptionDetector
{
    private const string PostgresUniqueViolationSqlState = "23505";
    private const int SqliteConstraintErrorCode = 19;
    private const string BusinessIdempotencyIndexName = "IX_notifications_event_id_type";

    /// <summary>
    /// Determines whether the specified database update exception was caused by a uniqueness violation
    /// on the notification business effect constraint.
    /// </summary>
    /// <param name="exception">The database update exception to inspect.</param>
    /// <returns><see langword="true"/> if the exception represents a duplicate notification business effect; otherwise <see langword="false"/>.</returns>
    public static bool IsNotificationUniqueConstraintViolation(DbUpdateException exception)
    {
        if (exception is null)
        {
            return false;
        }

        Exception? current = exception;
        while (current is not null)
        {
            // PostgreSQL provider check (Npgsql.PostgresException)
            if (current.GetType().Name == "PostgresException")
            {
                Type type = current.GetType();
                var sqlState = type.GetProperty("SqlState")?.GetValue(current) as string;
                var constraintName = type.GetProperty("ConstraintName")?.GetValue(current) as string;

                if (sqlState == PostgresUniqueViolationSqlState)
                {
                    if (string.Equals(constraintName, BusinessIdempotencyIndexName, StringComparison.OrdinalIgnoreCase) ||
                        current.Message.Contains(BusinessIdempotencyIndexName, StringComparison.OrdinalIgnoreCase) ||
                        (current.Message.Contains("notifications", StringComparison.OrdinalIgnoreCase) && current.Message.Contains("EventId", StringComparison.OrdinalIgnoreCase)))
                    {
                        return true;
                    }
                }
            }

            // SQLite provider check (Microsoft.Data.Sqlite.SqliteException)
            if (current.GetType().Name == "SqliteException")
            {
                Type type = current.GetType();
                var errorCode = type.GetProperty("SqliteErrorCode")?.GetValue(current) as int?;

                if (errorCode == SqliteConstraintErrorCode)
                {
                    if (current.Message.Contains("notifications", StringComparison.OrdinalIgnoreCase) ||
                        current.Message.Contains(BusinessIdempotencyIndexName, StringComparison.OrdinalIgnoreCase) ||
                        current.Message.Contains("EventId", StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }
            }

            current = current.InnerException;
        }

        return false;
    }
}
