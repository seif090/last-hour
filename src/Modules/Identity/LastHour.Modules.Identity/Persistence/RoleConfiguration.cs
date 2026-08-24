using LastHour.Modules.Identity.Domain.Roles;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LastHour.Modules.Identity.Persistence;

/// <summary>
/// Configures the relational shape of the <see cref="Role"/> aggregate and the two
/// association tables it participates in. The unique index on the normalized role name is the
/// single enforcement point for role uniqueness (case-insensitive), and the composite primary
/// keys of <c>user_roles</c> and <c>role_permissions</c> make duplicate assignments and grants
/// impossible even under concurrency. Referential actions are deliberately restrictive: a role
/// that is assigned to a user or holds permissions cannot be deleted, and a permission that is
/// granted cannot be deleted — authorization state can never be orphaned silently, and
/// PostgreSQL's single-cascade-path constraint is respected.
/// </summary>
public sealed class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.ToTable("roles");

        builder.OwnsOne(role => role.Name, name =>
        {
            name.Property(value => value.Value)
                .HasColumnName("Name")
                .HasMaxLength(RoleName.MaxLength)
                .IsRequired();

            name.Property(value => value.NormalizedValue)
                .HasColumnName("NormalizedName")
                .HasMaxLength(RoleName.MaxLength)
                .IsRequired();

            name.HasIndex(value => value.NormalizedValue)
                .IsUnique()
                .HasDatabaseName("IX_roles_normalized_name");
        });

        builder.Property(role => role.Description)
            .HasMaxLength(Role.MaxDescriptionLength);

        builder.Property(role => role.CreatedAt).IsRequired();
        builder.Property(role => role.CreatedBy).HasMaxLength(64);
        builder.Property(role => role.UpdatedBy).HasMaxLength(64);

        // The role side of the user assignments and the permission grants; the join-entity
        // side (tables, composite keys, the user/permission inverse relationships) lives in
        // the dedicated UserRole and RolePermissionGrant configurations.
        builder.HasMany(role => role.UserRoles)
            .WithOne()
            .HasForeignKey(assignment => assignment.RoleId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(role => role.RolePermissions)
            .WithOne(grant => grant.Role)
            .HasForeignKey(grant => grant.RoleId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
