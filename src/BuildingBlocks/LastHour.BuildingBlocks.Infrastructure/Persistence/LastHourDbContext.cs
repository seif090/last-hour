using System.Reflection;
using LastHour.BuildingBlocks.Infrastructure.Persistence.Configuration;
using LastHour.BuildingBlocks.Infrastructure.Persistence.Idempotency;
using LastHour.BuildingBlocks.Infrastructure.Persistence.Outbox;
using LastHour.BuildingBlocks.Infrastructure.Persistence.Seeding;
using Microsoft.EntityFrameworkCore;

namespace LastHour.BuildingBlocks.Infrastructure.Persistence;

/// <summary>
/// The shared database context of the LastHour backend. It owns the outbox and consumer inbox tables
/// and applies cross-cutting concerns for every entity mapped into the model: automatic value conversion
/// for strongly typed identifiers, soft-delete query filters, and (via the registered
/// interceptors) audit stamps, soft deletion and outbox capture. Modules contribute entity
/// configurations by registering their assemblies with the persistence service collection
/// extension; nothing in the model is hard-coded here beyond the outbox and consumer inbox.
/// </summary>
public class LastHourDbContext : DbContext
{
    private readonly IReadOnlyCollection<Assembly> _configurationAssemblies;

    /// <summary>
    /// Initializes a new instance of the <see cref="LastHourDbContext"/> class.
    /// </summary>
    /// <param name="options">The database context options, including the configured provider.</param>
    /// <param name="configurationAssemblies">Assemblies whose entity configurations are applied to the model.</param>
    public LastHourDbContext(DbContextOptions<LastHourDbContext> options, IEnumerable<Assembly>? configurationAssemblies = null)
        : base(options)
    {
        _configurationAssemblies = configurationAssemblies?.Distinct().ToArray() ?? Array.Empty<Assembly>();
    }

    /// <summary>
    /// Gets the outbox messages persisted by the domain events interceptor and the outbox publisher.
    /// </summary>
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    /// <summary>
    /// Gets the consumer inbox messages tracking consumer-scoped event idempotency.
    /// </summary>
    public DbSet<ConsumerInboxMessage> ConsumerInboxMessages => Set<ConsumerInboxMessage>();

    /// <inheritdoc/>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfiguration(new OutboxMessageConfiguration());
        modelBuilder.ApplyConfiguration(new SeedHistoryConfiguration());
        modelBuilder.ApplyConfiguration(new ConsumerInboxMessageConfiguration());

        foreach (Assembly assembly in _configurationAssemblies)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(assembly);
        }

        modelBuilder.ApplyStronglyTypedIdConverters();
        modelBuilder.ApplySoftDeleteQueryFilters();
    }
}
