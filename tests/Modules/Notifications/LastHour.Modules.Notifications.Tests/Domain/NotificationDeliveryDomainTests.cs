using LastHour.Modules.Notifications.Domain.Notifications;
using LastHour.Modules.Notifications.Infrastructure.Delivery;

namespace LastHour.Modules.Notifications.Tests.Domain;

public sealed class NotificationDeliveryDomainTests
{
    [Fact]
    public void Create_ShouldInitializePendingDelivery()
    {
        // Arrange
        Guid notificationId = Guid.NewGuid();
        DateTime now = DateTime.UtcNow;

        // Act
        var delivery = NotificationDelivery.Create(notificationId, DeliveryChannel.Email, now, maximumRetryCount: 3);

        // Assert
        Assert.NotEqual(Guid.Empty, delivery.Id);
        Assert.Equal(notificationId, delivery.NotificationId);
        Assert.Equal(DeliveryChannel.Email, delivery.Channel);
        Assert.Equal(DeliveryStatus.Pending, delivery.Status);
        Assert.Equal(0, delivery.RetryCount);
        Assert.Equal(3, delivery.MaximumRetryCount);
        Assert.Equal(now, delivery.NextAttemptAtUtc);
        Assert.Null(delivery.LockExpiresAtUtc);
        Assert.Null(delivery.ClaimToken);
        Assert.Null(delivery.DeliveredAtUtc);
        Assert.Null(delivery.FailedAtUtc);
        Assert.Null(delivery.LastError);
    }

    [Fact]
    public void Create_WithEmptyNotificationId_ShouldThrowArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            NotificationDelivery.Create(Guid.Empty, DeliveryChannel.InApp, DateTime.UtcNow));
    }

    [Fact]
    public void Claim_ShouldTransitionStatusToProcessingAndSetLease()
    {
        // Arrange
        var delivery = NotificationDelivery.Create(Guid.NewGuid(), DeliveryChannel.InApp, DateTime.UtcNow);
        Guid claimToken = Guid.NewGuid();
        DateTime now = DateTime.UtcNow;
        DateTime leaseExpiresAt = now.AddMinutes(2);

        // Act
        delivery.Claim(claimToken, leaseExpiresAt, now);

        // Assert
        Assert.Equal(DeliveryStatus.Processing, delivery.Status);
        Assert.Equal(claimToken, delivery.ClaimToken);
        Assert.Equal(leaseExpiresAt, delivery.LockExpiresAtUtc);
        Assert.Equal(now, delivery.UpdatedAtUtc);
    }

    [Fact]
    public void Claim_OnDeliveredDelivery_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var delivery = NotificationDelivery.Create(Guid.NewGuid(), DeliveryChannel.InApp, DateTime.UtcNow);
        delivery.MarkDelivered(DateTime.UtcNow);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() =>
            delivery.Claim(Guid.NewGuid(), DateTime.UtcNow.AddMinutes(2), DateTime.UtcNow));
    }

    [Fact]
    public void MarkDelivered_ShouldSetTerminalDeliveredStateAndClearLease()
    {
        // Arrange
        var delivery = NotificationDelivery.Create(Guid.NewGuid(), DeliveryChannel.Sms, DateTime.UtcNow);
        delivery.Claim(Guid.NewGuid(), DateTime.UtcNow.AddMinutes(2), DateTime.UtcNow);
        DateTime deliveredAt = DateTime.UtcNow;

        // Act
        delivery.MarkDelivered(deliveredAt);

        // Assert
        Assert.Equal(DeliveryStatus.Delivered, delivery.Status);
        Assert.Equal(deliveredAt, delivery.DeliveredAtUtc);
        Assert.Null(delivery.LockExpiresAtUtc);
        Assert.Null(delivery.ClaimToken);
        Assert.Null(delivery.NextAttemptAtUtc);
    }

    [Fact]
    public void RecordFailure_WithinRetryLimit_ShouldScheduleRetryWithBackoff()
    {
        // Arrange
        var delivery = NotificationDelivery.Create(Guid.NewGuid(), DeliveryChannel.Email, DateTime.UtcNow, maximumRetryCount: 3);
        DateTime now = DateTime.UtcNow;
        TimeSpan delay = TimeSpan.FromSeconds(30);

        // Act
        delivery.RecordFailure("SMTP connection refused", now, maxRetryCount: 3, retryDelay: delay);

        // Assert
        Assert.Equal(DeliveryStatus.RetryScheduled, delivery.Status);
        Assert.Equal(1, delivery.RetryCount);
        Assert.Equal("SMTP connection refused", delivery.LastError);
        Assert.Equal(now.Add(delay), delivery.NextAttemptAtUtc);
        Assert.Null(delivery.LockExpiresAtUtc);
        Assert.Null(delivery.ClaimToken);
    }

    [Fact]
    public void RecordFailure_ExceedingMaxRetryCount_ShouldTransitionToFailedPermanent()
    {
        // Arrange
        var delivery = NotificationDelivery.Create(Guid.NewGuid(), DeliveryChannel.Email, DateTime.UtcNow, maximumRetryCount: 2);
        DateTime now = DateTime.UtcNow;
        TimeSpan delay = TimeSpan.FromSeconds(10);

        delivery.RecordFailure("Attempt 1 error", now, maxRetryCount: 2, retryDelay: delay);

        // Act
        delivery.RecordFailure("Attempt 2 error", now, maxRetryCount: 2, retryDelay: delay);

        // Assert
        Assert.Equal(DeliveryStatus.FailedPermanent, delivery.Status);
        Assert.Equal(2, delivery.RetryCount);
        Assert.Null(delivery.NextAttemptAtUtc);
        Assert.Equal("Attempt 2 error", delivery.LastError);
    }

    [Fact]
    public void Requeue_OnFailedPermanentDelivery_ShouldResetToPending()
    {
        // Arrange
        var delivery = NotificationDelivery.Create(Guid.NewGuid(), DeliveryChannel.Email, DateTime.UtcNow, maximumRetryCount: 1);
        delivery.RecordFailure("Permanent failure error", DateTime.UtcNow, maxRetryCount: 1, TimeSpan.FromSeconds(10));
        Assert.Equal(DeliveryStatus.FailedPermanent, delivery.Status);

        DateTime requeuedAt = DateTime.UtcNow;

        // Act
        delivery.Requeue(requeuedAt);

        // Assert
        Assert.Equal(DeliveryStatus.Pending, delivery.Status);
        Assert.Equal(0, delivery.RetryCount);
        Assert.Equal(requeuedAt, delivery.NextAttemptAtUtc);
        Assert.Null(delivery.LastError);
    }

    [Fact]
    public void CalculateRetryDelay_ShouldBoundExponentialBackoff()
    {
        // Arrange
        TimeSpan baseDelay = TimeSpan.FromSeconds(10);
        TimeSpan maxDelay = TimeSpan.FromMinutes(60);

        // Act & Assert
        Assert.Equal(TimeSpan.FromSeconds(10), NotificationDeliveryProcessor.CalculateRetryDelay(baseDelay, maxDelay, 0));
        Assert.Equal(TimeSpan.FromSeconds(20), NotificationDeliveryProcessor.CalculateRetryDelay(baseDelay, maxDelay, 1));
        Assert.Equal(TimeSpan.FromSeconds(40), NotificationDeliveryProcessor.CalculateRetryDelay(baseDelay, maxDelay, 2));
        Assert.Equal(maxDelay, NotificationDeliveryProcessor.CalculateRetryDelay(baseDelay, maxDelay, 10));
    }
}
