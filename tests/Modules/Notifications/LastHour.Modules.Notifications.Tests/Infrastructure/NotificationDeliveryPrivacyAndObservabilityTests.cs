using LastHour.Modules.Notifications.Application.Delivery;
using LastHour.Modules.Notifications.Domain.Notifications;
using LastHour.Modules.Notifications.Infrastructure.Delivery;
using Microsoft.Extensions.Logging.Abstractions;

namespace LastHour.Modules.Notifications.Tests.Infrastructure;

public sealed class NotificationDeliveryPrivacyAndObservabilityTests
{
    [Fact]
    public async Task EmailChannelSender_SendAsync_ShouldSucceedWithoutExposingPii()
    {
        // Arrange
        var sender = new EmailNotificationChannelSender(NullLogger<EmailNotificationChannelSender>.Instance);
        var notification = Notification.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "sensitive-user-email@domain.com",
            "Sensitive Subject",
            "Sensitive Body containing PII and secret tokens",
            "UserPasswordChanged",
            DateTime.UtcNow);

        var delivery = NotificationDelivery.Create(notification.Id, DeliveryChannel.Email, DateTime.UtcNow);

        // Act
        var result = await sender.SendAsync(notification, delivery);

        // Assert
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task SmsChannelSender_SendAsync_ShouldSucceedWithoutExposingPii()
    {
        // Arrange
        var sender = new SmsNotificationChannelSender(NullLogger<SmsNotificationChannelSender>.Instance);
        var notification = Notification.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "+1-555-0199-private-phone",
            "Security Alert",
            "Your verification code is 123456",
            "UserLockedOut",
            DateTime.UtcNow);

        var delivery = NotificationDelivery.Create(notification.Id, DeliveryChannel.Sms, DateTime.UtcNow);

        // Act
        var result = await sender.SendAsync(notification, delivery);

        // Assert
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task InAppChannelSender_SendAsync_ShouldSucceed()
    {
        // Arrange
        var sender = new InAppNotificationChannelSender(NullLogger<InAppNotificationChannelSender>.Instance);
        var notification = Notification.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "user-123",
            "Welcome",
            "Welcome message",
            "UserRegistered",
            DateTime.UtcNow);

        var delivery = NotificationDelivery.Create(notification.Id, DeliveryChannel.InApp, DateTime.UtcNow);

        // Act
        var result = await sender.SendAsync(notification, delivery);

        // Assert
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void PolicyResolver_ForSecurityEvents_ShouldIncludeEmailChannel()
    {
        // Arrange
        var resolver = new NotificationDeliveryPolicyResolver();
        var passwordNotification = Notification.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "user@domain.com",
            "Password Changed",
            "Body",
            "UserPasswordChanged",
            DateTime.UtcNow);

        // Act
        var channels = resolver.ResolveChannels(passwordNotification);

        // Assert
        Assert.Contains(DeliveryChannel.InApp, channels);
        Assert.Contains(DeliveryChannel.Email, channels);
    }

    [Fact]
    public void Metrics_ContainNoPII()
    {
        // Arrange & Act
        Type metricsType = typeof(NotificationDeliveryMetrics);
        var methods = metricsType.GetMethods();

        string[] forbiddenPiiNames = new[] { "Email", "Phone", "UserId", "RecipientId", "Token", "Secret", "Password" };

        // Assert
        foreach (var method in methods)
        {
            var parameters = method.GetParameters();
            foreach (var param in parameters)
            {
                Assert.False(forbiddenPiiNames.Any(p => param.Name != null && param.Name.Contains(p, StringComparison.OrdinalIgnoreCase)),
                    $"Metric recorder method '{method.Name}' parameter '{param.Name}' must not expose PII.");
            }
        }
    }
}
