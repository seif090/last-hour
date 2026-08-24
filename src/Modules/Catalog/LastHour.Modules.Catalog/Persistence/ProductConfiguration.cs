using LastHour.Modules.Catalog.Domain.Categories;
using LastHour.Modules.Catalog.Domain.Products;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LastHour.Modules.Catalog.Persistence;

/// <summary>
/// Configures the relational shape of the <see cref="Product"/> aggregate. The strongly typed
/// identifier is converted automatically through shared model conventions, SKU uniqueness is
/// enforced at the database level on normalized SKU values, monetary value is mapped into
/// explicit relational columns, and the category foreign key enforces restrictive deletion.
/// </summary>
public sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("products");

        builder.OwnsOne(product => product.Sku, sku =>
        {
            sku.Property(value => value.Value)
                .HasColumnName("Sku")
                .HasMaxLength(Sku.MaxLength)
                .IsRequired();

            sku.Property(value => value.NormalizedValue)
                .HasColumnName("NormalizedSku")
                .HasMaxLength(Sku.MaxLength)
                .IsRequired();

            sku.HasIndex(value => value.NormalizedValue)
                .IsUnique()
                .HasDatabaseName("IX_products_normalized_sku");
        });

        builder.Property(product => product.Name)
            .HasMaxLength(Product.MaxNameLength)
            .IsRequired();

        builder.Property(product => product.Description)
            .HasMaxLength(Product.MaxDescriptionLength);

        builder.OwnsOne(product => product.BasePrice, price =>
        {
            price.Property(value => value.Amount)
                .HasColumnName("PriceAmount")
                .HasPrecision(18, 4)
                .IsRequired();

            price.Property(value => value.Currency)
                .HasColumnName("PriceCurrency")
                .HasMaxLength(3)
                .IsRequired();
        });

        builder.HasOne<Category>()
            .WithMany()
            .HasForeignKey(product => product.CategoryId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired();

        builder.HasIndex(product => product.CategoryId)
            .HasDatabaseName("IX_products_category_id");

        builder.Property(product => product.CreatedAt).IsRequired();
        builder.Property(product => product.CreatedBy).HasMaxLength(64);
        builder.Property(product => product.UpdatedBy).HasMaxLength(64);

        builder.Property(product => product.IsDeleted).IsRequired();
        builder.Property(product => product.DeletedBy).HasMaxLength(64);
    }
}
