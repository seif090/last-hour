using LastHour.Modules.Catalog.Domain.Categories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LastHour.Modules.Catalog.Persistence;

/// <summary>
/// Configures the relational shape of the <see cref="Category"/> aggregate. The strongly typed
/// identifier is converted automatically through shared model conventions, and the optional
/// parent category relationship uses restrictive delete behavior to protect category hierarchies
/// from accidental cascading deletion.
/// </summary>
public sealed class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.ToTable("categories");

        builder.Property(category => category.Name)
            .HasMaxLength(Category.MaxNameLength)
            .IsRequired();

        builder.Property(category => category.Description)
            .HasMaxLength(Category.MaxDescriptionLength);

        builder.HasOne<Category>()
            .WithMany()
            .HasForeignKey(category => category.ParentCategoryId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);

        builder.HasIndex(category => category.ParentCategoryId)
            .HasDatabaseName("IX_categories_parent_category_id");

        builder.Property(category => category.CreatedAt).IsRequired();
        builder.Property(category => category.CreatedBy).HasMaxLength(64);
        builder.Property(category => category.UpdatedBy).HasMaxLength(64);
    }
}
