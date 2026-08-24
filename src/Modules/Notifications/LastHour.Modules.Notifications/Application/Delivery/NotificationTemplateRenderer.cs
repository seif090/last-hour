using System.Text.RegularExpressions;
using LastHour.Modules.Notifications.Domain.Notifications;

namespace LastHour.Modules.Notifications.Application.Delivery;

/// <summary>
/// Production template renderer replacing <c>{{VarName}}</c> tokens safely without reflection or code execution.
/// </summary>
public sealed class NotificationTemplateRenderer : INotificationTemplateRenderer
{
    private static readonly Regex PlaceholderRegex = new Regex(@"\{\{([a-zA-Z0-9_]+)\}\}", RegexOptions.Compiled);

    /// <inheritdoc/>
    public RenderedNotificationContent Render(NotificationTemplate notificationTemplate, NotificationTemplateContext context)
    {
        ArgumentNullException.ThrowIfNull(notificationTemplate);
        ArgumentNullException.ThrowIfNull(context);

        IReadOnlyDictionary<string, string> variables = context.ToDictionary();

        string? renderedSubject = notificationTemplate.Subject is not null
            ? SubstitutePlaceholders(notificationTemplate.Subject, variables)
            : null;

        string renderedBody = SubstitutePlaceholders(notificationTemplate.Body, variables);

        return new RenderedNotificationContent(renderedSubject, renderedBody);
    }

    private static string SubstitutePlaceholders(string templateText, IReadOnlyDictionary<string, string> variables)
    {
        if (string.IsNullOrEmpty(templateText))
        {
            return string.Empty;
        }

        return PlaceholderRegex.Replace(templateText, match =>
        {
            string varName = match.Groups[1].Value;
            if (variables.TryGetValue(varName, out string? value))
            {
                return value;
            }

            // Return original match if token is unrecognized (safe fallback)
            return match.Value;
        });
    }
}
