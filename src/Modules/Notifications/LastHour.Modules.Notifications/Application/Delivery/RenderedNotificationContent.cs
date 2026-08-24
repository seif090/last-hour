namespace LastHour.Modules.Notifications.Application.Delivery;

/// <summary>
/// Represents the rendered output of a notification template after placeholder substitution.
/// </summary>
public sealed record RenderedNotificationContent(string? subject, string body);
