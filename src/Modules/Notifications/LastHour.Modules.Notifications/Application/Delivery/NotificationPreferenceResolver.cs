using LastHour.Modules.Notifications.Application.Persistence;
using LastHour.Modules.Notifications.Domain.Notifications;

namespace LastHour.Modules.Notifications.Application.Delivery;

/// <summary>
/// Production preference resolver applying default policies, user preferences, and security invariants.
/// </summary>
public sealed class NotificationPreferenceResolver : INotificationPreferenceResolver
{
    private readonly INotificationPreferenceRepository _preferenceRepository;

    /// <summary>
    /// Initializes a new instance of the <see cref="NotificationPreferenceResolver"/> class.
    /// </summary>
    public NotificationPreferenceResolver(INotificationPreferenceRepository preferenceRepository)
    {
        _preferenceRepository = preferenceRepository ?? throw new ArgumentNullException(nameof(preferenceRepository));
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<DeliveryChannel>> ResolveChannelsAsync(
        Guid userId,
        string notificationType,
        IReadOnlyList<DeliveryChannel> candidateChannels,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(notificationType);
        ArgumentNullException.ThrowIfNull(candidateChannels);

        if (candidateChannels.Count == 0)
        {
            return Array.Empty<DeliveryChannel>();
        }

        string normalizedType = notificationType.Trim();
        bool isSecurityEvent = string.Equals(normalizedType, "UserPasswordChanged", StringComparison.OrdinalIgnoreCase) ||
                               string.Equals(normalizedType, "UserLockedOut", StringComparison.OrdinalIgnoreCase);

        // Retrieve user preferences for this notification type
        IReadOnlyList<NotificationPreference> userPreferences = await _preferenceRepository
            .GetByUserAndTypeAsync(userId, normalizedType, cancellationToken)
            .ConfigureAwait(false);

        var preferenceMap = userPreferences.ToDictionary(p => p.Channel, p => p.IsEnabled);

        var resolvedChannels = new List<DeliveryChannel>();

        foreach (DeliveryChannel channel in candidateChannels.Distinct())
        {
            if (preferenceMap.TryGetValue(channel, out bool isExplicitlyEnabled))
            {
                if (isExplicitlyEnabled)
                {
                    resolvedChannels.Add(channel);
                }
                else
                {
                    // IN-APP SECURITY INVARIANT: InApp channel for security events cannot be disabled by user opt-out.
                    if (isSecurityEvent && channel == DeliveryChannel.InApp)
                    {
                        resolvedChannels.Add(channel);
                    }

                    // All other channels (including Email on security events) respect explicit opt-out.
                }
            }
            else
            {
                // Default preference policy when no explicit preference record exists:
                // Security events default to [InApp, Email]
                // Standard events default to [InApp]
                // SMS and Push default to disabled unless explicitly enabled.
                if (channel == DeliveryChannel.InApp)
                {
                    resolvedChannels.Add(channel);
                }
                else if (channel == DeliveryChannel.Email && isSecurityEvent)
                {
                    resolvedChannels.Add(channel);
                }
            }
        }

        return resolvedChannels.AsReadOnly();
    }
}
