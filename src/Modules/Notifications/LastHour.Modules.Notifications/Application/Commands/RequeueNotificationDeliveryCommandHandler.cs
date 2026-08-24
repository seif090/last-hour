using LastHour.BuildingBlocks.Application.Contracts;
using LastHour.BuildingBlocks.Application.Cqrs;
using LastHour.BuildingBlocks.SharedKernel.Results;
using LastHour.Modules.Notifications.Application.Delivery;
using LastHour.Modules.Notifications.Application.Persistence;
using LastHour.Modules.Notifications.Domain.Notifications;
using Microsoft.EntityFrameworkCore;

namespace LastHour.Modules.Notifications.Application.Commands;

/// <summary>
/// Handles <see cref="RequeueNotificationDeliveryCommand"/> to execute safe administrative single-delivery requeue.
/// </summary>
public sealed class RequeueNotificationDeliveryCommandHandler : ICommandHandler<RequeueNotificationDeliveryCommand>
{
    private readonly INotificationDeliveryRepository _deliveryRepository;
    private readonly INotificationDeliveryAuditRepository _auditRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    /// <summary>
    /// Initializes a new instance of the <see cref="RequeueNotificationDeliveryCommandHandler"/> class.
    /// </summary>
    public RequeueNotificationDeliveryCommandHandler(
        INotificationDeliveryRepository deliveryRepository,
        INotificationDeliveryAuditRepository auditRepository,
        IUnitOfWork unitOfWork,
        IClock clock)
    {
        _deliveryRepository = deliveryRepository ?? throw new ArgumentNullException(nameof(deliveryRepository));
        _auditRepository = auditRepository ?? throw new ArgumentNullException(nameof(auditRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
    }

    /// <inheritdoc/>
    public async Task<Result> Handle(RequeueNotificationDeliveryCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.AdminUserId == Guid.Empty)
        {
            return Result.Failure(Error.Unauthorized(
                "AuthenticationFailed",
                "Administrative actor identity is required."));
        }

        NotificationDelivery? delivery = await _deliveryRepository.GetByIdTrackedAsync(request.DeliveryId, cancellationToken);
        if (delivery is null)
        {
            return Result.Failure(Error.NotFound(
                "NotificationDeliveryNotFound",
                $"The specified notification delivery with ID '{request.DeliveryId}' was not found."));
        }

        DeliveryStatus previousStatus = delivery.Status;
        DateTime now = _clock.UtcNow;

        try
        {
            delivery.RequeueByAdmin(request.AdminUserId, request.Reason, now);
        }
        catch (InvalidOperationException ex)
        {
            return Result.Failure(Error.Conflict("RequeueRejected", ex.Message));
        }

        var auditEntry = NotificationDeliveryAuditEntry.Create(
            delivery.Id,
            NotificationOperationsAction.Requeued,
            previousStatus,
            DeliveryStatus.Pending,
            request.AdminUserId,
            request.Reason,
            now,
            request.CorrelationId);

        await _auditRepository.AddAsync(auditEntry, cancellationToken);

        try
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Result.Failure(Error.Conflict(
                "ConcurrencyConflict",
                "The notification delivery was modified by a concurrent process. Requeue rejected."));
        }

        return Result.Success();
    }
}
