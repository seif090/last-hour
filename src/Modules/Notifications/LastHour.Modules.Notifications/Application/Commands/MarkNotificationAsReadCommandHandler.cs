using LastHour.BuildingBlocks.Application.Contracts;
using LastHour.BuildingBlocks.Application.Cqrs;
using LastHour.BuildingBlocks.SharedKernel.Results;
using LastHour.Modules.Notifications.Application.Persistence;
using LastHour.Modules.Notifications.Domain.Notifications;

namespace LastHour.Modules.Notifications.Application.Commands;

/// <summary>
/// Handles marking a single notification as read by its recipient.
/// </summary>
public sealed class MarkNotificationAsReadCommandHandler : ICommandHandler<MarkNotificationAsReadCommand>
{
    private readonly INotificationRepository _notificationRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    /// <summary>
    /// Initializes a new instance of the <see cref="MarkNotificationAsReadCommandHandler"/> class.
    /// </summary>
    /// <param name="notificationRepository">The notification repository.</param>
    /// <param name="unitOfWork">The unit of work.</param>
    /// <param name="clock">The clock provider.</param>
    public MarkNotificationAsReadCommandHandler(
        INotificationRepository notificationRepository,
        IUnitOfWork unitOfWork,
        IClock clock)
    {
        _notificationRepository = notificationRepository ?? throw new ArgumentNullException(nameof(notificationRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
    }

    /// <inheritdoc/>
    public async Task<Result> Handle(MarkNotificationAsReadCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.UserId == Guid.Empty)
        {
            return Result.Failure(Error.Unauthorized(
                "AuthenticationFailed",
                "The authenticated user context is required."));
        }

        Notification? notification = await _notificationRepository.GetByIdTrackedAsync(request.NotificationId, cancellationToken);

        // Treat notifications belonging to other recipients as not found to avoid authorization leakage
        if (notification is null || notification.RecipientId != request.UserId)
        {
            return Result.Failure(Error.NotFound(
                "NotificationNotFound",
                "The specified notification was not found."));
        }

        if (!notification.IsRead)
        {
            notification.MarkAsRead(_clock.UtcNow);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return Result.Success();
    }
}
