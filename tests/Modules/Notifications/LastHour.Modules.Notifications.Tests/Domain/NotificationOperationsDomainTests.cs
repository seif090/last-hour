using LastHour.Modules.Notifications.Domain.Notifications;

namespace LastHour.Modules.Notifications.Tests.Domain;

public class NotificationOperationsDomainTests
{
    [Fact]
    public void RequeueByAdmin_RejectsEmptyActorId()
    {
        var delivery = NotificationDelivery.Create(Guid.NewGuid(), DeliveryChannel.Email, DateTime.UtcNow);
        delivery.RecordFailure("Error", DateTime.UtcNow, 1, TimeSpan.FromSeconds(10));

        Assert.Throws<ArgumentException>(() =>
            delivery.RequeueByAdmin(Guid.Empty, "Valid reason", DateTime.UtcNow));
    }

    [Fact]
    public void RequeueByAdmin_RejectsEmptyReason()
    {
        var delivery = NotificationDelivery.Create(Guid.NewGuid(), DeliveryChannel.Email, DateTime.UtcNow);
        delivery.RecordFailure("Error", DateTime.UtcNow, 1, TimeSpan.FromSeconds(10));

        Assert.Throws<ArgumentException>(() =>
            delivery.RequeueByAdmin(Guid.NewGuid(), "", DateTime.UtcNow));

        Assert.Throws<ArgumentException>(() =>
            delivery.RequeueByAdmin(Guid.NewGuid(), "   ", DateTime.UtcNow));
    }

    [Fact]
    public void RequeueByAdmin_DeliveredDelivery_CannotBeRequeued()
    {
        var delivery = NotificationDelivery.Create(Guid.NewGuid(), DeliveryChannel.Email, DateTime.UtcNow);
        delivery.MarkDelivered(DateTime.UtcNow);

        Assert.Throws<InvalidOperationException>(() =>
            delivery.RequeueByAdmin(Guid.NewGuid(), "Admin override attempt", DateTime.UtcNow));
    }

    [Fact]
    public void RequeueByAdmin_ProcessingDeliveryWithActiveLease_CannotBeRequeued()
    {
        DateTime now = DateTime.UtcNow;
        var delivery = NotificationDelivery.Create(Guid.NewGuid(), DeliveryChannel.Email, now);
        delivery.Claim(Guid.NewGuid(), now.AddMinutes(5), now); // Active lease for 5 minutes

        Assert.Throws<InvalidOperationException>(() =>
            delivery.RequeueByAdmin(Guid.NewGuid(), "Admin override attempt", now.AddMinutes(1)));
    }

    [Fact]
    public void RequeueByAdmin_ProcessingDeliveryWithExpiredLease_CanBeRequeued()
    {
        DateTime now = DateTime.UtcNow;
        var delivery = NotificationDelivery.Create(Guid.NewGuid(), DeliveryChannel.Email, now);
        delivery.Claim(Guid.NewGuid(), now.AddMinutes(5), now); // Lease expires at now + 5 min

        DateTime at6Min = now.AddMinutes(6); // Expired lease
        Guid actorId = Guid.NewGuid();

        delivery.RequeueByAdmin(actorId, "Reclaim expired processing lease", at6Min);

        Assert.Equal(DeliveryStatus.Pending, delivery.Status);
        Assert.Equal(at6Min, delivery.NextAttemptAtUtc);
        Assert.Null(delivery.ClaimToken);
        Assert.Null(delivery.LockExpiresAtUtc);
    }

    [Fact]
    public void RequeueByAdmin_FailedPermanentDelivery_CanBeRequeued()
    {
        DateTime now = DateTime.UtcNow;
        var delivery = NotificationDelivery.Create(Guid.NewGuid(), DeliveryChannel.Email, now);
        delivery.RecordFailure("Permanent failure", now, 1, TimeSpan.FromMinutes(1)); // maxRetryCount = 1 -> FailedPermanent

        Assert.Equal(DeliveryStatus.FailedPermanent, delivery.Status);

        Guid actorId = Guid.NewGuid();
        delivery.RequeueByAdmin(actorId, "Admin recovery", now.AddMinutes(10));

        Assert.Equal(DeliveryStatus.Pending, delivery.Status);
    }

    [Fact]
    public void RequeueByAdmin_RetryScheduledDelivery_CanBeRequeued()
    {
        DateTime now = DateTime.UtcNow;
        var delivery = NotificationDelivery.Create(Guid.NewGuid(), DeliveryChannel.Email, now);
        delivery.RecordFailure("Transient failure", now, 3, TimeSpan.FromMinutes(5));

        Assert.Equal(DeliveryStatus.RetryScheduled, delivery.Status);

        Guid actorId = Guid.NewGuid();
        delivery.RequeueByAdmin(actorId, "Manual requeue before scheduled time", now.AddMinutes(1));

        Assert.Equal(DeliveryStatus.Pending, delivery.Status);
    }

    [Fact]
    public void RequeueByAdmin_ResetsRetryCountToZero()
    {
        DateTime now = DateTime.UtcNow;
        var delivery = NotificationDelivery.Create(Guid.NewGuid(), DeliveryChannel.Email, now);
        delivery.RecordFailure("Error 1", now, 3, TimeSpan.FromMinutes(1));

        Assert.Equal(1, delivery.RetryCount);

        delivery.RequeueByAdmin(Guid.NewGuid(), "Reset retry budget", now);

        Assert.Equal(0, delivery.RetryCount);
    }

    [Fact]
    public void RequeueByAdmin_ClearsLastError()
    {
        DateTime now = DateTime.UtcNow;
        var delivery = NotificationDelivery.Create(Guid.NewGuid(), DeliveryChannel.Email, now);
        delivery.RecordFailure("Critical error description", now, 3, TimeSpan.FromMinutes(1));

        Assert.Equal("Critical error description", delivery.LastError);

        delivery.RequeueByAdmin(Guid.NewGuid(), "Clear error log", now);

        Assert.Null(delivery.LastError);
    }

    [Fact]
    public void RequeueByAdmin_ClearsClaimToken()
    {
        DateTime now = DateTime.UtcNow;
        var delivery = NotificationDelivery.Create(Guid.NewGuid(), DeliveryChannel.Email, now);
        Guid claimToken = Guid.NewGuid();
        delivery.Claim(claimToken, now.AddSeconds(-1), now); // Expired claim

        delivery.RequeueByAdmin(Guid.NewGuid(), "Clear claim token", now);

        Assert.Null(delivery.ClaimToken);
    }

    [Fact]
    public void RequeueByAdmin_ClearsLockExpiresAtUtc()
    {
        DateTime now = DateTime.UtcNow;
        var delivery = NotificationDelivery.Create(Guid.NewGuid(), DeliveryChannel.Email, now);
        delivery.Claim(Guid.NewGuid(), now.AddSeconds(-1), now); // Expired lock

        delivery.RequeueByAdmin(Guid.NewGuid(), "Clear lock expiration", now);

        Assert.Null(delivery.LockExpiresAtUtc);
    }

    [Fact]
    public void RequeueByAdmin_SetsNextAttemptAtUtcToUtcNow()
    {
        DateTime now = DateTime.UtcNow;
        DateTime requeuedAt = now.AddMinutes(30);
        var delivery = NotificationDelivery.Create(Guid.NewGuid(), DeliveryChannel.Email, now);
        delivery.RecordFailure("Error", now, 3, TimeSpan.FromHours(2));

        delivery.RequeueByAdmin(Guid.NewGuid(), "Set immediate attempt", requeuedAt);

        Assert.Equal(requeuedAt, delivery.NextAttemptAtUtc);
    }

    [Fact]
    public void RequeueByAdmin_PreservesDeliveryId()
    {
        DateTime now = DateTime.UtcNow;
        var delivery = NotificationDelivery.Create(Guid.NewGuid(), DeliveryChannel.Email, now);
        Guid originalId = delivery.Id;

        delivery.RecordFailure("Error", now, 1, TimeSpan.FromMinutes(1));
        delivery.RequeueByAdmin(Guid.NewGuid(), "Preserve identity check", now.AddMinutes(5));

        Assert.Equal(originalId, delivery.Id);
    }

    [Fact]
    public void RequeueByAdmin_DoesNotCreateSecondDelivery()
    {
        DateTime now = DateTime.UtcNow;
        var delivery = NotificationDelivery.Create(Guid.NewGuid(), DeliveryChannel.Email, now);
        Guid notifId = delivery.NotificationId;

        delivery.RequeueByAdmin(Guid.NewGuid(), "In-place state update", now);

        Assert.Equal(notifId, delivery.NotificationId);
        Assert.Equal(DeliveryStatus.Pending, delivery.Status);
    }

    [Fact]
    public void NotificationDeliveryAttempt_RejectsInvalidDeliveryId()
    {
        Assert.Throws<ArgumentException>(() =>
            NotificationDeliveryAttempt.Create(Guid.Empty, 1, DeliveryChannel.Email, DateTime.UtcNow));
    }

    [Fact]
    public void NotificationDeliveryAttempt_RejectsAttemptNumberLessThanOne()
    {
        Guid deliveryId = Guid.NewGuid();

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            NotificationDeliveryAttempt.Create(deliveryId, 0, DeliveryChannel.Email, DateTime.UtcNow));

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            NotificationDeliveryAttempt.Create(deliveryId, -1, DeliveryChannel.Email, DateTime.UtcNow));
    }

    [Fact]
    public void NotificationDeliveryAttempt_CompletionCalculatesDurationCorrectly()
    {
        DateTime startedAt = new DateTime(2026, 8, 20, 10, 0, 0, DateTimeKind.Utc);
        DateTime completedAt = startedAt.AddMilliseconds(1250);

        var attempt = NotificationDeliveryAttempt.Create(Guid.NewGuid(), 1, DeliveryChannel.Email, startedAt);
        attempt.Complete(DeliveryStatus.Delivered, completedAt);

        Assert.Equal(DeliveryStatus.Delivered, attempt.Status);
        Assert.Equal(completedAt, attempt.CompletedAtUtc);
        Assert.Equal(1250, attempt.DurationMilliseconds);
    }

    [Fact]
    public void NotificationDeliveryAttempt_DoesNotAllowNegativeDuration()
    {
        DateTime startedAt = new DateTime(2026, 8, 20, 10, 0, 0, DateTimeKind.Utc);
        DateTime earlierAt = startedAt.AddMinutes(-1);

        var attempt = NotificationDeliveryAttempt.Create(Guid.NewGuid(), 1, DeliveryChannel.Email, startedAt);

        Assert.Throws<ArgumentException>(() =>
            attempt.Complete(DeliveryStatus.FailedPermanent, earlierAt));
    }

    [Fact]
    public void NotificationDeliveryAuditEntry_RejectsEmptyActorId()
    {
        Guid deliveryId = Guid.NewGuid();
        DateTime now = DateTime.UtcNow;

        Assert.Throws<ArgumentException>(() =>
            NotificationDeliveryAuditEntry.Create(deliveryId, "Requeued", DeliveryStatus.FailedPermanent, DeliveryStatus.Pending, Guid.Empty, "Reason", now));
    }

    [Fact]
    public void NotificationDeliveryAuditEntry_RejectsEmptyDeliveryId()
    {
        Guid actorId = Guid.NewGuid();
        DateTime now = DateTime.UtcNow;

        Assert.Throws<ArgumentException>(() =>
            NotificationDeliveryAuditEntry.Create(Guid.Empty, "Requeued", DeliveryStatus.FailedPermanent, DeliveryStatus.Pending, actorId, "Reason", now));
    }

    [Fact]
    public void NotificationDeliveryAuditEntry_RejectsEmptyAction()
    {
        Guid deliveryId = Guid.NewGuid();
        Guid actorId = Guid.NewGuid();
        DateTime now = DateTime.UtcNow;

        Assert.Throws<ArgumentException>(() =>
            NotificationDeliveryAuditEntry.Create(deliveryId, "", DeliveryStatus.FailedPermanent, DeliveryStatus.Pending, actorId, "Reason", now));
    }

    [Fact]
    public void NotificationDeliveryAuditEntry_RejectsEmptyReason()
    {
        Guid deliveryId = Guid.NewGuid();
        Guid actorId = Guid.NewGuid();
        DateTime now = DateTime.UtcNow;

        Assert.Throws<ArgumentException>(() =>
            NotificationDeliveryAuditEntry.Create(deliveryId, "Requeued", DeliveryStatus.FailedPermanent, DeliveryStatus.Pending, actorId, "", now));
    }

    [Fact]
    public void AttemptModel_ContainsNoRecipientPiiFields()
    {
        Type attemptType = typeof(NotificationDeliveryAttempt);
        var propertyNames = attemptType.GetProperties().Select(p => p.Name).ToList();

        string[] forbiddenPiiNames = new[] { "Recipient", "Email", "Phone", "Body", "Template", "Password", "Secret", "Token" };

        foreach (string forbidden in forbiddenPiiNames)
        {
            Assert.DoesNotContain(propertyNames, name => name.Equals(forbidden, StringComparison.OrdinalIgnoreCase));
        }
    }

    [Fact]
    public void AuditModel_ContainsNoRecipientPiiFields()
    {
        Type auditType = typeof(NotificationDeliveryAuditEntry);
        var propertyNames = auditType.GetProperties().Select(p => p.Name).ToList();

        string[] forbiddenPiiNames = new[] { "Recipient", "Email", "Phone", "Body", "Template", "Password", "Secret", "Token" };

        foreach (string forbidden in forbiddenPiiNames)
        {
            Assert.DoesNotContain(propertyNames, name => name.Equals(forbidden, StringComparison.OrdinalIgnoreCase));
        }
    }

    [Fact]
    public void AttemptEntity_IsImmutableAfterCompletion()
    {
        DateTime startedAt = DateTime.UtcNow;
        var attempt = NotificationDeliveryAttempt.Create(Guid.NewGuid(), 1, DeliveryChannel.Email, startedAt);
        attempt.Complete(DeliveryStatus.Delivered, startedAt.AddSeconds(1));

        Assert.Throws<InvalidOperationException>(() =>
            attempt.Complete(DeliveryStatus.FailedPermanent, startedAt.AddSeconds(2)));
    }

    [Fact]
    public void AuditEntity_IsImmutableAfterCreation()
    {
        Guid deliveryId = Guid.NewGuid();
        Guid actorId = Guid.NewGuid();
        DateTime now = DateTime.UtcNow;

        var audit = NotificationDeliveryAuditEntry.Create(deliveryId, "Requeued", DeliveryStatus.FailedPermanent, DeliveryStatus.Pending, actorId, "Manual override", now, "corr-123");

        Assert.Equal(deliveryId, audit.DeliveryId);
        Assert.Equal("Requeued", audit.Action);
        Assert.Equal(DeliveryStatus.FailedPermanent, audit.PreviousStatus);
        Assert.Equal(DeliveryStatus.Pending, audit.NewStatus);
        Assert.Equal(actorId, audit.ActorId);
        Assert.Equal("Manual override", audit.Reason);
        Assert.Equal(now, audit.OccurredAtUtc);
        Assert.Equal("corr-123", audit.CorrelationId);
    }
}
