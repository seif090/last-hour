using LastHour.Modules.Notifications.Domain.Notifications;

namespace LastHour.Modules.Notifications.Application.Delivery;

/// <summary>
/// Production policy resolver determining candidate channels and applying preference resolution.
/// </summary>
public sealed class NotificationDeliveryPolicyResolver : INotificationDeliveryPolicyResolver
{
    private readonly INotificationPreferenceResolver? _preferenceResolver;

    /// <summary>
    /// Initializes a new instance of the <see cref="NotificationDeliveryPolicyResolver"/> class.
    /// </summary>
    public NotificationDeliveryPolicyResolver(INotificationPreferenceResolver? preferenceResolver = null)
    {
        _preferenceResolver = preferenceResolver;
    }

    /// <inheritdoc/>
    public IReadOnlyList<DeliveryChannel> ResolveChannels(Notification notification)
    {
        ArgumentNullException.ThrowIfNull(notification);

        // Security-critical events resolve both InApp and Email candidate channels
        if (string.Equals(notification.Type, "UserPasswordChanged", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(notification.Type, "UserLockedOut", StringComparison.OrdinalIgnoreCase))
        {
            return new[] { DeliveryChannel.InApp, DeliveryChannel.Email };
        }

        // Standard notifications default candidate: InApp channel
        return new[] { DeliveryChannel.InApp };
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<DeliveryChannel>> ResolveChannelsAsync(Notification notification, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(notification);
        IReadOnlyList<DeliveryChannel> candidateChannels = ResolveChannels(notification);

        if (_preferenceResolver is null)
        {
            return candidateChannels;
        }

        Guid userId = notification.RecipientId;
        return await _preferenceResolver
            .ResolveChannelsAsync(userId, notification.Type, candidateChannels, cancellationToken)
            .ConfigureAwait(false);
    }
}
