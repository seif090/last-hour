using LastHour.BuildingBlocks.Application.Contracts;
using LastHour.BuildingBlocks.Application.Cqrs;
using LastHour.BuildingBlocks.SharedKernel.Results;
using LastHour.Modules.Notifications.Application.Delivery;
using LastHour.Modules.Notifications.Application.Models;
using LastHour.Modules.Notifications.Application.Persistence;
using LastHour.Modules.Notifications.Domain.Notifications;
using Microsoft.EntityFrameworkCore;

namespace LastHour.Modules.Notifications.Application.Commands;

/// <summary>
/// Handles <see cref="RequeueNotificationDeliveriesCommand"/> to execute batch administrative delivery requeues with partial success support.
/// </summary>
public sealed class RequeueNotificationDeliveriesCommandHandler : ICommandHandler<RequeueNotificationDeliveriesCommand, BatchRequeueResponseDto>
{
    private readonly INotificationDeliveryRepository _deliveryRepository;
    private readonly INotificationDeliveryAuditRepository _auditRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    /// <summary>
    /// Initializes a new instance of the <see cref="RequeueNotificationDeliveriesCommandHandler"/> class.
    /// </summary>
    public RequeueNotificationDeliveriesCommandHandler(
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
    public async Task<Result<BatchRequeueResponseDto>> Handle(
        RequeueNotificationDeliveriesCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.AdminUserId == Guid.Empty)
        {
            return Result<BatchRequeueResponseDto>.Failure(Error.Unauthorized(
                "AuthenticationFailed",
                "Administrative actor identity is required."));
        }

        var results = new List<BatchRequeueItemResultDto>();
        int requeuedCount = 0;
        int rejectedCount = 0;
        DateTime now = _clock.UtcNow;

        foreach (Guid deliveryId in request.DeliveryIds.Distinct())
        {
            NotificationDelivery? delivery = await _deliveryRepository.GetByIdTrackedAsync(deliveryId, cancellationToken);
            if (delivery is null)
            {
                results.Add(new BatchRequeueItemResultDto(deliveryId, requeued: false, rejectionReason: "Notification delivery not found."));
                rejectedCount++;
                continue;
            }

            DeliveryStatus previousStatus = delivery.Status;

            try
            {
                delivery.RequeueByAdmin(request.AdminUserId, request.Reason, now);
            }
            catch (InvalidOperationException ex)
            {
                results.Add(new BatchRequeueItemResultDto(deliveryId, requeued: false, rejectionReason: ex.Message));
                rejectedCount++;
                continue;
            }

            var auditEntry = NotificationDeliveryAuditEntry.Create(
                delivery.Id,
                NotificationOperationsAction.BatchRequeued,
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
                results.Add(new BatchRequeueItemResultDto(deliveryId, requeued: true));
                requeuedCount++;
            }
            catch (DbUpdateConcurrencyException ex)
            {
                foreach (var entry in ex.Entries)
                {
                    entry.State = Microsoft.EntityFrameworkCore.EntityState.Detached;
                }

                results.Add(new BatchRequeueItemResultDto(
                    deliveryId,
                    requeued: false,
                    rejectionReason: "Concurrent worker lease or status conflict occurred. Delivery skipped."));
                rejectedCount++;
            }
        }

        var response = new BatchRequeueResponseDto(
            request.DeliveryIds.Count,
            requeuedCount,
            rejectedCount,
            results);

        return Result<BatchRequeueResponseDto>.Success(response);
    }
}
