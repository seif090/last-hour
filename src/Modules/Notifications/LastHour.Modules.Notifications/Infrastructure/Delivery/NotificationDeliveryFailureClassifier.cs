using LastHour.BuildingBlocks.SharedKernel.Results;

namespace LastHour.Modules.Notifications.Infrastructure.Delivery;

/// <summary>
/// Provides deterministic, low-cardinality classification of delivery attempt failures for metric tags and diagnostic logs.
/// </summary>
public static class NotificationDeliveryFailureClassifier
{
    public const string Cancellation = "Cancellation";
    public const string Database = "Database";
    public const string Concurrency = "Concurrency";
    public const string Validation = "Validation";
    public const string Infrastructure = "Infrastructure";
    public const string Transient = "Transient";
    public const string Unknown = "Unknown";

    /// <summary>
    /// Classifies an exception into a low-cardinality failure type string.
    /// </summary>
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

        if (exception.GetType().Name.Contains("Concurrency", StringComparison.OrdinalIgnoreCase))
        {
            return Concurrency;
        }

        if (exception.GetType().Name.Contains("Db", StringComparison.OrdinalIgnoreCase) ||
            exception.GetType().Name.Contains("Sql", StringComparison.OrdinalIgnoreCase))
        {
            return Database;
        }

        if (exception is ArgumentException or InvalidOperationException or FormatException)
        {
            return Validation;
        }

        if (exception is HttpRequestException or System.Net.Sockets.SocketException or TimeoutException)
        {
            return Transient;
        }

        return Unknown;
    }

    /// <summary>
    /// Classifies an application Error into a low-cardinality failure type string.
    /// </summary>
    public static string Classify(Error error)
    {
        return error.Type switch
        {
            ErrorType.Validation => Validation,
            ErrorType.NotFound => Validation,
            ErrorType.Conflict => Concurrency,
            ErrorType.Failure => Transient,
            _ => Unknown,
        };
    }
}
