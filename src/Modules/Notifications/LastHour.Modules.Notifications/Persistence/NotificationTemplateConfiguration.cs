using LastHour.Modules.Notifications.Domain.Notifications;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LastHour.Modules.Notifications.Persistence;

internal sealed class NotificationTemplateConfiguration : IEntityTypeConfiguration<NotificationTemplate>
{
    public void Configure(EntityTypeBuilder<NotificationTemplate> builder)
    {
        builder.ToTable("notification_templates");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Id)
            .ValueGeneratedNever();

        builder.Property(t => t.TemplateKey)
            .IsRequired()
            .HasMaxLength(128);

        builder.Property(t => t.NotificationType)
            .IsRequired()
            .HasMaxLength(128);

        builder.Property(t => t.Channel)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(32);

        builder.Property(t => t.Version)
            .IsRequired();

        builder.Property(t => t.Subject)
            .HasMaxLength(256);

        builder.Property(t => t.Body)
            .IsRequired()
            .HasMaxLength(4000);

        builder.Property(t => t.IsActive)
            .IsRequired();

        builder.Property(t => t.CreatedAtUtc)
            .IsRequired();

        builder.Property(t => t.UpdatedAtUtc)
            .IsRequired();

        builder.HasIndex(t => new { t.TemplateKey, t.Channel, t.Version })
            .IsUnique()
            .HasDatabaseName("IX_notification_templates_key_channel_version");

        builder.HasIndex(t => new { t.NotificationType, t.Channel, t.IsActive })
            .HasDatabaseName("IX_notification_templates_type_channel_active");
    }
}
