using LastHour.Modules.Notifications.Application.Persistence;
using LastHour.Modules.Notifications.Domain.Notifications;

namespace LastHour.Modules.Notifications.Application.Delivery;

/// <summary>
/// Production template resolver fetching active database templates or supplying deterministic defaults.
/// </summary>
public sealed class NotificationTemplateResolver : INotificationTemplateResolver
{
    private readonly INotificationTemplateRepository _templateRepository;

    /// <summary>
    /// Initializes a new instance of the <see cref="NotificationTemplateResolver"/> class.
    /// </summary>
    public NotificationTemplateResolver(INotificationTemplateRepository templateRepository)
    {
        _templateRepository = templateRepository ?? throw new ArgumentNullException(nameof(templateRepository));
    }

    /// <inheritdoc/>
    public async Task<NotificationTemplate> ResolveTemplateAsync(string notificationType, DeliveryChannel channel, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(notificationType);
        string normalizedType = notificationType.Trim();

        NotificationTemplate? activeTemplate = await _templateRepository
            .GetActiveTemplateAsync(normalizedType, channel, cancellationToken)
            .ConfigureAwait(false);

        if (activeTemplate is not null)
        {
            return activeTemplate;
        }

        // Deterministic, provider-neutral default fallback template
        string templateKey = $"default_{normalizedType.ToLowerInvariant()}_{channel.ToString().ToLowerInvariant()}";
        string defaultSubject = GetDefaultSubject(normalizedType, channel);
        string defaultBody = GetDefaultBody(normalizedType, channel);

        return NotificationTemplate.Create(
            templateKey,
            normalizedType,
            channel,
            version: 1,
            body: defaultBody,
            subject: defaultSubject,
            isActive: true,
            createdAtUtc: DateTime.UtcNow);
    }

    private static string GetDefaultSubject(string notificationType, DeliveryChannel channel)
    {
        if (channel != DeliveryChannel.Email)
        {
            return string.Empty;
        }

        return notificationType switch
        {
            "UserPasswordChanged" => "Security Alert: Your password was changed",
            "UserLockedOut" => "Security Alert: Account locked out",
            "UserRegistered" => "Welcome to {{ApplicationName}}",
            "MerchantCreated" => "Merchant Account Created",
            "MerchantStatusChanged" => "Merchant Status Update",
            "MerchantProfileUpdated" => "Merchant Profile Updated",
            _ => "Notification from {{ApplicationName}}",
        };
    }

    private static string GetDefaultBody(string notificationType, DeliveryChannel channel)
    {
        return notificationType switch
        {
            "UserPasswordChanged" => "Hello {{UserName}}, your password for {{ApplicationName}} has been changed successfully.",
            "UserLockedOut" => "Hello {{UserName}}, your {{ApplicationName}} account has been temporarily locked due to multiple failed login attempts.",
            "UserRegistered" => "Hello {{UserName}}, welcome to {{ApplicationName}}!",
            "MerchantCreated" => "Merchant account successfully initialized.",
            "MerchantStatusChanged" => "Your merchant account status has changed.",
            "MerchantProfileUpdated" => "Your merchant profile details have been updated.",
            _ => "{{NotificationTitle}}: {{NotificationBody}}",
        };
    }
}
