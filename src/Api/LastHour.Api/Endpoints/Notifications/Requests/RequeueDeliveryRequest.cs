namespace LastHour.Api.Endpoints.Notifications.Requests;

/// <summary>
/// HTTP request contract for triggering an administrative requeue of a single notification delivery.
/// </summary>
public sealed record RequeueDeliveryRequest
{
    /// <summary>
    /// Gets the administrative justification or reason for requeuing the delivery.
    /// </summary>
    public string Reason { get; init; } = string.Empty;

    /// <summary>
    /// Gets an optional correlation identifier for administrative tracking.
    /// </summary>
    public string? CorrelationId { get; init; }
}
