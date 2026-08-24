using LastHour.BuildingBlocks.Application.Cqrs;
using LastHour.Modules.Notifications.Application.Models;

namespace LastHour.Modules.Notifications.Application.Commands;

/// <summary>
/// Command to mark all unread notifications as read for the authenticated recipient.
/// </summary>
public sealed record MarkAllNotificationsAsReadCommand : ICommand<MarkAllNotificationsAsReadResponse>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MarkAllNotificationsAsReadCommand"/> class.
    /// </summary>
    /// <param name="userId">The unique identifier of the authenticated user performing the operation.</param>
    public MarkAllNotificationsAsReadCommand(Guid userId)
    {
        UserId = userId;
    }

    /// <summary>
    /// Gets the unique identifier of the authenticated user.
    /// </summary>
    public Guid UserId { get; }
}
