using LastHour.Modules.Identity.Domain.Roles;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LastHour.Modules.Identity.Persistence;

/// <summary>
/// Configures the relational shape of the <see cref="RolePermissionGrant"/> association. The
/// composite primary key makes a duplicate grant between the same role and permission
/// impossible at the database, and the foreign keys reference the role and permission rows.
/// Deleting a permission that is still granted is prevented (restrict), so authorization state
/// is never orphaned; the role side is likewise restrictive (configured in
/// <see cref="RoleConfiguration"/>).
/// </summary>
public sealed class RolePermissionGrantConfiguration : IEntityTypeConfiguration<RolePermissionGrant>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<RolePermissionGrant> builder)
    {
        builder.ToTable("role_permissions");

        builder.HasKey(grant => new { grant.RoleId, grant.PermissionId });

        builder.HasOne<Permission>()
            .WithMany(permission => permission.RolePermissions)
            .HasForeignKey(grant => grant.PermissionId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
