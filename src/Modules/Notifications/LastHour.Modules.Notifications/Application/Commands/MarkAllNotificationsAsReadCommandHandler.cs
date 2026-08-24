using LastHour.BuildingBlocks.Application.Contracts;
using LastHour.BuildingBlocks.Application.Cqrs;
using LastHour.BuildingBlocks.SharedKernel.Results;
using LastHour.Modules.Notifications.Application.Models;
using LastHour.Modules.Notifications.Application.Persistence;

namespace LastHour.Modules.Notifications.Application.Commands;

/// <summary>
/// Handles marking all unread notifications as read for the authenticated recipient.
/// </summary>
public sealed class MarkAllNotificationsAsReadCommandHandler : ICommandHandler<MarkAllNotificationsAsReadCommand, MarkAllNotificationsAsReadResponse>
{
    private readonly INotificationRepository _notificationRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    /// <summary>
    /// Initializes a new instance of the <see cref="MarkAllNotificationsAsReadCommandHandler"/> class.
    /// </summary>
    /// <param name="notificationRepository">The notification repository.</param>
    /// <param name="unitOfWork">The unit of work.</param>
    /// <param name="clock">The clock provider.</param>
    public MarkAllNotificationsAsReadCommandHandler(
        INotificationRepository notificationRepository,
        IUnitOfWork unitOfWork,
        IClock clock)
    {
        _notificationRepository = notificationRepository ?? throw new ArgumentNullException(nameof(notificationRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
    }

    /// <inheritdoc/>
    public async Task<Result<MarkAllNotificationsAsReadResponse>> Handle(MarkAllNotificationsAsReadCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.UserId == Guid.Empty)
        {
            return Result<MarkAllNotificationsAsReadResponse>.Failure(Error.Unauthorized(
                "AuthenticationFailed",
                "The authenticated user context is required."));
        }

        int updatedCount = await _notificationRepository.MarkAllAsReadAsync(request.UserId, _clock.UtcNow, cancellationToken);
        if (updatedCount > 0)
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return Result<MarkAllNotificationsAsReadResponse>.Success(new MarkAllNotificationsAsReadResponse(updatedCount));
    }
}
