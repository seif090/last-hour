using LastHour.BuildingBlocks.Application.Cqrs;
using LastHour.Modules.Notifications.Application.Models;

namespace LastHour.Modules.Notifications.Application.Commands;

/// <summary>
/// Command for an authenticated administrator to batch-requeue multiple notification deliveries safely.
/// </summary>
public sealed record RequeueNotificationDeliveriesCommand : ICommand<BatchRequeueResponseDto>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RequeueNotificationDeliveriesCommand"/> class.
    /// </summary>
    /// <param name="deliveryIds">The collection of target delivery identifiers (1 to 100).</param>
    /// <param name="adminUserId">The unique identifier of the authenticated administrative actor.</param>
    /// <param name="reason">The administrative justification for requeuing.</param>
    /// <param name="correlationId">Optional request correlation identifier.</param>
    public RequeueNotificationDeliveriesCommand(
        IReadOnlyCollection<Guid> deliveryIds,
        Guid adminUserId,
        string reason,
        string? correlationId = null)
    {
        DeliveryIds = deliveryIds ?? Array.Empty<Guid>();
        AdminUserId = adminUserId;
        Reason = reason;
        CorrelationId = correlationId;
    }

    /// <summary>
    /// Gets the target delivery identifiers.
    /// </summary>
    public IReadOnlyCollection<Guid> DeliveryIds { get; }

    /// <summary>
    /// Gets the unique identifier of the authenticated administrative actor.
    /// </summary>
    public Guid AdminUserId { get; }

    /// <summary>
    /// Gets the administrative justification for requeuing.
    /// </summary>
    public string Reason { get; }

    /// <summary>
    /// Gets the optional request correlation identifier.
    /// </summary>
    public string? CorrelationId { get; }
}
