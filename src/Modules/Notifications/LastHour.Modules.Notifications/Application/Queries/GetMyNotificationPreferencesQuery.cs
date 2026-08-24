using LastHour.BuildingBlocks.Application.Cqrs;
using LastHour.Modules.Notifications.Application.Models;

namespace LastHour.Modules.Notifications.Application.Queries;

/// <summary>
/// Query to retrieve all notification preferences for the authenticated user.
/// </summary>
public sealed record GetMyNotificationPreferencesQuery : IQuery<IReadOnlyList<NotificationPreferenceDto>>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GetMyNotificationPreferencesQuery"/> class.
    /// </summary>
    public GetMyNotificationPreferencesQuery(Guid userId)
    {
        UserId = userId;
    }

    /// <summary>
    /// Gets the unique identifier of the authenticated user.
    /// </summary>
    public Guid UserId { get; }
}
