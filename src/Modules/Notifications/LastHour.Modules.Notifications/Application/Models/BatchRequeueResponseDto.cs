namespace LastHour.Modules.Notifications.Application.Models;

/// <summary>
/// DTO representing the aggregate results of a batch administrative requeue operation.
/// </summary>
public sealed record BatchRequeueResponseDto
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BatchRequeueResponseDto"/> class.
    /// </summary>
    public BatchRequeueResponseDto(
        int requestedCount,
        int requeuedCount,
        int rejectedCount,
        IReadOnlyCollection<BatchRequeueItemResultDto> results)
    {
        RequestedCount = requestedCount;
        RequeuedCount = requeuedCount;
        RejectedCount = rejectedCount;
        Results = results ?? Array.Empty<BatchRequeueItemResultDto>();
    }

    /// <summary>
    /// Gets the total number of delivery identifiers requested for requeue.
    /// </summary>
    public int RequestedCount { get; }

    /// <summary>
    /// Gets the number of deliveries successfully requeued.
    /// </summary>
    public int RequeuedCount { get; }

    /// <summary>
    /// Gets the number of deliveries that could not be requeued.
    /// </summary>
    public int RejectedCount { get; }

    /// <summary>
    /// Gets the detailed per-item results for all requested deliveries.
    /// </summary>
    public IReadOnlyCollection<BatchRequeueItemResultDto> Results { get; }
}
