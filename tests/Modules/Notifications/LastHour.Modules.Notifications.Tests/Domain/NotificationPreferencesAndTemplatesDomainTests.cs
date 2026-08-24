using LastHour.Modules.Notifications.Domain.Notifications;

namespace LastHour.Modules.Notifications.Tests.Domain;

public class NotificationPreferencesAndTemplatesDomainTests
{
    [Fact]
    public void Preference_Creation_ValidatesInputs()
    {
        Guid userId = Guid.NewGuid();

        // Empty User ID throws
        Assert.Throws<ArgumentException>(() =>
            NotificationPreference.Create(Guid.Empty, "UserRegistered", DeliveryChannel.Email, true, DateTime.UtcNow));

        // Empty Notification Type throws
        Assert.Throws<ArgumentException>(() =>
            NotificationPreference.Create(userId, "", DeliveryChannel.Email, true, DateTime.UtcNow));

        // Valid creation succeeds
        var preference = NotificationPreference.Create(userId, "UserRegistered", DeliveryChannel.Email, true, DateTime.UtcNow);
        Assert.Equal(userId, preference.UserId);
        Assert.Equal("UserRegistered", preference.NotificationType);
        Assert.Equal(DeliveryChannel.Email, preference.Channel);
        Assert.True(preference.IsEnabled);
    }

    [Fact]
    public void Preference_EnableAndDisable_MutatesStateAndTimestamp()
    {
        Guid userId = Guid.NewGuid();
        DateTime created = DateTime.UtcNow.AddHours(-1);
        DateTime updated = DateTime.UtcNow;

        var preference = NotificationPreference.Create(userId, "UserPasswordChanged", DeliveryChannel.Email, isEnabled: false, created);
        Assert.False(preference.IsEnabled);

        preference.Enable(updated);
        Assert.True(preference.IsEnabled);
        Assert.Equal(updated, preference.UpdatedAtUtc);

        preference.Disable(updated.AddMinutes(5));
        Assert.False(preference.IsEnabled);
        Assert.Equal(updated.AddMinutes(5), preference.UpdatedAtUtc);
    }

    [Fact]
    public void Template_Creation_ValidatesInputs()
    {
        // Empty template key throws
        Assert.Throws<ArgumentException>(() =>
            NotificationTemplate.Create("", "UserRegistered", DeliveryChannel.Email, 1, "Body"));

        // Empty notification type throws
        Assert.Throws<ArgumentException>(() =>
            NotificationTemplate.Create("welcome_email", "", DeliveryChannel.Email, 1, "Body"));

        // Version <= 0 throws
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            NotificationTemplate.Create("welcome_email", "UserRegistered", DeliveryChannel.Email, 0, "Body"));

        // Empty body throws
        Assert.Throws<ArgumentException>(() =>
            NotificationTemplate.Create("welcome_email", "UserRegistered", DeliveryChannel.Email, 1, ""));

        // Valid template succeeds
        var template = NotificationTemplate.Create("welcome_email", "UserRegistered", DeliveryChannel.Email, 1, "Hello {{UserName}}", "Welcome!", isActive: true);
        Assert.Equal("welcome_email", template.TemplateKey);
        Assert.Equal("UserRegistered", template.NotificationType);
        Assert.Equal(DeliveryChannel.Email, template.Channel);
        Assert.Equal(1, template.Version);
        Assert.Equal("Welcome!", template.Subject);
        Assert.Equal("Hello {{UserName}}", template.Body);
        Assert.True(template.IsActive);
    }

    [Fact]
    public void Template_ActivateAndDeactivate_MutatesState()
    {
        var template = NotificationTemplate.Create("welcome_email", "UserRegistered", DeliveryChannel.Email, 1, "Body", isActive: true);
        DateTime now = DateTime.UtcNow;

        template.Deactivate(now);
        Assert.False(template.IsActive);

        template.Activate(now.AddMinutes(1));
        Assert.True(template.IsActive);
    }

    [Fact]
    public void QuietHours_Creation_ValidatesTimeZone()
    {
        Guid userId = Guid.NewGuid();

        // Invalid time zone throws
        Assert.Throws<ArgumentException>(() =>
            NotificationQuietHours.Create(userId, enabled: true, new TimeOnly(22, 0), new TimeOnly(7, 0), "Invalid/TimeZone_ID", DateTime.UtcNow));

        // Valid creation succeeds
        var quietHours = NotificationQuietHours.Create(userId, enabled: true, new TimeOnly(22, 0), new TimeOnly(7, 0), "UTC", DateTime.UtcNow);
        Assert.Equal(userId, quietHours.UserId);
        Assert.True(quietHours.Enabled);
        Assert.Equal(new TimeOnly(22, 0), quietHours.StartLocalTime);
        Assert.Equal(new TimeOnly(7, 0), quietHours.EndLocalTime);
    }

    [Fact]
    public void QuietHours_IsInQuietHours_EvaluatesSameDayAndMidnightCrossing()
    {
        Guid userId = Guid.NewGuid();

        // Midnight crossing: 22:00 to 07:00 UTC
        var overnight = NotificationQuietHours.Create(userId, enabled: true, new TimeOnly(22, 0), new TimeOnly(7, 0), "UTC", DateTime.UtcNow);

        // 23:00 UTC is inside quiet hours
        DateTime at23 = new DateTime(2026, 8, 20, 23, 0, 0, DateTimeKind.Utc);
        Assert.True(overnight.IsInQuietHours(at23));

        // 03:00 UTC is inside quiet hours
        DateTime at03 = new DateTime(2026, 8, 20, 3, 0, 0, DateTimeKind.Utc);
        Assert.True(overnight.IsInQuietHours(at03));

        // 12:00 UTC is OUTSIDE quiet hours
        DateTime at12 = new DateTime(2026, 8, 20, 12, 0, 0, DateTimeKind.Utc);
        Assert.False(overnight.IsInQuietHours(at12));

        // Same day window: 09:00 to 17:00 UTC
        var sameDay = NotificationQuietHours.Create(userId, enabled: true, new TimeOnly(9, 0), new TimeOnly(17, 0), "UTC", DateTime.UtcNow);
        Assert.True(sameDay.IsInQuietHours(at12));
        Assert.False(sameDay.IsInQuietHours(at23));
    }
}
