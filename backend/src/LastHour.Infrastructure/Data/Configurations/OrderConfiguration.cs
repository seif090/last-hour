using LastHour.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LastHour.Infrastructure.Data.Configurations;

public class OrderConfiguration : BaseEntityConfiguration<Order>
{
    public override void Configure(EntityTypeBuilder<Order> builder)
    {
        base.Configure(builder);

        builder.ToTable("Orders");

        builder.Property(o => o.CustomerId)
            .HasMaxLength(36)
            .IsRequired();

        builder.Property(o => o.StoreId)
            .HasMaxLength(36)
            .IsRequired();

        builder.Property(o => o.Status)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(o => o.Subtotal)
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        builder.Property(o => o.ServiceFee)
            .HasColumnType("decimal(18,2)");

        builder.Property(o => o.DeliveryFee)
            .HasColumnType("decimal(18,2)");

        builder.Property(o => o.Discount)
            .HasColumnType("decimal(18,2)");

        builder.Property(o => o.Total)
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        builder.Property(o => o.PaymentMethod)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(o => o.DeliveryAddress)
            .HasMaxLength(500);

        builder.Property(o => o.CouponCode)
            .HasMaxLength(50);

        builder.Property(o => o.Notes)
            .HasMaxLength(1000);

        builder.Property(o => o.CancellationReason)
            .HasMaxLength(500);

        builder.HasMany(o => o.Items)
            .WithOne()
            .HasForeignKey(oi => oi.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(o => o.CustomerId);
        builder.HasIndex(o => o.StoreId);
        builder.HasIndex(o => o.Status);
        builder.HasIndex(o => o.CreatedAt);
    }
}

public class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
{
    public void Configure(EntityTypeBuilder<OrderItem> builder)
    {
        builder.ToTable("OrderItems");

        builder.HasKey(oi => oi.Id);

        builder.Property(oi => oi.Id)
            .HasMaxLength(36)
            .IsRequired();

        builder.Property(oi => oi.OrderId)
            .HasMaxLength(36)
            .IsRequired();

        builder.Property(oi => oi.OfferId)
            .HasMaxLength(36)
            .IsRequired();

        builder.Property(oi => oi.OfferTitle)
            .HasMaxLength(300)
            .IsRequired();

        builder.Property(oi => oi.Price)
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        builder.Property(oi => oi.OfferImageUrl)
            .HasMaxLength(1000);

        builder.HasIndex(oi => oi.OrderId);
        builder.HasIndex(oi => oi.OfferId);
    }
}
