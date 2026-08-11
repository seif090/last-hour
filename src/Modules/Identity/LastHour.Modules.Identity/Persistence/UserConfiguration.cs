using LastHour.Modules.Identity.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LastHour.Modules.Identity.Persistence;

/// <summary>
/// Configures the relational shape of the <see cref="User"/> aggregate. Column names follow
/// the repository convention (PascalCase property names; snake_case table name), the id maps
/// to <c>uuid</c> through the shared strongly typed id converters, and the normalized email
/// column carries the database-level unique constraint that makes duplicate addresses
/// impossible even under concurrency.
/// </summary>
public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");

        // The strongly typed id (UserId) is deliberately not configured here: the shared
        // model convention (ApplyStronglyTypedIdConverters) discovers id-typed properties and
        // wires the value converter. Declaring the key explicitly would mark the property as
        // already mapped and the convention would skip it.

        // The unique constraint on the normalized representation is the single enforcement
        // point for address uniqueness: "User@Example.com" and "user@example.com" collide
        // here, so a concurrent double-registration is rejected by the database, not by an
        // application-level existence check.
        builder.OwnsOne(user => user.Email, email =>
        {
            email.Property(address => address.Value)
                .HasColumnName("Email")
                .HasMaxLength(Email.MaxLength)
                .IsRequired();

            email.Property(address => address.NormalizedValue)
                .HasColumnName("NormalizedEmail")
                .HasMaxLength(Email.MaxLength)
                .IsRequired();

            email.HasIndex(address => address.NormalizedValue)
                .IsUnique()
                .HasDatabaseName("IX_users_normalized_email");
        });

        builder.OwnsOne(user => user.PasswordCredential, credential =>
        {
            credential.Property(value => value.Hash)
                .HasColumnName("PasswordHash")
                .HasMaxLength(PasswordCredential.MaxHashLength)
                .IsRequired();
        });

        builder.Property(user => user.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(user => user.Version)
            .IsConcurrencyToken();

        builder.Property(user => user.CreatedAt).IsRequired();
        builder.Property(user => user.CreatedBy).HasMaxLength(64);
        builder.Property(user => user.UpdatedBy).HasMaxLength(64);
    }
}
