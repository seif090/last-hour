using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LastHour.BuildingBlocks.Infrastructure.Persistence.Outbox;

/// <summary>
/// Configures the relational shape of <see cref="OutboxMessage"/>.
/// </summary>
public sealed class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("outbox_messages");

        builder.HasKey(message => message.Id);

        builder.Property(message => message.Type)
            .HasMaxLength(512)
            .IsRequired();

        builder.Property(message => message.Content)
            .HasColumnType("jsonb")
            .IsRequired();

        builder.Property(message => message.OccurredOnUtc)
            .IsRequired();

        builder.Property(message => message.ProcessedOnUtc)
            .IsConcurrencyToken();

        builder.Property(message => message.LockExpiresAtUtc)
            .IsConcurrencyToken();

        builder.Property(message => message.NextAttemptAtUtc);

        builder.Property(message => message.Error)
            .HasColumnType("text");

        builder.HasIndex(message => new { message.ProcessedOnUtc, message.NextAttemptAtUtc, message.LockExpiresAtUtc, message.OccurredOnUtc })
            .HasDatabaseName("IX_outbox_messages_pending");
    }
}
