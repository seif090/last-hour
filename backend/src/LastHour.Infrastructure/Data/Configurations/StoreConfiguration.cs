using LastHour.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LastHour.Infrastructure.Data.Configurations;

public class StoreConfiguration : BaseEntityConfiguration<Store>
{
    public override void Configure(EntityTypeBuilder<Store> builder)
    {
        base.Configure(builder);

        builder.ToTable("Stores");

        builder.Property(s => s.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(s => s.Description)
            .HasMaxLength(2000);

        builder.Property(s => s.Category)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(s => s.Address)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(s => s.Phone)
            .HasMaxLength(20);

        builder.Property(s => s.Email)
            .HasMaxLength(200);

        builder.Property(s => s.Website)
            .HasMaxLength(500);

        builder.Property(s => s.OpeningHours)
            .HasMaxLength(100);

        builder.Property(s => s.ClosingHours)
            .HasMaxLength(100);

        builder.Property(s => s.AcceptedPaymentMethods)
            .HasMaxLength(500);

        builder.Property(s => s.OwnerId)
            .HasMaxLength(36)
            .IsRequired();

        builder.HasIndex(s => s.OwnerId);
        builder.HasIndex(s => s.Category);
        builder.HasIndex(s => s.IsActive);
        builder.HasIndex(s => s.Latitude);
        builder.HasIndex(s => s.Longitude);
    }
}
