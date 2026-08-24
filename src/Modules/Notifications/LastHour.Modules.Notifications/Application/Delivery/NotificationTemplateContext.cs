namespace LastHour.Modules.Notifications.Application.Delivery;

/// <summary>
/// Safe context object containing allowed template substitution variables.
/// Credentials, passwords, tokens, and raw secrets are strictly prohibited.
/// </summary>
public sealed class NotificationTemplateContext
{
    public NotificationTemplateContext(
        string? userName = null,
        string? notificationTitle = null,
        string? notificationBody = null,
        DateTime? createdAtUtc = null,
        string? applicationName = "LastHour")
    {
        UserName = userName ?? "User";
        NotificationTitle = notificationTitle ?? string.Empty;
        NotificationBody = notificationBody ?? string.Empty;
        CreatedAtUtc = createdAtUtc ?? DateTime.UtcNow;
        ApplicationName = applicationName ?? "LastHour";
    }

    /// <summary>
    /// Gets the recipient user display name.
    /// </summary>
    public string UserName { get; }

    /// <summary>
    /// Gets the notification title.
    /// </summary>
    public string NotificationTitle { get; }

    /// <summary>
    /// Gets the notification body content.
    /// </summary>
    public string NotificationBody { get; }

    /// <summary>
    /// Gets the notification creation timestamp.
    /// </summary>
    public DateTime CreatedAtUtc { get; }

    /// <summary>
    /// Gets the application display name.
    /// </summary>
    public string ApplicationName { get; }

    /// <summary>
    /// Converts the context variables into a dictionary for template rendering.
    /// </summary>
    public IReadOnlyDictionary<string, string> ToDictionary()
    {
        return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["UserName"] = UserName,
            ["NotificationTitle"] = NotificationTitle,
            ["NotificationBody"] = NotificationBody,
            ["CreatedAtUtc"] = CreatedAtUtc.ToString("u"),
            ["ApplicationName"] = ApplicationName,
        };
    }
}
