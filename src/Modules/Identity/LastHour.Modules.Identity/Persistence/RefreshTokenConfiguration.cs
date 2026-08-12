using LastHour.Modules.Identity.Domain.RefreshTokens;
using LastHour.Modules.Identity.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LastHour.Modules.Identity.Persistence;

/// <summary>
/// Configures the relational shape of the <see cref="RefreshToken"/> records. Column names
/// follow the repository convention (PascalCase property names; snake_case table name), the
/// token hash carries the unique index that makes lookup-by-hash exact and fast, and the
/// family and user columns are indexed for reuse detection and per-user session queries.
/// The raw token secret is never part of the model.
/// </summary>
public sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("refresh_tokens");

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(token => token.UserId)
            .IsRequired();

        builder.Property(token => token.TokenHash)
            .HasColumnName("TokenHash")
            .HasMaxLength(RefreshToken.HashLength)
            .IsRequired();

        builder.Property(token => token.FamilyId)
            .HasColumnName("FamilyId")
            .IsRequired();

        builder.Property(token => token.CreatedAtUtc).HasColumnName("CreatedAtUtc").IsRequired();
        builder.Property(token => token.ExpiresAtUtc).HasColumnName("ExpiresAtUtc").IsRequired();
        builder.Property(token => token.RevokedAtUtc).HasColumnName("RevokedAtUtc");

        builder.Property(token => token.RevocationReason)
            .HasColumnName("RevocationReason")
            .HasConversion<string>()
            .HasMaxLength(20);

        // Lookup by hash is the only path a client ever takes, so it must be unique and exact.
        builder.HasIndex(token => token.TokenHash)
            .IsUnique()
            .HasDatabaseName("IX_refresh_tokens_token_hash");

        // At most one ACTIVE token per family: the database-level invariant that makes token
        // rotation concurrency-safe. The losing writer of a simultaneous rotation fails at its
        // successor insert (unique violation), so two valid replacements can never exist.
        builder.HasIndex(token => token.FamilyId)
            .IsUnique()
            .HasFilter("\"RevokedAtUtc\" IS NULL")
            .HasDatabaseName("IX_refresh_tokens_active_family");

        // Family revocation (reuse detection) and per-user session queries and future
        // revoke-all-for-user also need the user index.
        builder.HasIndex(token => token.UserId)
            .HasDatabaseName("IX_refresh_tokens_user_id");
    }
}
