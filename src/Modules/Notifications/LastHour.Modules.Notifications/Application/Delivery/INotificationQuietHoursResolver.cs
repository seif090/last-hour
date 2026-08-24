using LastHour.Modules.Notifications.Domain.Notifications;

namespace LastHour.Modules.Notifications.Application.Delivery;

/// <summary>
/// Service interface evaluating user quiet hours and determining scheduled delivery timestamps.
/// </summary>
public interface INotificationQuietHoursResolver
{
    /// <summary>
    /// Calculates the next eligible delivery timestamp for a given channel and user, deferring external channels if in quiet hours.
    /// InApp delivery is never deferred.
    /// </summary>
    Task<DateTime?> CalculateScheduledAttemptAtAsync(Guid userId, DeliveryChannel channel, DateTime nowUtc, CancellationToken cancellationToken = default);
}
