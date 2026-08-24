using LastHour.BuildingBlocks.Application.Cqrs;
using LastHour.BuildingBlocks.Application.Models;
using LastHour.BuildingBlocks.SharedKernel.Results;
using LastHour.Modules.Notifications.Application.Models;
using LastHour.Modules.Notifications.Application.Persistence;
using LastHour.Modules.Notifications.Domain.Notifications;

namespace LastHour.Modules.Notifications.Application.Queries;

/// <summary>
/// Handles the retrieval of a paged list of notifications for the authenticated user.
/// </summary>
public sealed class GetMyNotificationsQueryHandler : IQueryHandler<GetMyNotificationsQuery, PagedResult<NotificationDto>>
{
    private readonly INotificationRepository _notificationRepository;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetMyNotificationsQueryHandler"/> class.
    /// </summary>
    /// <param name="notificationRepository">The notification repository.</param>
    public GetMyNotificationsQueryHandler(INotificationRepository notificationRepository)
    {
        _notificationRepository = notificationRepository ?? throw new ArgumentNullException(nameof(notificationRepository));
    }

    /// <inheritdoc/>
    public async Task<Result<PagedResult<NotificationDto>>> Handle(GetMyNotificationsQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.UserId == Guid.Empty)
        {
            return Result<PagedResult<NotificationDto>>.Failure(Error.Unauthorized(
                "AuthenticationFailed",
                "The authenticated user context is required to query notifications."));
        }

        PagedResult<Notification> paged = await _notificationRepository.GetPagedByRecipientIdAsync(
            request.UserId,
            request.IsRead,
            request.Type,
            request.PageNumber,
            request.PageSize,
            cancellationToken);

        IReadOnlyList<NotificationDto> dtos = paged.Items
            .Select(n => new NotificationDto(
                n.Id,
                n.Type,
                n.Title,
                n.Body,
                n.IsRead,
                n.CreatedAtUtc,
                n.ReadAtUtc))
            .ToList();

        PagedResult<NotificationDto> result = new PagedResult<NotificationDto>(dtos, paged.PageNumber, paged.PageSize, paged.TotalCount);
        return Result<PagedResult<NotificationDto>>.Success(result);
    }
}
