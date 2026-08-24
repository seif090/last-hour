using LastHour.Modules.Notifications.Domain.Notifications;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LastHour.Modules.Notifications.Persistence;

internal sealed class NotificationPreferenceConfiguration : IEntityTypeConfiguration<NotificationPreference>
{
    public void Configure(EntityTypeBuilder<NotificationPreference> builder)
    {
        builder.ToTable("notification_preferences");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id)
            .ValueGeneratedNever();

        builder.Property(p => p.UserId)
            .IsRequired();

        builder.Property(p => p.NotificationType)
            .IsRequired()
            .HasMaxLength(128);

        builder.Property(p => p.Channel)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(32);

        builder.Property(p => p.IsEnabled)
            .IsRequired();

        builder.Property(p => p.CreatedAtUtc)
            .IsRequired();

        builder.Property(p => p.UpdatedAtUtc)
            .IsRequired();

        builder.HasIndex(p => new { p.UserId, p.NotificationType, p.Channel })
            .IsUnique()
            .HasDatabaseName("IX_notification_preferences_user_type_channel");

        builder.HasIndex(p => new { p.UserId, p.NotificationType })
            .HasDatabaseName("IX_notification_preferences_user_type");
    }
}
