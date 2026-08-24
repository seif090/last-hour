using LastHour.BuildingBlocks.Application.Cqrs;
using LastHour.BuildingBlocks.Application.Models;
using LastHour.BuildingBlocks.SharedKernel.Results;
using LastHour.Modules.Notifications.Application.Models;
using LastHour.Modules.Notifications.Application.Persistence;
using LastHour.Modules.Notifications.Domain.Notifications;

namespace LastHour.Modules.Notifications.Application.Queries;

/// <summary>
/// Handles <see cref="GetNotificationDeliveriesQuery"/> to return paginated operational summary projections.
/// </summary>
public sealed class GetNotificationDeliveriesQueryHandler : IQueryHandler<GetNotificationDeliveriesQuery, PagedResult<NotificationDeliverySummaryDto>>
{
    private readonly INotificationDeliveryRepository _deliveryRepository;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetNotificationDeliveriesQueryHandler"/> class.
    /// </summary>
    public GetNotificationDeliveriesQueryHandler(INotificationDeliveryRepository deliveryRepository)
    {
        _deliveryRepository = deliveryRepository ?? throw new ArgumentNullException(nameof(deliveryRepository));
    }

    /// <inheritdoc/>
    public async Task<Result<PagedResult<NotificationDeliverySummaryDto>>> Handle(
        GetNotificationDeliveriesQuery request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        PagedResult<NotificationDelivery> pagedDeliveries = await _deliveryRepository.GetPagedDeliveriesAsync(
            request.Status,
            request.Channel,
            request.NotificationId,
            request.CreatedFromUtc,
            request.CreatedToUtc,
            request.PageNumber,
            request.PageSize,
            cancellationToken);

        IReadOnlyList<NotificationDeliverySummaryDto> dtos = pagedDeliveries.Items
            .Select(d => new NotificationDeliverySummaryDto(
                d.Id,
                d.NotificationId,
                d.Channel,
                d.Status,
                d.RetryCount,
                d.MaximumRetryCount,
                d.NextAttemptAtUtc,
                d.LockExpiresAtUtc,
                d.CreatedAtUtc,
                d.UpdatedAtUtc))
            .ToList();

        PagedResult<NotificationDeliverySummaryDto> result = new PagedResult<NotificationDeliverySummaryDto>(
            dtos,
            pagedDeliveries.PageNumber,
            pagedDeliveries.PageSize,
            pagedDeliveries.TotalCount);

        return Result<PagedResult<NotificationDeliverySummaryDto>>.Success(result);
    }
}
