using LastHour.Modules.Merchant.Domain.Merchants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MerchantAggregate = LastHour.Modules.Merchant.Domain.Merchants.Merchant;

namespace LastHour.Modules.Merchant.Persistence;

/// <summary>
/// Configures the relational shape of the <see cref="MerchantAggregate"/> aggregate root.
/// The strongly typed identifier is converted automatically through shared model conventions,
/// value objects are mapped to columns via owned entity types, and indexes support efficient lookups.
/// </summary>
public sealed class MerchantConfiguration : IEntityTypeConfiguration<MerchantAggregate>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<MerchantAggregate> builder)
    {
        builder.ToTable("merchants");

        builder.OwnsOne(merchant => merchant.Name, name =>
        {
            name.Property(value => value.Value)
                .HasColumnName("Name")
                .HasMaxLength(MerchantName.MaxLength)
                .IsRequired();

            name.HasIndex(value => value.Value)
                .IsUnique()
                .HasDatabaseName("IX_merchants_name");
        });

        builder.OwnsOne(merchant => merchant.Address, address =>
        {
            address.Property(value => value.Street)
                .HasColumnName("Street")
                .HasMaxLength(200)
                .IsRequired();

            address.Property(value => value.City)
                .HasColumnName("City")
                .HasMaxLength(100)
                .IsRequired();

            address.Property(value => value.StateOrProvince)
                .HasColumnName("StateOrProvince")
                .HasMaxLength(100);

            address.Property(value => value.PostalCode)
                .HasColumnName("PostalCode")
                .HasMaxLength(20);

            address.Property(value => value.Country)
                .HasColumnName("Country")
                .HasMaxLength(100)
                .IsRequired();
        });

        builder.OwnsOne(merchant => merchant.Phone, phone =>
        {
            phone.Property(value => value.Value)
                .HasColumnName("Phone")
                .HasMaxLength(PhoneNumber.MaxLength)
                .IsRequired();
        });

        builder.Property(merchant => merchant.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.HasIndex(merchant => merchant.Status)
            .HasDatabaseName("IX_merchants_status");

        builder.Property(merchant => merchant.OwnerId)
            .IsRequired();

        builder.HasIndex(merchant => merchant.OwnerId)
            .HasDatabaseName("IX_merchants_owner_id");

        builder.Property(merchant => merchant.CreatedAt).IsRequired();
        builder.Property(merchant => merchant.CreatedBy).HasMaxLength(64);
        builder.Property(merchant => merchant.UpdatedBy).HasMaxLength(64);
    }
}
