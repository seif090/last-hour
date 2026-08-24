using LastHour.BuildingBlocks.Application.Cqrs;

namespace LastHour.Modules.Notifications.Application.Commands;

/// <summary>
/// Command to mark a single notification as read by the authenticated recipient.
/// </summary>
public sealed record MarkNotificationAsReadCommand : ICommand
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MarkNotificationAsReadCommand"/> class.
    /// </summary>
    /// <param name="notificationId">The unique identifier of the notification to mark as read.</param>
    /// <param name="userId">The unique identifier of the authenticated user performing the operation.</param>
    public MarkNotificationAsReadCommand(Guid notificationId, Guid userId)
    {
        NotificationId = notificationId;
        UserId = userId;
    }

    /// <summary>
    /// Gets the unique identifier of the notification to mark as read.
    /// </summary>
    public Guid NotificationId { get; }

    /// <summary>
    /// Gets the unique identifier of the authenticated user.
    /// </summary>
    public Guid UserId { get; }
}
