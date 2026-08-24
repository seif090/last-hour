using LastHour.BuildingBlocks.Application.Cqrs;
using LastHour.BuildingBlocks.SharedKernel.Results;
using LastHour.Modules.Notifications.Application.Models;
using LastHour.Modules.Notifications.Application.Persistence;
using LastHour.Modules.Notifications.Domain.Notifications;

namespace LastHour.Modules.Notifications.Application.Queries;

/// <summary>
/// Handles <see cref="GetNotificationDeliveryDetailsQuery"/> to return detailed operational projections.
/// </summary>
public sealed class GetNotificationDeliveryDetailsQueryHandler : IQueryHandler<GetNotificationDeliveryDetailsQuery, NotificationDeliveryDetailDto>
{
    private readonly INotificationDeliveryRepository _deliveryRepository;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetNotificationDeliveryDetailsQueryHandler"/> class.
    /// </summary>
    public GetNotificationDeliveryDetailsQueryHandler(INotificationDeliveryRepository deliveryRepository)
    {
        _deliveryRepository = deliveryRepository ?? throw new ArgumentNullException(nameof(deliveryRepository));
    }

    /// <inheritdoc/>
    public async Task<Result<NotificationDeliveryDetailDto>> Handle(
        GetNotificationDeliveryDetailsQuery request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        NotificationDelivery? delivery = await _deliveryRepository.GetByIdAsync(request.DeliveryId, cancellationToken);
        if (delivery is null)
        {
            return Result<NotificationDeliveryDetailDto>.Failure(Error.NotFound(
                "NotificationDeliveryNotFound",
                $"The specified notification delivery with ID '{request.DeliveryId}' was not found."));
        }

        var dto = new NotificationDeliveryDetailDto(
            delivery.Id,
            delivery.NotificationId,
            delivery.Channel,
            delivery.Status,
            delivery.RetryCount,
            delivery.MaximumRetryCount,
            delivery.NextAttemptAtUtc,
            delivery.LockExpiresAtUtc,
            delivery.LastError,
            delivery.CreatedAtUtc,
            delivery.UpdatedAtUtc);

        return Result<NotificationDeliveryDetailDto>.Success(dto);
    }
}
