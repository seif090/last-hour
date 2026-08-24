using LastHour.Modules.Notifications.Domain.Notifications;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LastHour.Modules.Notifications.Persistence;

internal sealed class NotificationQuietHoursConfiguration : IEntityTypeConfiguration<NotificationQuietHours>
{
    public void Configure(EntityTypeBuilder<NotificationQuietHours> builder)
    {
        builder.ToTable("notification_quiet_hours");

        builder.HasKey(q => q.Id);

        builder.Property(q => q.Id)
            .ValueGeneratedNever();

        builder.Property(q => q.UserId)
            .IsRequired();

        builder.Property(q => q.Enabled)
            .IsRequired();

        builder.Property(q => q.StartLocalTime)
            .IsRequired();

        builder.Property(q => q.EndLocalTime)
            .IsRequired();

        builder.Property(q => q.TimeZoneId)
            .IsRequired()
            .HasMaxLength(64);

        builder.Property(q => q.CreatedAtUtc)
            .IsRequired();

        builder.Property(q => q.UpdatedAtUtc)
            .IsRequired();

        builder.HasIndex(q => q.UserId)
            .IsUnique()
            .HasDatabaseName("IX_notification_quiet_hours_user_id");
    }
}
