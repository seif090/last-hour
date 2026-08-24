using LastHour.Modules.Notifications.Domain.Notifications;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LastHour.Modules.Notifications.Persistence;

internal sealed class NotificationDeliveryConfiguration : IEntityTypeConfiguration<NotificationDelivery>
{
    public void Configure(EntityTypeBuilder<NotificationDelivery> builder)
    {
        builder.ToTable("notification_deliveries");

        builder.HasKey(d => d.Id);

        builder.Property(d => d.Id)
            .ValueGeneratedNever();

        builder.Property(d => d.NotificationId)
            .IsRequired();

        builder.Property(d => d.Channel)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(32);

        builder.Property(d => d.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(32);

        builder.Property(d => d.RetryCount)
            .IsRequired();

        builder.Property(d => d.MaximumRetryCount)
            .IsRequired();

        builder.Property(d => d.NextAttemptAtUtc);

        builder.Property(d => d.ClaimToken)
            .IsConcurrencyToken();

        builder.Property(d => d.LockExpiresAtUtc);

        builder.Property(d => d.DeliveredAtUtc);

        builder.Property(d => d.FailedAtUtc);

        builder.Property(d => d.LastError)
            .HasMaxLength(1000);

        builder.Property(d => d.CreatedAtUtc)
            .IsRequired();

        builder.Property(d => d.UpdatedAtUtc)
            .IsRequired();

        builder.HasOne<Notification>()
            .WithMany()
            .HasForeignKey(d => d.NotificationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(d => new { d.NotificationId, d.Channel })
            .IsUnique()
            .HasDatabaseName("IX_notification_deliveries_notification_id_channel");

        builder.HasIndex(d => new { d.Status, d.NextAttemptAtUtc })
            .HasDatabaseName("IX_notification_deliveries_status_next_attempt");

        builder.HasIndex(d => new { d.Status, d.LockExpiresAtUtc })
            .HasDatabaseName("IX_notification_deliveries_status_lock_expires");

        builder.HasIndex(d => new { d.Status, d.Channel, d.CreatedAtUtc })
            .HasDatabaseName("IX_notification_deliveries_status_channel_created_at");
    }
}
