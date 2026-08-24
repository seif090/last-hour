using LastHour.Modules.Identity.Domain.Roles;
using LastHour.Modules.Identity.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LastHour.Modules.Identity.Persistence;

/// <summary>
/// Configures the relational shape of the <see cref="UserRole"/> association. The composite
/// primary key makes a duplicate assignment between the same user and role impossible at the
/// database, and the foreign keys reference the user and role rows. A user's assignments are
/// deleted with the account (cascade), while deleting a role that is still assigned is
/// prevented (restrict, configured on the role side) — authorization state is never orphaned.
/// </summary>
public sealed class UserRoleConfiguration : IEntityTypeConfiguration<UserRole>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<UserRole> builder)
    {
        builder.ToTable("user_roles");

        builder.HasKey(assignment => new { assignment.UserId, assignment.RoleId });

        builder.HasOne<User>()
            .WithMany(user => user.UserRoles)
            .HasForeignKey(assignment => assignment.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
