using LastHour.Modules.Notifications.Application.Persistence;
using LastHour.Modules.Notifications.Domain.Notifications;

namespace LastHour.Modules.Notifications.Application.Delivery;

/// <summary>
/// Production quiet hours resolver enforcing local quiet-hours deferral for external channels.
/// </summary>
public sealed class NotificationQuietHoursResolver : INotificationQuietHoursResolver
{
    private readonly INotificationQuietHoursRepository _quietHoursRepository;

    /// <summary>
    /// Initializes a new instance of the <see cref="NotificationQuietHoursResolver"/> class.
    /// </summary>
    public NotificationQuietHoursResolver(INotificationQuietHoursRepository quietHoursRepository)
    {
        _quietHoursRepository = quietHoursRepository ?? throw new ArgumentNullException(nameof(quietHoursRepository));
    }

    /// <inheritdoc/>
    public async Task<DateTime?> CalculateScheduledAttemptAtAsync(Guid userId, DeliveryChannel channel, DateTime nowUtc, CancellationToken cancellationToken = default)
    {
        // IN-APP SEMANTICS: InApp delivery is immediate and NEVER delayed by quiet hours
        if (channel == DeliveryChannel.InApp || userId == Guid.Empty)
        {
            return nowUtc;
        }

        NotificationQuietHours? quietHours = await _quietHoursRepository
            .GetByUserIdAsync(userId, cancellationToken)
            .ConfigureAwait(false);

        if (quietHours is null || !quietHours.Enabled)
        {
            return nowUtc;
        }

        if (!quietHours.IsInQuietHours(nowUtc))
        {
            return nowUtc;
        }

        // Calculate next eligible UTC time after quiet hours end
        TimeZoneInfo timeZone = TimeZoneInfo.FindSystemTimeZoneById(quietHours.TimeZoneId);
        DateTime localNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(nowUtc, DateTimeKind.Utc), timeZone);

        DateTime localEnd = localNow.Date.Add(quietHours.EndLocalTime.ToTimeSpan());

        if (localEnd <= localNow)
        {
            localEnd = localEnd.AddDays(1);
        }

        DateTime nextEligibleUtc = TimeZoneInfo.ConvertTimeToUtc(localEnd, timeZone);
        return nextEligibleUtc;
    }
}
