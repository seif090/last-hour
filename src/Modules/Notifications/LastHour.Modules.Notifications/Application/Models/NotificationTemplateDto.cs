namespace LastHour.Modules.Notifications.Application.Models;

/// <summary>
/// DTO representing a versioned notification template.
/// </summary>
public sealed record NotificationTemplateDto(
    Guid id,
    string templateKey,
    string notificationType,
    string channel,
    int version,
    string? subject,
    string body,
    bool isActive,
    DateTime updatedAtUtc);
