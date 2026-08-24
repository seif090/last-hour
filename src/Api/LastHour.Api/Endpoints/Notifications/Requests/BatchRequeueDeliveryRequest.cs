namespace LastHour.Api.Endpoints.Notifications.Requests;

/// <summary>
/// HTTP request contract for triggering a batch administrative requeue of multiple notification deliveries.
/// </summary>
public sealed record BatchRequeueDeliveryRequest
{
    /// <summary>
    /// Gets the collection of target delivery identifiers (max 100).
    /// </summary>
    public IReadOnlyCollection<Guid> DeliveryIds { get; init; } = Array.Empty<Guid>();

    /// <summary>
    /// Gets the administrative justification or reason for requeuing the deliveries.
    /// </summary>
    public string Reason { get; init; } = string.Empty;

    /// <summary>
    /// Gets an optional correlation identifier for administrative tracking.
    /// </summary>
    public string? CorrelationId { get; init; }
}
