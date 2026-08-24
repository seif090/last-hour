using LastHour.Modules.Notifications.Application.Delivery;
using LastHour.Modules.Notifications.Domain.Notifications;
using LastHour.Modules.Notifications.Infrastructure.Delivery;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace LastHour.Modules.Notifications.Tests.Infrastructure;

public sealed class NotificationDeliveryFactoryAndChannelTests
{
    [Fact]
    public void Factory_ResolvesEverySupportedChannel()
    {
        // Arrange
        var inAppSender = new InAppNotificationChannelSender(NullLogger<InAppNotificationChannelSender>.Instance);
        var emailSender = new EmailNotificationChannelSender(NullLogger<EmailNotificationChannelSender>.Instance);
        var smsSender = new SmsNotificationChannelSender(NullLogger<SmsNotificationChannelSender>.Instance);

        var factory = new NotificationChannelFactory(new INotificationChannelSender[] { inAppSender, emailSender, smsSender });

        // Act & Assert
        Assert.Equal(inAppSender, factory.GetSender(DeliveryChannel.InApp));
        Assert.Equal(emailSender, factory.GetSender(DeliveryChannel.Email));
        Assert.Equal(smsSender, factory.GetSender(DeliveryChannel.Sms));
    }

    [Fact]
    public void Factory_DoesNotSilentlyFallback()
    {
        // Arrange: Factory with only InApp sender registered
        var inAppSender = new InAppNotificationChannelSender(NullLogger<InAppNotificationChannelSender>.Instance);
        var factory = new NotificationChannelFactory(new INotificationChannelSender[] { inAppSender });

        // Act & Assert: Requesting Email must throw NotSupportedException, NOT return InAppSender
        Assert.Throws<NotSupportedException>(() => factory.GetSender(DeliveryChannel.Email));
        Assert.Throws<NotSupportedException>(() => factory.GetSender(DeliveryChannel.Sms));
    }

    [Fact]
    public void UnsupportedChannel_FailsDeterministically()
    {
        // Arrange
        var factory = new NotificationChannelFactory(Array.Empty<INotificationChannelSender>());

        // Act & Assert
        var ex = Assert.Throws<NotSupportedException>(() => factory.GetSender(DeliveryChannel.Push));
        Assert.Contains("Push", ex.Message);
    }

    [Fact]
    public void InvalidOptions_AreRejected()
    {
        // Arrange
        var validator = new NotificationDeliveryOptionsValidator();

        var invalidOptions1 = new NotificationDeliveryOptions
        {
            BatchSize = 0,
            PollingInterval = TimeSpan.Zero,
            LeaseDuration = TimeSpan.FromSeconds(-5),
            BaseRetryDelay = TimeSpan.Zero,
            MaximumRetryDelay = TimeSpan.FromSeconds(1),
            MaxRetryCount = -1,
        };

        var invalidOptions2 = new NotificationDeliveryOptions
        {
            BaseRetryDelay = TimeSpan.FromMinutes(10),
            MaximumRetryDelay = TimeSpan.FromSeconds(5), // Maximum < Base
        };

        // Act
        ValidateOptionsResult result1 = validator.Validate(null, invalidOptions1);
        ValidateOptionsResult result2 = validator.Validate(null, invalidOptions2);

        // Assert
        Assert.True(result1.Failed);
        Assert.Contains(result1.Failures, f => f.Contains("BatchSize"));
        Assert.Contains(result1.Failures, f => f.Contains("PollingInterval"));
        Assert.Contains(result1.Failures, f => f.Contains("LeaseDuration"));
        Assert.Contains(result1.Failures, f => f.Contains("BaseRetryDelay"));
        Assert.Contains(result1.Failures, f => f.Contains("MaxRetryCount"));

        Assert.True(result2.Failed);
        Assert.Contains(result2.Failures, f => f.Contains("MaximumRetryDelay must be greater than or equal to BaseRetryDelay"));
    }
}
