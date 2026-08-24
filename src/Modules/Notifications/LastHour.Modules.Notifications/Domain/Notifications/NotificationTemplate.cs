using LastHour.BuildingBlocks.SharedKernel.Domain;

namespace LastHour.Modules.Notifications.Domain.Notifications;

/// <summary>
/// Aggregate root representing a provider-neutral versioned notification template.
/// </summary>
public sealed class NotificationTemplate : AggregateRoot<Guid>
{
    private NotificationTemplate()
    {
    }

    private NotificationTemplate(
        Guid id,
        string templateKey,
        string notificationType,
        DeliveryChannel channel,
        int version,
        string body,
        string? subject,
        bool isActive,
        DateTime createdAtUtc)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Identifier cannot be empty.", nameof(id));
        }

        if (string.IsNullOrWhiteSpace(templateKey))
        {
            throw new ArgumentException("Template key cannot be empty.", nameof(templateKey));
        }

        if (string.IsNullOrWhiteSpace(notificationType))
        {
            throw new ArgumentException("Notification type cannot be empty.", nameof(notificationType));
        }

        if (version <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(version), "Template version must be greater than zero.");
        }

        if (string.IsNullOrWhiteSpace(body))
        {
            throw new ArgumentException("Template body cannot be empty.", nameof(body));
        }

        Id = id;
        TemplateKey = templateKey.Trim();
        NotificationType = notificationType.Trim();
        Channel = channel;
        Version = version;
        Body = body;
        Subject = subject?.Trim();
        IsActive = isActive;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = createdAtUtc;
    }

    /// <summary>
    /// Gets the logical template key identifier.
    /// </summary>
    public string TemplateKey { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the notification type.
    /// </summary>
    public string NotificationType { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the target delivery channel.
    /// </summary>
    public DeliveryChannel Channel { get; private set; }

    /// <summary>
    /// Gets the template version number.
    /// </summary>
    public int Version { get; private set; }

    /// <summary>
    /// Gets the optional subject line (used for Email channel).
    /// </summary>
    public string? Subject { get; private set; }

    /// <summary>
    /// Gets the template body containing placeholder tokens.
    /// </summary>
    public string Body { get; private set; } = string.Empty;

    /// <summary>
    /// Gets a value indicating whether this template version is active.
    /// </summary>
    public bool IsActive { get; private set; }

    /// <summary>
    /// Gets the UTC creation timestamp.
    /// </summary>
    public DateTime CreatedAtUtc { get; private set; }

    /// <summary>
    /// Gets the UTC update timestamp.
    /// </summary>
    public DateTime UpdatedAtUtc { get; private set; }

    /// <summary>
    /// Factory method to create a new versioned notification template.
    /// </summary>
    public static NotificationTemplate Create(
        string templateKey,
        string notificationType,
        DeliveryChannel channel,
        int version,
        string body,
        string? subject = null,
        bool isActive = true,
        DateTime? createdAtUtc = null)
    {
        DateTime now = createdAtUtc ?? DateTime.UtcNow;
        return new NotificationTemplate(Guid.NewGuid(), templateKey, notificationType, channel, version, body, subject, isActive, now);
    }

    /// <summary>
    /// Activates this template version.
    /// </summary>
    public void Activate(DateTime utcNow)
    {
        IsActive = true;
        UpdatedAtUtc = utcNow;
    }

    /// <summary>
    /// Deactivates this template version.
    /// </summary>
    public void Deactivate(DateTime utcNow)
    {
        IsActive = false;
        UpdatedAtUtc = utcNow;
    }

    /// <summary>
    /// Updates the template body and subject content.
    /// </summary>
    public void UpdateBody(string body, string? subject, DateTime utcNow)
    {
        if (string.IsNullOrWhiteSpace(body))
        {
            throw new ArgumentException("Template body cannot be empty.", nameof(body));
        }

        Body = body;
        Subject = subject?.Trim();
        UpdatedAtUtc = utcNow;
    }
}
