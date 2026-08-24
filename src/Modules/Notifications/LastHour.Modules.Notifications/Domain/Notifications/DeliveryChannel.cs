namespace LastHour.Modules.Notifications.Domain.Notifications;

/// <summary>
/// Represents the supported delivery channels for a notification.
/// </summary>
public enum DeliveryChannel
{
    /// <summary>
    /// In-app notification channel.
    /// </summary>
    InApp = 1,

    /// <summary>
    /// Email notification channel.
    /// </summary>
    Email = 2,

    /// <summary>
    /// SMS notification channel.
    /// </summary>
    Sms = 3,

    /// <summary>
    /// Push notification channel.
    /// </summary>
    Push = 4,
}
