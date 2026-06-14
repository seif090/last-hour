using LastHour.Domain.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LastHour.Infrastructure.Data.Configurations;

public class BaseEntityConfiguration<T> : IEntityTypeConfiguration<T> where T : BaseEntity
{
    public virtual void Configure(EntityTypeBuilder<T> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasMaxLength(36)
            .IsRequired();

        builder.Property(e => e.CreatedAt)
            .IsRequired();

        builder.Property(e => e.UpdatedAt)
            .IsRequired(false);

        builder.Property(e => e.CreatedBy)
            .HasMaxLength(36)
            .IsRequired(false);

        builder.Property(e => e.UpdatedBy)
            .HasMaxLength(36)
            .IsRequired(false);

        builder.HasQueryFilter(e => !e.IsDeleted);

        builder.Property(e => e.IsDeleted)
            .HasDefaultValue(false);

        builder.Property(e => e.DeletedAt)
            .IsRequired(false);
    }
}
