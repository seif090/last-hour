namespace LastHour.BuildingBlocks.Infrastructure.Observability.FailureClassification;

/// <summary>
/// Provides deterministic, low-cardinality classification of exceptions occurring during
/// outbox message dispatch and consumer event handling.
/// </summary>
public static class FailureClassifier
{
    /// <summary>Cancellation requested by host shutdown or internal token.</summary>
    public const string Cancellation = "Cancellation";

    /// <summary>Database errors such as SQL exceptions, connection errors, or constraint violations.</summary>
    public const string Database = "Database";

    /// <summary>Optimistic concurrency token mismatch.</summary>
    public const string Concurrency = "Concurrency";

    /// <summary>Input or payload validation failure.</summary>
    public const string Validation = "Validation";

    /// <summary>Network, HTTP, or socket infrastructure failure.</summary>
    public const string Infrastructure = "Infrastructure";

    /// <summary>Transient timeouts or deadlock conditions.</summary>
    public const string Transient = "Transient";

    /// <summary>Unclassified general failure.</summary>
    public const string Unknown = "Unknown";

    /// <summary>
    /// Classifies an exception into a bounded, low-cardinality failure type string suitable for metrics tags and structured logs.
    /// </summary>
    /// <param name="exception">The exception to classify.</param>
    /// <returns>A constant failure classification string.</returns>
    public static string Classify(Exception? exception)
    {
        if (exception is null)
        {
            return Unknown;
        }

        if (exception is OperationCanceledException or TaskCanceledException)
        {
            return Cancellation;
        }

        if (exception is Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException)
        {
            return Concurrency;
        }

        if (exception is Microsoft.EntityFrameworkCore.DbUpdateException)
        {
            return Database;
        }

        if (exception is System.Data.Common.DbException)
        {
            return Database;
        }

        if (exception is TimeoutException)
        {
            return Transient;
        }

        if (exception is ArgumentException or FormatException)
        {
            return Validation;
        }

        if (exception is System.Net.Http.HttpRequestException or System.Net.Sockets.SocketException)
        {
            return Infrastructure;
        }

        return Unknown;
    }
}
