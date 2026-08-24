using LastHour.Modules.Notifications.Domain.Notifications;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LastHour.Modules.Notifications.Persistence;

/// <summary>
/// Configures the relational shape of the <see cref="Notification"/> entity.
/// </summary>
public sealed class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable("notifications");

        builder.HasKey(notification => notification.Id);

        builder.Property(notification => notification.Id)
            .ValueGeneratedNever();

        builder.Property(notification => notification.EventId);

        builder.Property(notification => notification.RecipientId)
            .IsRequired();

        builder.Property(notification => notification.Recipient)
            .HasMaxLength(Notification.MaxRecipientLength)
            .IsRequired();

        builder.Property(notification => notification.Title)
            .HasMaxLength(Notification.MaxTitleLength)
            .IsRequired();

        builder.Property(notification => notification.Body)
            .HasMaxLength(Notification.MaxBodyLength)
            .IsRequired();

        builder.Property(notification => notification.Type)
            .HasMaxLength(Notification.MaxTypeLength)
            .IsRequired();

        builder.Property(notification => notification.CreatedAtUtc)
            .IsRequired();

        builder.Property(notification => notification.SentAtUtc);

        builder.Property(notification => notification.IsRead)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(notification => notification.ReadAtUtc);

        builder.HasIndex(notification => notification.RecipientId)
            .HasDatabaseName("IX_notifications_recipient_id");

        builder.HasIndex(notification => notification.EventId)
            .HasDatabaseName("IX_notifications_event_id");

        builder.HasIndex(notification => new { notification.EventId, notification.Type })
            .IsUnique()
            .HasFilter("\"EventId\" IS NOT NULL")
            .HasDatabaseName("IX_notifications_event_id_type");

        builder.HasIndex(notification => new { notification.RecipientId, notification.CreatedAtUtc, notification.Id })
            .HasDatabaseName("IX_notifications_recipient_created_at");

        builder.HasIndex(notification => new { notification.RecipientId, notification.IsRead, notification.CreatedAtUtc })
            .HasDatabaseName("IX_notifications_recipient_is_read_created_at");

        builder.Property(notification => notification.CreatedAt).IsRequired();
        builder.Property(notification => notification.CreatedBy).HasMaxLength(64);
        builder.Property(notification => notification.UpdatedAt);
        builder.Property(notification => notification.UpdatedBy).HasMaxLength(64);
    }
}
