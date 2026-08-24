using LastHour.BuildingBlocks.Application.Cqrs;
using LastHour.Modules.Notifications.Application.Models;

namespace LastHour.Modules.Notifications.Application.Queries;

/// <summary>
/// Query to list system notification templates.
/// </summary>
public sealed record GetNotificationTemplatesQuery : IQuery<IReadOnlyList<NotificationTemplateDto>>
{
}
