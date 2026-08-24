using LastHour.Modules.Notifications.Domain.Notifications;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LastHour.Modules.Notifications.Persistence;

internal sealed class NotificationDeliveryAttemptConfiguration : IEntityTypeConfiguration<NotificationDeliveryAttempt>
{
    public void Configure(EntityTypeBuilder<NotificationDeliveryAttempt> builder)
    {
        builder.ToTable("notification_delivery_attempts");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.Id)
            .ValueGeneratedNever();

        builder.Property(a => a.DeliveryId)
            .IsRequired();

        builder.Property(a => a.AttemptNumber)
            .IsRequired();

        builder.Property(a => a.Channel)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(32);

        builder.Property(a => a.StartedAtUtc)
            .IsRequired();

        builder.Property(a => a.CompletedAtUtc);

        builder.Property(a => a.DurationMilliseconds);

        builder.Property(a => a.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(32);

        builder.Property(a => a.FailureType)
            .HasMaxLength(128);

        builder.Property(a => a.CreatedAtUtc)
            .IsRequired();

        builder.HasOne<NotificationDelivery>()
            .WithMany()
            .HasForeignKey(a => a.DeliveryId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(a => new { a.DeliveryId, a.AttemptNumber })
            .IsUnique()
            .HasDatabaseName("IX_notification_delivery_attempts_delivery_id_attempt_number");

        builder.HasIndex(a => new { a.DeliveryId, a.StartedAtUtc })
            .HasDatabaseName("IX_notification_delivery_attempts_delivery_id_started_at");

        builder.HasIndex(a => a.CreatedAtUtc)
            .HasDatabaseName("IX_notification_delivery_attempts_created_at");
    }
}
