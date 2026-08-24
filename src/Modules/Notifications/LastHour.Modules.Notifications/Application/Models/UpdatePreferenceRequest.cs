namespace LastHour.Modules.Notifications.Application.Models;

/// <summary>
/// Request payload for updating a notification preference.
/// </summary>
public sealed record UpdatePreferenceRequest(bool isEnabled);
