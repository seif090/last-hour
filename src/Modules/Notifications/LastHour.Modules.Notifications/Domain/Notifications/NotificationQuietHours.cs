using LastHour.BuildingBlocks.SharedKernel.Domain;

namespace LastHour.Modules.Notifications.Domain.Notifications;

/// <summary>
/// Entity representing a user's local quiet-hours notification schedule.
/// </summary>
public sealed class NotificationQuietHours : AggregateRoot<Guid>
{
    private NotificationQuietHours()
    {
    }

    private NotificationQuietHours(
        Guid id,
        Guid userId,
        bool enabled,
        TimeOnly startLocalTime,
        TimeOnly endLocalTime,
        string timeZoneId,
        DateTime createdAtUtc)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Identifier cannot be empty.", nameof(id));
        }

        if (userId == Guid.Empty)
        {
            throw new ArgumentException("User identifier cannot be empty.", nameof(userId));
        }

        if (string.IsNullOrWhiteSpace(timeZoneId))
        {
            throw new ArgumentException("Time zone identifier cannot be empty.", nameof(timeZoneId));
        }

        // Validate time zone format
        try
        {
            TimeZoneInfo.FindSystemTimeZoneById(timeZoneId.Trim());
        }
        catch (Exception ex) when (ex is TimeZoneNotFoundException or InvalidTimeZoneException)
        {
            throw new ArgumentException($"Invalid time zone identifier '{timeZoneId}'.", nameof(timeZoneId), ex);
        }

        Id = id;
        UserId = userId;
        Enabled = enabled;
        StartLocalTime = startLocalTime;
        EndLocalTime = endLocalTime;
        TimeZoneId = timeZoneId.Trim();
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = createdAtUtc;
    }

    /// <summary>
    /// Gets the user identifier.
    /// </summary>
    public Guid UserId { get; private set; }

    /// <summary>
    /// Gets a value indicating whether quiet hours are active.
    /// </summary>
    public bool Enabled { get; private set; }

    /// <summary>
    /// Gets the local start time of quiet hours.
    /// </summary>
    public TimeOnly StartLocalTime { get; private set; }

    /// <summary>
    /// Gets the local end time of quiet hours.
    /// </summary>
    public TimeOnly EndLocalTime { get; private set; }

    /// <summary>
    /// Gets the IANA / Windows time zone identifier.
    /// </summary>
    public string TimeZoneId { get; private set; } = "UTC";

    /// <summary>
    /// Gets the UTC creation timestamp.
    /// </summary>
    public DateTime CreatedAtUtc { get; private set; }

    /// <summary>
    /// Gets the UTC update timestamp.
    /// </summary>
    public DateTime UpdatedAtUtc { get; private set; }

    /// <summary>
    /// Factory method to create a new quiet hours configuration.
    /// </summary>
    public static NotificationQuietHours Create(
        Guid userId,
        bool enabled,
        TimeOnly startLocalTime,
        TimeOnly endLocalTime,
        string timeZoneId,
        DateTime createdAtUtc)
    {
        return new NotificationQuietHours(Guid.NewGuid(), userId, enabled, startLocalTime, endLocalTime, timeZoneId, createdAtUtc);
    }

    /// <summary>
    /// Updates the quiet hours schedule and time zone.
    /// </summary>
    public void SetQuietHours(TimeOnly startLocalTime, TimeOnly endLocalTime, string timeZoneId, DateTime utcNow)
    {
        if (string.IsNullOrWhiteSpace(timeZoneId))
        {
            throw new ArgumentException("Time zone identifier cannot be empty.", nameof(timeZoneId));
        }

        TimeZoneInfo.FindSystemTimeZoneById(timeZoneId.Trim());

        StartLocalTime = startLocalTime;
        EndLocalTime = endLocalTime;
        TimeZoneId = timeZoneId.Trim();
        UpdatedAtUtc = utcNow;
    }

    /// <summary>
    /// Enables quiet hours enforcement.
    /// </summary>
    public void Enable(DateTime utcNow)
    {
        Enabled = true;
        UpdatedAtUtc = utcNow;
    }

    /// <summary>
    /// Disables quiet hours enforcement.
    /// </summary>
    public void Disable(DateTime utcNow)
    {
        Enabled = false;
        UpdatedAtUtc = utcNow;
    }

    /// <summary>
    /// Determines whether the given UTC timestamp falls inside the user's quiet hours window.
    /// Handles midnight crossing (e.g. 22:00 to 07:00) deterministically.
    /// </summary>
    public bool IsInQuietHours(DateTime utcNow)
    {
        if (!Enabled)
        {
            return false;
        }

        TimeZoneInfo timeZone = TimeZoneInfo.FindSystemTimeZoneById(TimeZoneId);
        DateTime localTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utcNow, DateTimeKind.Utc), timeZone);
        TimeOnly currentLocalTime = TimeOnly.FromDateTime(localTime);

        if (StartLocalTime < EndLocalTime)
        {
            // Same-day interval (e.g. 09:00 to 17:00)
            return currentLocalTime >= StartLocalTime && currentLocalTime < EndLocalTime;
        }

        // Overnight interval (e.g. 22:00 to 07:00)
        return currentLocalTime >= StartLocalTime || currentLocalTime < EndLocalTime;
    }
}
