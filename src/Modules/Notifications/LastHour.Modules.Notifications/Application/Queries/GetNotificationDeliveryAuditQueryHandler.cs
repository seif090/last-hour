using LastHour.BuildingBlocks.Application.Cqrs;
using LastHour.BuildingBlocks.SharedKernel.Results;
using LastHour.Modules.Notifications.Application.Models;
using LastHour.Modules.Notifications.Application.Persistence;
using LastHour.Modules.Notifications.Domain.Notifications;

namespace LastHour.Modules.Notifications.Application.Queries;

/// <summary>
/// Handles <see cref="GetNotificationDeliveryAuditQuery"/> to return administrative audit log projections.
/// </summary>
public sealed class GetNotificationDeliveryAuditQueryHandler : IQueryHandler<GetNotificationDeliveryAuditQuery, IReadOnlyCollection<NotificationDeliveryAuditDto>>
{
    private readonly INotificationDeliveryAuditRepository _auditRepository;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetNotificationDeliveryAuditQueryHandler"/> class.
    /// </summary>
    public GetNotificationDeliveryAuditQueryHandler(INotificationDeliveryAuditRepository auditRepository)
    {
        _auditRepository = auditRepository ?? throw new ArgumentNullException(nameof(auditRepository));
    }

    /// <inheritdoc/>
    public async Task<Result<IReadOnlyCollection<NotificationDeliveryAuditDto>>> Handle(
        GetNotificationDeliveryAuditQuery request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        IReadOnlyList<NotificationDeliveryAuditEntry> audits = await _auditRepository.GetByDeliveryIdAsync(request.DeliveryId, cancellationToken);

        IReadOnlyCollection<NotificationDeliveryAuditDto> dtos = audits
            .Select(e => new NotificationDeliveryAuditDto(
                e.Id,
                e.DeliveryId,
                e.Action,
                e.PreviousStatus,
                e.NewStatus,
                e.ActorId,
                e.Reason,
                e.OccurredAtUtc,
                e.CorrelationId))
            .ToList();

        return Result<IReadOnlyCollection<NotificationDeliveryAuditDto>>.Success(dtos);
    }
}
