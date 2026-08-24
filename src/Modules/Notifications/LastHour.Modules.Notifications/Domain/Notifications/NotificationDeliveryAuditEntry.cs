using LastHour.BuildingBlocks.SharedKernel.Domain;

namespace LastHour.Modules.Notifications.Domain.Notifications;

/// <summary>
/// Entity representing an append-only operational audit record for administrative delivery actions.
/// </summary>
public sealed class NotificationDeliveryAuditEntry : AggregateRoot<Guid>
{
    private NotificationDeliveryAuditEntry()
    {
    }

    private NotificationDeliveryAuditEntry(
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
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Identifier cannot be empty.", nameof(id));
        }

        if (deliveryId == Guid.Empty)
        {
            throw new ArgumentException("Delivery identifier cannot be empty.", nameof(deliveryId));
        }

        if (string.IsNullOrWhiteSpace(action))
        {
            throw new ArgumentException("Audit action cannot be empty.", nameof(action));
        }

        if (actorId == Guid.Empty)
        {
            throw new ArgumentException("Actor identifier cannot be empty.", nameof(actorId));
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new ArgumentException("Reason cannot be empty.", nameof(reason));
        }

        Id = id;
        DeliveryId = deliveryId;
        Action = action.Trim();
        PreviousStatus = previousStatus;
        NewStatus = newStatus;
        ActorId = actorId;
        Reason = reason.Trim();
        OccurredAtUtc = occurredAtUtc;
        CorrelationId = string.IsNullOrWhiteSpace(correlationId) ? null : correlationId.Trim();
    }

    /// <summary>
    /// Gets the target delivery identifier.
    /// </summary>
    public Guid DeliveryId { get; private set; }

    /// <summary>
    /// Gets the operational action performed (e.g. Requeued, BatchRequeued).
    /// </summary>
    public string Action { get; private set; } = null!;

    /// <summary>
    /// Gets the delivery status before the action occurred.
    /// </summary>
    public DeliveryStatus PreviousStatus { get; private set; }

    /// <summary>
    /// Gets the delivery status resulting from the action.
    /// </summary>
    public DeliveryStatus NewStatus { get; private set; }

    /// <summary>
    /// Gets the authenticated actor identifier performing the administrative operation.
    /// </summary>
    public Guid ActorId { get; private set; }

    /// <summary>
    /// Gets the justification or reason provided for the administrative action.
    /// </summary>
    public string Reason { get; private set; } = null!;

    /// <summary>
    /// Gets the UTC timestamp when the operational action occurred.
    /// </summary>
    public DateTime OccurredAtUtc { get; private set; }

    /// <summary>
    /// Gets the optional request or workflow correlation identifier.
    /// </summary>
    public string? CorrelationId { get; private set; }

    /// <summary>
    /// Factory method to create an append-only operational audit entry.
    /// </summary>
    public static NotificationDeliveryAuditEntry Create(
        Guid deliveryId,
        string action,
        DeliveryStatus previousStatus,
        DeliveryStatus newStatus,
        Guid actorId,
        string reason,
        DateTime occurredAtUtc,
        string? correlationId = null)
    {
        return new NotificationDeliveryAuditEntry(
            Guid.NewGuid(),
            deliveryId,
            action,
            previousStatus,
            newStatus,
            actorId,
            reason,
            occurredAtUtc,
            correlationId);
    }
}
