using LastHour.BuildingBlocks.Application.Cqrs;
using LastHour.Modules.Notifications.Domain.Notifications;

namespace LastHour.Modules.Notifications.Application.Commands;

/// <summary>
/// Command to explicitly disable a user's notification preference for a specific type and channel.
/// </summary>
public sealed record DisableNotificationPreferenceCommand : ICommand
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DisableNotificationPreferenceCommand"/> class.
    /// </summary>
    public DisableNotificationPreferenceCommand(Guid userId, string notificationType, DeliveryChannel channel)
    {
        UserId = userId;
        NotificationType = notificationType;
        Channel = channel;
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
}
