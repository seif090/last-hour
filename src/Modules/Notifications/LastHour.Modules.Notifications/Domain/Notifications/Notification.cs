using LastHour.BuildingBlocks.SharedKernel.Domain;

namespace LastHour.Modules.Notifications.Domain.Notifications;

/// <summary>
/// Represents a durable notification aggregate root within the Notifications module.
/// </summary>
public sealed class Notification : AggregateRoot<Guid>, IAuditableEntity
{
    /// <summary>
    /// The maximum length of the notification type identifier.
    /// </summary>
    public const int MaxTypeLength = 100;

    /// <summary>
    /// The maximum length of the notification recipient identifier.
    /// </summary>
    public const int MaxRecipientLength = 256;

    /// <summary>
    /// The maximum length of the notification title.
    /// </summary>
    public const int MaxTitleLength = 200;

    /// <summary>
    /// The maximum length of the notification body text.
    /// </summary>
    public const int MaxBodyLength = 2000;

    private Notification()
    {
        // EF Core materialization only.
    }

    private Notification(
        Guid id,
        Guid? eventId,
        Guid recipientId,
        string recipient,
        string title,
        string body,
        string type,
        DateTime createdAtUtc)
    {
        Id = id;
        EventId = eventId;
        RecipientId = recipientId;
        Recipient = recipient;
        Title = title;
        Body = body;
        Type = type;
        CreatedAtUtc = createdAtUtc;
        IsRead = false;
        ReadAtUtc = null;
    }

    /// <summary>
    /// Gets the originating domain event identifier, if applicable.
    /// </summary>
    public Guid? EventId { get; private set; }

    /// <summary>
    /// Gets the unique identifier of the target recipient (e.g. merchant or user).
    /// </summary>
    public Guid RecipientId { get; private set; }

    /// <summary>
    /// Gets the recipient address or contact handle (e.g. phone or email).
    /// </summary>
    public string Recipient { get; private set; } = null!;

    /// <summary>
    /// Gets the title or subject of the notification.
    /// </summary>
    public string Title { get; private set; } = null!;

    /// <summary>
    /// Gets the body or content of the notification.
    /// </summary>
    public string Body { get; private set; } = null!;

    /// <summary>
    /// Gets the notification type classification.
    /// </summary>
    public string Type { get; private set; } = null!;

    /// <summary>
    /// Gets the UTC timestamp when the notification was created.
    /// </summary>
    public DateTime CreatedAtUtc { get; private set; }

    /// <summary>
    /// Gets the UTC timestamp when the notification was delivered or sent, if completed.
    /// </summary>
    public DateTime? SentAtUtc { get; private set; }

    /// <summary>
    /// Gets a value indicating whether the notification has been delivered.
    /// </summary>
    public bool IsSent => SentAtUtc.HasValue;

    /// <summary>
    /// Gets a value indicating whether the notification has been read by the recipient.
    /// </summary>
    public bool IsRead { get; private set; }

    /// <summary>
    /// Gets the UTC timestamp when the notification was marked as read, or <see langword="null"/> if unread.
    /// </summary>
    public DateTime? ReadAtUtc { get; private set; }

    /// <inheritdoc/>
    public DateTime CreatedAt { get; set; }

    /// <inheritdoc/>
    public string? CreatedBy { get; set; }

    /// <inheritdoc/>
    public DateTime? UpdatedAt { get; set; }

    /// <inheritdoc/>
    public string? UpdatedBy { get; set; }

    /// <summary>
    /// Creates a new <see cref="Notification"/> aggregate instance.
    /// </summary>
    /// <param name="eventId">The optional originating event identifier.</param>
    /// <param name="recipientId">The recipient identifier.</param>
    /// <param name="recipient">The recipient address or handle.</param>
    /// <param name="title">The notification title.</param>
    /// <param name="body">The notification body.</param>
    /// <param name="type">The notification type.</param>
    /// <param name="createdAtUtc">The creation timestamp.</param>
    /// <returns>A new <see cref="Notification"/> instance.</returns>
    public static Notification Create(
        Guid? eventId,
        Guid recipientId,
        string recipient,
        string title,
        string body,
        string type,
        DateTime createdAtUtc)
    {
        if (recipientId == Guid.Empty)
        {
            throw new ArgumentException("The recipient identifier cannot be empty.", nameof(recipientId));
        }

        if (string.IsNullOrWhiteSpace(recipient))
        {
            throw new ArgumentException("The recipient cannot be null or whitespace.", nameof(recipient));
        }

        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ArgumentException("The title cannot be null or whitespace.", nameof(title));
        }

        if (string.IsNullOrWhiteSpace(body))
        {
            throw new ArgumentException("The body cannot be null or whitespace.", nameof(body));
        }

        if (string.IsNullOrWhiteSpace(type))
        {
            throw new ArgumentException("The type cannot be null or whitespace.", nameof(type));
        }

        string trimmedRecipient = recipient.Trim();
        string trimmedTitle = title.Trim();
        string trimmedBody = body.Trim();
        string trimmedType = type.Trim();

        if (trimmedRecipient.Length > MaxRecipientLength)
        {
            throw new ArgumentException($"The recipient cannot exceed {MaxRecipientLength} characters.", nameof(recipient));
        }

        if (trimmedTitle.Length > MaxTitleLength)
        {
            throw new ArgumentException($"The title cannot exceed {MaxTitleLength} characters.", nameof(title));
        }

        if (trimmedBody.Length > MaxBodyLength)
        {
            throw new ArgumentException($"The body cannot exceed {MaxBodyLength} characters.", nameof(body));
        }

        if (trimmedType.Length > MaxTypeLength)
        {
            throw new ArgumentException($"The type cannot exceed {MaxTypeLength} characters.", nameof(type));
        }

        return new Notification(
            Guid.NewGuid(),
            eventId,
            recipientId,
            trimmedRecipient,
            trimmedTitle,
            trimmedBody,
            trimmedType,
            createdAtUtc);
    }

    /// <summary>
    /// Marks the notification as delivered.
    /// </summary>
    /// <param name="sentAtUtc">The UTC delivery timestamp.</param>
    public void MarkAsSent(DateTime sentAtUtc)
    {
        SentAtUtc = sentAtUtc;
    }

    /// <summary>
    /// Marks the notification as read by the recipient. If already marked as read, this operation is a no-op (idempotent).
    /// </summary>
    /// <param name="utcNow">The current UTC timestamp.</param>
    public void MarkAsRead(DateTime utcNow)
    {
        if (IsRead)
        {
            return;
        }

        IsRead = true;
        ReadAtUtc = utcNow;
    }
}
