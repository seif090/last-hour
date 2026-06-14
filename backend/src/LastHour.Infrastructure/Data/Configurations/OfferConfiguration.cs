using LastHour.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LastHour.Infrastructure.Data.Configurations;

public class OfferConfiguration : BaseEntityConfiguration<Offer>
{
    public override void Configure(EntityTypeBuilder<Offer> builder)
    {
        base.Configure(builder);

        builder.ToTable("Offers");

        builder.Property(o => o.Title)
            .HasMaxLength(300)
            .IsRequired();

        builder.Property(o => o.Description)
            .HasMaxLength(2000)
            .IsRequired();

        builder.Property(o => o.Category)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(o => o.OriginalPrice)
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        builder.Property(o => o.DiscountPrice)
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        builder.Property(o => o.ImageUrl)
            .HasMaxLength(1000);

        builder.Property(o => o.ImageUrls)
            .HasMaxLength(4000);

        builder.Property(o => o.StoreId)
            .HasMaxLength(36)
            .IsRequired();

        builder.HasOne(o => o.Store)
            .WithMany(s => s.Offers)
            .HasForeignKey(o => o.StoreId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(o => o.StoreId);
        builder.HasIndex(o => o.Category);
        builder.HasIndex(o => o.IsActive);
        builder.HasIndex(o => o.IsFeatured);
        builder.HasIndex(o => o.ExpiryTime);
    }
}
