namespace LastHour.Modules.Notifications.Application.Models;

/// <summary>
/// Response model returned after marking all unread notifications as read.
/// </summary>
public sealed record MarkAllNotificationsAsReadResponse
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MarkAllNotificationsAsReadResponse"/> class.
    /// </summary>
    /// <param name="updatedCount">The number of notifications transitioned to the read state.</param>
    public MarkAllNotificationsAsReadResponse(int updatedCount)
    {
        UpdatedCount = updatedCount;
    }

    /// <summary>
    /// Gets the number of notifications transitioned to the read state.
    /// </summary>
    public int UpdatedCount { get; }
}
