namespace LastHour.Modules.Notifications.Application.Models;

/// <summary>
/// DTO representing the outcome of an administrative requeue operation for a single delivery item within a batch request.
/// </summary>
public sealed record BatchRequeueItemResultDto
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BatchRequeueItemResultDto"/> class.
    /// </summary>
    /// <param name="deliveryId">The delivery identifier.</param>
    /// <param name="requeued">Whether the delivery was successfully requeued.</param>
    /// <param name="rejectionReason">The reason why requeuing was rejected, or <see langword="null"/> if successful.</param>
    public BatchRequeueItemResultDto(Guid deliveryId, bool requeued, string? rejectionReason = null)
    {
        DeliveryId = deliveryId;
        Requeued = requeued;
        RejectionReason = rejectionReason;
    }

    /// <summary>
    /// Gets the unique delivery identifier.
    /// </summary>
    public Guid DeliveryId { get; }

    /// <summary>
    /// Gets a value indicating whether the delivery was successfully requeued.
    /// </summary>
    public bool Requeued { get; }

    /// <summary>
    /// Gets the rejection reason if the delivery could not be requeued.
    /// </summary>
    public string? RejectionReason { get; }
}
