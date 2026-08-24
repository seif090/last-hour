using LastHour.Modules.Identity.Domain.Roles;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LastHour.Modules.Identity.Persistence;

/// <summary>
/// Configures the relational shape of the <see cref="Permission"/> aggregate. The unique index
/// on the code is the single enforcement point for capability identity: a permission code is a
/// stable <c>Resource.Action</c> value, and the database guarantees no two permissions share
/// it. The grants relationship (permissions granted to roles) is configured on the
/// <see cref="Role"/> side in <see cref="RoleConfiguration"/>.
/// </summary>
public sealed class PermissionConfiguration : IEntityTypeConfiguration<Permission>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<Permission> builder)
    {
        builder.ToTable("permissions");

        builder.Property(permission => permission.Code)
            .HasMaxLength(Permission.MaxCodeLength)
            .IsRequired();

        builder.HasIndex(permission => permission.Code)
            .IsUnique()
            .HasDatabaseName("IX_permissions_code");

        builder.Property(permission => permission.Description)
            .HasMaxLength(256);

        builder.Property(permission => permission.CreatedAt).IsRequired();
        builder.Property(permission => permission.CreatedBy).HasMaxLength(64);
        builder.Property(permission => permission.UpdatedBy).HasMaxLength(64);
    }
}
