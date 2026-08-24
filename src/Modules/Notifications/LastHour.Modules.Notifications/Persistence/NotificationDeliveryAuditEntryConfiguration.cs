using LastHour.Modules.Notifications.Domain.Notifications;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LastHour.Modules.Notifications.Persistence;

internal sealed class NotificationDeliveryAuditEntryConfiguration : IEntityTypeConfiguration<NotificationDeliveryAuditEntry>
{
    public void Configure(EntityTypeBuilder<NotificationDeliveryAuditEntry> builder)
    {
        builder.ToTable("notification_delivery_audit_entries");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .ValueGeneratedNever();

        builder.Property(e => e.DeliveryId)
            .IsRequired();

        builder.Property(e => e.Action)
            .IsRequired()
            .HasMaxLength(64);

        builder.Property(e => e.PreviousStatus)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(32);

        builder.Property(e => e.NewStatus)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(32);

        builder.Property(e => e.ActorId)
            .IsRequired();

        builder.Property(e => e.Reason)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(e => e.OccurredAtUtc)
            .IsRequired();

        builder.Property(e => e.CorrelationId)
            .HasMaxLength(128);

        builder.HasOne<NotificationDelivery>()
            .WithMany()
            .HasForeignKey(e => e.DeliveryId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(e => new { e.DeliveryId, e.OccurredAtUtc })
            .HasDatabaseName("IX_notification_delivery_audit_entries_delivery_id_occurred_at");

        builder.HasIndex(e => e.OccurredAtUtc)
            .HasDatabaseName("IX_notification_delivery_audit_entries_occurred_at");

        builder.HasIndex(e => new { e.ActorId, e.OccurredAtUtc })
            .HasDatabaseName("IX_notification_delivery_audit_entries_actor_id_occurred_at");
    }
}
