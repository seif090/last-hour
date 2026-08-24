using LastHour.BuildingBlocks.SharedKernel.Domain;

namespace LastHour.Modules.Notifications.Domain.Notifications;

/// <summary>
/// Aggregate root representing a user's notification delivery preference for a specific notification type and channel.
/// </summary>
public sealed class NotificationPreference : AggregateRoot<Guid>
{
    private NotificationPreference()
    {
    }

    private NotificationPreference(
        Guid id,
        Guid userId,
        string notificationType,
        DeliveryChannel channel,
        bool isEnabled,
        DateTime createdAtUtc)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Identifier cannot be empty.", nameof(id));
        }

        if (userId == Guid.Empty)
        {
            throw new ArgumentException("User identifier cannot be empty.", nameof(userId));
        }

        if (string.IsNullOrWhiteSpace(notificationType))
        {
            throw new ArgumentException("Notification type cannot be empty.", nameof(notificationType));
        }

        Id = id;
        UserId = userId;
        NotificationType = notificationType.Trim();
        Channel = channel;
        IsEnabled = isEnabled;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = createdAtUtc;
    }

    /// <summary>
    /// Gets the user identifier.
    /// </summary>
    public Guid UserId { get; private set; }

    /// <summary>
    /// Gets the notification type.
    /// </summary>
    public string NotificationType { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the target delivery channel.
    /// </summary>
    public DeliveryChannel Channel { get; private set; }

    /// <summary>
    /// Gets a value indicating whether notifications are enabled for this channel.
    /// </summary>
    public bool IsEnabled { get; private set; }

    /// <summary>
    /// Gets the UTC creation timestamp.
    /// </summary>
    public DateTime CreatedAtUtc { get; private set; }

    /// <summary>
    /// Gets the UTC update timestamp.
    /// </summary>
    public DateTime UpdatedAtUtc { get; private set; }

    /// <summary>
    /// Factory method to create a new notification preference.
    /// </summary>
    public static NotificationPreference Create(
        Guid userId,
        string notificationType,
        DeliveryChannel channel,
        bool isEnabled,
        DateTime createdAtUtc)
    {
        return new NotificationPreference(Guid.NewGuid(), userId, notificationType, channel, isEnabled, createdAtUtc);
    }

    /// <summary>
    /// Enables notification delivery for this channel.
    /// </summary>
    public void Enable(DateTime utcNow)
    {
        IsEnabled = true;
        UpdatedAtUtc = utcNow;
    }

    /// <summary>
    /// Disables notification delivery for this channel.
    /// </summary>
    public void Disable(DateTime utcNow)
    {
        IsEnabled = false;
        UpdatedAtUtc = utcNow;
    }

    /// <summary>
    /// Updates the delivery channel.
    /// </summary>
    public void ChangeChannel(DeliveryChannel channel, DateTime utcNow)
    {
        Channel = channel;
        UpdatedAtUtc = utcNow;
    }
}
