using LastHour.Modules.Notifications.Domain.Notifications;

namespace LastHour.Modules.Notifications.Application.Delivery;

/// <summary>
/// Domain service interface for resolving active notification delivery channels based on candidate channels and user preferences.
/// </summary>
public interface INotificationPreferenceResolver
{
    /// <summary>
    /// Filters candidate delivery channels against user preferences and default policies.
    /// </summary>
    Task<IReadOnlyList<DeliveryChannel>> ResolveChannelsAsync(
        Guid userId,
        string notificationType,
        IReadOnlyList<DeliveryChannel> candidateChannels,
        CancellationToken cancellationToken = default);
}
