using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LastHour.BuildingBlocks.Infrastructure.Persistence.Idempotency;

/// <summary>
/// Configures the relational shape and unique constraints of <see cref="ConsumerInboxMessage"/>.
/// </summary>
public sealed class ConsumerInboxMessageConfiguration : IEntityTypeConfiguration<ConsumerInboxMessage>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<ConsumerInboxMessage> builder)
    {
        builder.ToTable("consumer_inbox_messages");

        builder.HasKey(message => message.Id);

        builder.Property(message => message.EventId)
            .IsRequired();

        builder.Property(message => message.Consumer)
            .HasMaxLength(ConsumerInboxMessage.MaxConsumerLength)
            .IsRequired();

        builder.Property(message => message.StartedOnUtc)
            .IsRequired();

        builder.Property(message => message.CompletedOnUtc);

        builder.Property(message => message.LockExpiresAtUtc)
            .IsConcurrencyToken();

        builder.HasIndex(message => new { message.EventId, message.Consumer })
            .IsUnique()
            .HasDatabaseName("IX_consumer_inbox_messages_event_id_consumer");
    }
}
