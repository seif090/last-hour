using LastHour.BuildingBlocks.Application.Cqrs;
using LastHour.Modules.Notifications.Domain.Notifications;

namespace LastHour.Modules.Notifications.Application.Commands;

/// <summary>
/// Command to update (upsert) a user's notification preference for a specific type and channel.
/// </summary>
public sealed record UpdateNotificationPreferenceCommand : ICommand
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateNotificationPreferenceCommand"/> class.
    /// </summary>
    public UpdateNotificationPreferenceCommand(Guid userId, string notificationType, DeliveryChannel channel, bool isEnabled)
    {
        UserId = userId;
        NotificationType = notificationType;
        Channel = channel;
        IsEnabled = isEnabled;
    }

    /// <summary>
    /// Gets the unique identifier of the authenticated user.
    /// </summary>
    public Guid UserId { get; }

    /// <summary>
    /// Gets the notification type.
    /// </summary>
    public string NotificationType { get; }

    /// <summary>
    /// Gets the delivery channel.
    /// </summary>
    public DeliveryChannel Channel { get; }

    /// <summary>
    /// Gets a value indicating whether notification delivery is enabled for this channel.
    /// </summary>
    public bool IsEnabled { get; }
}
