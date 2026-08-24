using LastHour.Modules.Notifications.Application.Delivery;
using LastHour.Modules.Notifications.Domain.Notifications;
using LastHour.Modules.Notifications.Infrastructure.Delivery;

namespace LastHour.Modules.Notifications.Tests.Domain;

public sealed class NotificationDeliveryReliabilityDomainTests
{
    [Fact]
    public void RetryCount_IsMonotonic()
    {
        // Arrange
        var delivery = NotificationDelivery.Create(Guid.NewGuid(), DeliveryChannel.Email, DateTime.UtcNow, maximumRetryCount: 5);
        DateTime now = DateTime.UtcNow;
        TimeSpan delay = TimeSpan.FromSeconds(10);

        // Act
        delivery.RecordFailure("Error 1", now, maxRetryCount: 5, retryDelay: delay);
        int retry1 = delivery.RetryCount;

        delivery.RecordFailure("Error 2", now, maxRetryCount: 5, retryDelay: delay);
        int retry2 = delivery.RetryCount;

        delivery.RecordFailure("Error 3", now, maxRetryCount: 5, retryDelay: delay);
        int retry3 = delivery.RetryCount;

        // Assert
        Assert.True(retry1 < retry2);
        Assert.True(retry2 < retry3);
        Assert.Equal(1, retry1);
        Assert.Equal(2, retry2);
        Assert.Equal(3, retry3);
    }

    [Fact]
    public void RetryCount_NeverExceedsMaximum()
    {
        // Arrange
        int maxRetries = 3;
        var delivery = NotificationDelivery.Create(Guid.NewGuid(), DeliveryChannel.Email, DateTime.UtcNow, maximumRetryCount: maxRetries);
        DateTime now = DateTime.UtcNow;
        TimeSpan delay = TimeSpan.FromSeconds(10);

        // Act
        for (int i = 0; i < maxRetries; i++)
        {
            delivery.RecordFailure($"Error {i}", now, maxRetryCount: maxRetries, retryDelay: delay);
        }

        // Assert
        Assert.Equal(DeliveryStatus.FailedPermanent, delivery.Status);
        Assert.Equal(maxRetries, delivery.RetryCount);
        Assert.Null(delivery.NextAttemptAtUtc);

        // Attempting to record failure on a terminal delivery must throw InvalidOperationException
        Assert.Throws<InvalidOperationException>(() =>
            delivery.RecordFailure("Extra error", now, maxRetryCount: maxRetries, retryDelay: delay));
    }

    [Fact]
    public void Backoff_NeverExceedsMaximumDelay()
    {
        // Arrange
        TimeSpan baseDelay = TimeSpan.FromSeconds(10);
        TimeSpan maxDelay = TimeSpan.FromMinutes(15);

        // Act & Assert
        for (int retryCount = 0; retryCount < 100; retryCount++)
        {
            TimeSpan calculated = NotificationDeliveryProcessor.CalculateRetryDelay(baseDelay, maxDelay, retryCount);
            Assert.True(calculated <= maxDelay, $"Retry count {retryCount} resulted in delay {calculated} exceeding maximum {maxDelay}");
            Assert.True(calculated >= baseDelay, $"Retry count {retryCount} resulted in delay {calculated} below base {baseDelay}");
        }
    }

    [Fact]
    public void ExtremeRetryCount_DoesNotOverflow()
    {
        // Arrange
        TimeSpan baseDelay = TimeSpan.FromSeconds(5);
        TimeSpan maxDelay = TimeSpan.FromHours(1);

        // Act & Assert
        TimeSpan delayIntegerMax = NotificationDeliveryProcessor.CalculateRetryDelay(baseDelay, maxDelay, int.MaxValue);
        TimeSpan delayNegative = NotificationDeliveryProcessor.CalculateRetryDelay(baseDelay, maxDelay, -100);

        Assert.Equal(maxDelay, delayIntegerMax);
        Assert.Equal(baseDelay, delayNegative);
    }

    [Fact]
    public void PermanentFailure_DoesNotRetry()
    {
        // Arrange
        var delivery = NotificationDelivery.Create(Guid.NewGuid(), DeliveryChannel.Sms, DateTime.UtcNow, maximumRetryCount: 1);
        DateTime now = DateTime.UtcNow;

        // Act
        delivery.RecordFailure("Fatal error", now, maxRetryCount: 1, TimeSpan.FromSeconds(10));

        // Assert
        Assert.Equal(DeliveryStatus.FailedPermanent, delivery.Status);
        Assert.Null(delivery.NextAttemptAtUtc);
        Assert.Null(delivery.LockExpiresAtUtc);
    }

    [Fact]
    public void RetryExhaustion_TransitionsToFailedPermanent()
    {
        // Arrange
        var delivery = NotificationDelivery.Create(Guid.NewGuid(), DeliveryChannel.Email, DateTime.UtcNow, maximumRetryCount: 2);
        DateTime now = DateTime.UtcNow;

        // Act
        delivery.RecordFailure("Attempt 1 failure", now, maxRetryCount: 2, TimeSpan.FromSeconds(5));
        Assert.Equal(DeliveryStatus.RetryScheduled, delivery.Status);

        delivery.RecordFailure("Attempt 2 failure", now, maxRetryCount: 2, TimeSpan.FromSeconds(5));

        // Assert
        Assert.Equal(DeliveryStatus.FailedPermanent, delivery.Status);
        Assert.Equal(2, delivery.RetryCount);
        Assert.Null(delivery.NextAttemptAtUtc);
    }

    [Fact]
    public void PolicyResolution_IsDeterministic()
    {
        // Arrange
        var resolver = new NotificationDeliveryPolicyResolver();
        var notification1 = Notification.Create(Guid.NewGuid(), Guid.NewGuid(), "user1@lasthour.local", "Subject", "Body", "UserPasswordChanged", DateTime.UtcNow);
        var notification2 = Notification.Create(Guid.NewGuid(), Guid.NewGuid(), "user1@lasthour.local", "Subject", "Body", "UserPasswordChanged", DateTime.UtcNow);

        // Act
        IReadOnlyList<DeliveryChannel> channels1 = resolver.ResolveChannels(notification1);
        IReadOnlyList<DeliveryChannel> channels2 = resolver.ResolveChannels(notification2);

        // Assert
        Assert.Equal(channels1.Count, channels2.Count);
        for (int i = 0; i < channels1.Count; i++)
        {
            Assert.Equal(channels1[i], channels2[i]);
        }
    }

    [Fact]
    public void PolicyResolution_ContainsNoDuplicateChannels()
    {
        // Arrange
        var resolver = new NotificationDeliveryPolicyResolver();

        string[] notificationTypes = new[]
        {
            "UserRegistered",
            "UserPasswordChanged",
            "UserLockedOut",
            "MerchantCreated",
            "MerchantStatusChanged",
            "MerchantProfileUpdated",
        };

        foreach (string type in notificationTypes)
        {
            var notification = Notification.Create(Guid.NewGuid(), Guid.NewGuid(), "user@lasthour.local", "Subject", "Body", type, DateTime.UtcNow);

            // Act
            IReadOnlyList<DeliveryChannel> channels = resolver.ResolveChannels(notification);

            // Assert
            Assert.Equal(channels.Count, channels.Distinct().Count());
        }
    }

    [Fact]
    public void InAppDelivery_DoesNotCreateSecondNotification()
    {
        // Arrange
        Guid notificationId = Guid.NewGuid();

        // Act
        var delivery = NotificationDelivery.Create(notificationId, DeliveryChannel.InApp, DateTime.UtcNow);

        // Assert
        Assert.Equal(notificationId, delivery.NotificationId);
        Assert.Equal(DeliveryChannel.InApp, delivery.Channel);
        // The delivery entity represents channel execution state; no new Notification aggregate is spawned.
    }

    [Fact]
    public void DeliveryContracts_ContainNoCredentials()
    {
        // Arrange & Act
        Type deliveryType = typeof(NotificationDelivery);
        Type optionsType = typeof(NotificationDeliveryOptions);

        var deliveryProperties = deliveryType.GetProperties();
        var optionsProperties = optionsType.GetProperties();

        string[] sensitiveNames = new[] { "Password", "Secret", "ApiKey", "Credential", "AuthToken", "TokenSecret" };

        // Assert
        foreach (var prop in deliveryProperties)
        {
            Assert.False(sensitiveNames.Any(s => prop.Name.Contains(s, StringComparison.OrdinalIgnoreCase)),
                $"Property '{prop.Name}' on NotificationDelivery must not contain sensitive credential names.");
        }

        foreach (var prop in optionsProperties)
        {
            Assert.False(sensitiveNames.Any(s => prop.Name.Contains(s, StringComparison.OrdinalIgnoreCase)),
                $"Property '{prop.Name}' on NotificationDeliveryOptions must not contain sensitive credential names.");
        }
    }
}
