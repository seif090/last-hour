namespace LastHour.Modules.Notifications.Application.Models;

/// <summary>
/// Represents a notification data transfer object returned by application queries and API endpoints.
/// </summary>
public sealed record NotificationDto
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NotificationDto"/> class.
    /// </summary>
    /// <param name="id">The unique identifier of the notification.</param>
    /// <param name="type">The notification classification type.</param>
    /// <param name="title">The title or subject of the notification.</param>
    /// <param name="body">The content or body of the notification.</param>
    /// <param name="isRead">Indicates whether the notification has been marked as read.</param>
    /// <param name="createdAtUtc">The UTC timestamp when the notification was created.</param>
    /// <param name="readAtUtc">The UTC timestamp when the notification was marked as read, or <see langword="null"/> if unread.</param>
    public NotificationDto(
        Guid id,
        string type,
        string title,
        string body,
        bool isRead,
        DateTime createdAtUtc,
        DateTime? readAtUtc)
    {
        Id = id;
        Type = type;
        Title = title;
        Body = body;
        IsRead = isRead;
        CreatedAtUtc = createdAtUtc;
        ReadAtUtc = readAtUtc;
    }

    /// <summary>
    /// Gets the unique identifier of the notification.
    /// </summary>
    public Guid Id { get; }

    /// <summary>
    /// Gets the notification classification type.
    /// </summary>
    public string Type { get; }

    /// <summary>
    /// Gets the title or subject of the notification.
    /// </summary>
    public string Title { get; }

    /// <summary>
    /// Gets the content or body of the notification.
    /// </summary>
    public string Body { get; }

    /// <summary>
    /// Gets a value indicating whether the notification has been marked as read.
    /// </summary>
    public bool IsRead { get; }

    /// <summary>
    /// Gets the UTC timestamp when the notification was created.
    /// </summary>
    public DateTime CreatedAtUtc { get; }

    /// <summary>
    /// Gets the UTC timestamp when the notification was marked as read, or <see langword="null"/> if unread.
    /// </summary>
    public DateTime? ReadAtUtc { get; }
}
