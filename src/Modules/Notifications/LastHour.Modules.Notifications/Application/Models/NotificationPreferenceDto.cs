namespace LastHour.Modules.Notifications.Application.Models;

/// <summary>
/// DTO representing a user's notification preference for a specific type and channel.
/// </summary>
public sealed record NotificationPreferenceDto(
    string notificationType,
    string channel,
    bool isEnabled,
    DateTime updatedAtUtc);
