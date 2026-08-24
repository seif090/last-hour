using LastHour.Modules.Notifications.Domain.Notifications;

namespace LastHour.Modules.Notifications.Application.Models;

/// <summary>
/// DTO representing an administrative audit log entry for a notification delivery.
/// </summary>
public sealed record NotificationDeliveryAuditDto
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NotificationDeliveryAuditDto"/> class.
    /// </summary>
    public NotificationDeliveryAuditDto(
        Guid id,
        Guid deliveryId,
        string action,
        DeliveryStatus previousStatus,
        DeliveryStatus newStatus,
        Guid actorId,
        string reason,
        DateTime occurredAtUtc,
        string? correlationId)
    {
        Id = id;
        DeliveryId = deliveryId;
        Action = action;
        PreviousStatus = previousStatus;
        NewStatus = newStatus;
        ActorId = actorId;
        Reason = reason;
        OccurredAtUtc = occurredAtUtc;
        CorrelationId = correlationId;
    }

    /// <summary>
    /// Gets the unique audit entry identifier.
    /// </summary>
    public Guid Id { get; }

    /// <summary>
    /// Gets the target delivery identifier.
    /// </summary>
    public Guid DeliveryId { get; }

    /// <summary>
    /// Gets the administrative action code (e.g. Requeued, BatchRequeued).
    /// </summary>
    public string Action { get; }

    /// <summary>
    /// Gets the delivery status before the action.
    /// </summary>
    public DeliveryStatus PreviousStatus { get; }

    /// <summary>
    /// Gets the delivery status resulting from the action.
    /// </summary>
    public DeliveryStatus NewStatus { get; }

    /// <summary>
    /// Gets the authenticated administrative actor identifier.
    /// </summary>
    public Guid ActorId { get; }

    /// <summary>
    /// Gets the administrative justification or reason provided.
    /// </summary>
    public string Reason { get; }

    /// <summary>
    /// Gets the UTC timestamp when the action occurred.
    /// </summary>
    public DateTime OccurredAtUtc { get; }

    /// <summary>
    /// Gets the optional workflow or request correlation identifier.
    /// </summary>
    public string? CorrelationId { get; }
}
