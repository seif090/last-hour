using LastHour.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LastHour.Infrastructure.Data.Configurations;

public class CartItemConfiguration : IEntityTypeConfiguration<CartItem>
{
    public void Configure(EntityTypeBuilder<CartItem> builder)
    {
        builder.ToTable("CartItems");
        builder.HasKey(ci => ci.Id);
        builder.Property(ci => ci.Id).HasMaxLength(36).IsRequired();
        builder.Property(ci => ci.UserId).HasMaxLength(36).IsRequired();
        builder.Property(ci => ci.OfferId).HasMaxLength(36).IsRequired();
        builder.HasOne(ci => ci.Offer).WithMany(o => o.CartItems).HasForeignKey(ci => ci.OfferId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(ci => new { ci.UserId, ci.OfferId }).IsUnique();
    }
}

public class CategoryConfiguration : BaseEntityConfiguration<Category>
{
    public override void Configure(EntityTypeBuilder<Category> builder)
    {
        base.Configure(builder);
        builder.ToTable("Categories");
        builder.Property(c => c.Name).HasMaxLength(100).IsRequired();
        builder.Property(c => c.ImageUrl).HasMaxLength(1000);
        builder.Property(c => c.Icon).HasMaxLength(50);
        builder.Property(c => c.Color).HasMaxLength(50);
        builder.HasIndex(c => c.Name).IsUnique();
    }
}

public class CouponConfiguration : BaseEntityConfiguration<Coupon>
{
    public override void Configure(EntityTypeBuilder<Coupon> builder)
    {
        base.Configure(builder);
        builder.ToTable("Coupons");
        builder.Property(c => c.Code).HasMaxLength(50).IsRequired();
        builder.Property(c => c.DiscountPercentage).HasColumnType("decimal(5,2)");
        builder.Property(c => c.MaxDiscountAmount).HasColumnType("decimal(18,2)");
        builder.Property(c => c.MinOrderAmount).HasColumnType("decimal(18,2)");
        builder.HasIndex(c => c.Code).IsUnique();
    }
}

public class ReviewConfiguration : IEntityTypeConfiguration<Review>
{
    public void Configure(EntityTypeBuilder<Review> builder)
    {
        builder.ToTable("Reviews");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).HasMaxLength(36).IsRequired();
        builder.Property(r => r.UserId).HasMaxLength(36).IsRequired();
        builder.Property(r => r.StoreId).HasMaxLength(36);
        builder.Property(r => r.OfferId).HasMaxLength(36);
        builder.Property(r => r.Comment).HasMaxLength(2000);
        builder.HasIndex(r => r.StoreId);
        builder.HasIndex(r => r.OfferId);
        builder.HasIndex(r => r.UserId);
    }
}

public class AddressConfiguration : IEntityTypeConfiguration<Address>
{
    public void Configure(EntityTypeBuilder<Address> builder)
    {
        builder.ToTable("Addresses");
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).HasMaxLength(36).IsRequired();
        builder.Property(a => a.UserId).HasMaxLength(36).IsRequired();
        builder.Property(a => a.Label).HasMaxLength(100).IsRequired();
        builder.Property(a => a.Street).HasMaxLength(200).IsRequired();
        builder.Property(a => a.Building).HasMaxLength(50);
        builder.Property(a => a.Apartment).HasMaxLength(50);
        builder.Property(a => a.Floor).HasMaxLength(50);
        builder.Property(a => a.Landmark).HasMaxLength(200);
        builder.Property(a => a.City).HasMaxLength(100).IsRequired();
        builder.Property(a => a.State).HasMaxLength(100);
        builder.Property(a => a.ZipCode).HasMaxLength(20);
        builder.HasIndex(a => a.UserId);
    }
}

public class FavoriteConfiguration : IEntityTypeConfiguration<Favorite>
{
    public void Configure(EntityTypeBuilder<Favorite> builder)
    {
        builder.ToTable("Favorites");
        builder.HasKey(f => f.Id);
        builder.Property(f => f.Id).HasMaxLength(36).IsRequired();
        builder.Property(f => f.UserId).HasMaxLength(36).IsRequired();
        builder.Property(f => f.StoreId).HasMaxLength(36);
        builder.Property(f => f.OfferId).HasMaxLength(36);
        builder.HasIndex(f => new { f.UserId, f.StoreId });
        builder.HasIndex(f => new { f.UserId, f.OfferId });
    }
}

public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable("Notifications");
        builder.HasKey(n => n.Id);
        builder.Property(n => n.Id).HasMaxLength(36).IsRequired();
        builder.Property(n => n.UserId).HasMaxLength(36).IsRequired();
        builder.Property(n => n.Title).HasMaxLength(200).IsRequired();
        builder.Property(n => n.Body).HasMaxLength(2000);
        builder.Property(n => n.Type).HasMaxLength(50);
        builder.Property(n => n.Data).HasMaxLength(4000);
        builder.HasIndex(n => n.UserId);
        builder.HasIndex(n => n.IsRead);
    }
}

public class PaymentTransactionConfiguration : IEntityTypeConfiguration<PaymentTransaction>
{
    public void Configure(EntityTypeBuilder<PaymentTransaction> builder)
    {
        builder.ToTable("PaymentTransactions");
        builder.HasKey(pt => pt.Id);
        builder.Property(pt => pt.Id).HasMaxLength(36).IsRequired();
        builder.Property(pt => pt.OrderId).HasMaxLength(36).IsRequired();
        builder.Property(pt => pt.Amount).HasColumnType("decimal(18,2)").IsRequired();
        builder.Property(pt => pt.Currency).HasMaxLength(3).IsRequired();
        builder.Property(pt => pt.PaymentMethod).HasMaxLength(50).IsRequired();
        builder.Property(pt => pt.Status).HasMaxLength(50).IsRequired();
        builder.Property(pt => pt.TransactionId).HasMaxLength(200);
        builder.Property(pt => pt.GatewayResponse).HasMaxLength(4000);
        builder.HasIndex(pt => pt.OrderId);
        builder.HasIndex(pt => pt.TransactionId);
    }
}

public class UserProfileConfiguration : IEntityTypeConfiguration<UserProfile>
{
    public void Configure(EntityTypeBuilder<UserProfile> builder)
    {
        builder.ToTable("UserProfiles");
        builder.HasKey(up => up.Id);
        builder.Property(up => up.Id).HasMaxLength(36).IsRequired();
        builder.Property(up => up.IdentityId).HasMaxLength(36).IsRequired();
        builder.Property(up => up.FullName).HasMaxLength(200).IsRequired();
        builder.Property(up => up.Email).HasMaxLength(200).IsRequired();
        builder.Property(up => up.Phone).HasMaxLength(20);
        builder.Property(up => up.AvatarUrl).HasMaxLength(1000);
        builder.HasIndex(up => up.IdentityId).IsUnique();
        builder.HasIndex(up => up.Email).IsUnique();
    }
}

public class RefreshTokenConfiguration : IEntityTypeConfiguration<LastHour.Domain.Entities.RefreshToken>
{
    public void Configure(EntityTypeBuilder<LastHour.Domain.Entities.RefreshToken> builder)
    {
        builder.ToTable("RefreshTokens");
        builder.HasKey(rt => rt.Id);
        builder.Property(rt => rt.Id).HasMaxLength(36).IsRequired();
        builder.Property(rt => rt.UserId).HasMaxLength(36).IsRequired();
        builder.Property(rt => rt.Token).HasMaxLength(500).IsRequired();
        builder.Property(rt => rt.JwtId).HasMaxLength(100).IsRequired();
        builder.HasIndex(rt => rt.UserId);
        builder.HasIndex(rt => rt.Token);
        builder.HasIndex(rt => rt.JwtId);
    }
}
