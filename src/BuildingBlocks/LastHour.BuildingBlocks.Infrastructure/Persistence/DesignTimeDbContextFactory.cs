using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace LastHour.BuildingBlocks.Infrastructure.Persistence;

/// <summary>
/// Creates <see cref="LastHourDbContext"/> instances for the EF Core design-time tooling
/// (<c>dotnet ef migrations</c>). This factory is never used at runtime; it exists solely so
/// migrations can be generated against PostgreSQL without a configured application host. The
/// connection string is a development placeholder that is only relevant while generating
/// migrations, which does not require a reachable database.
/// </summary>
public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<LastHourDbContext>
{
    /// <inheritdoc/>
    public LastHourDbContext CreateDbContext(string[] args)
    {
        DbContextOptions<LastHourDbContext> options = new DbContextOptionsBuilder<LastHourDbContext>()
            .UseNpgsql("Host=localhost;Database=last_hour;Username=last_hour;Password=last_hour")
            .Options;

        return new LastHourDbContext(options, DiscoverModuleAssemblies());
    }

    // Module entity configurations live in their own assemblies, which the infrastructure
    // layer must not reference. At design time the tooling runs from the startup project's
    // output (the API host, which references every module), so the module assemblies are
    // discoverable next to the executing host. Run `dotnet dotnet-ef` with
    // `--startup-project src/Api/LastHour.Api` so module mappings participate in the model
    // and migrations include module tables.
    private static Assembly[] DiscoverModuleAssemblies()
    {
        string baseDirectory = AppContext.BaseDirectory;

        return Directory.Exists(baseDirectory)
            ? Directory.GetFiles(baseDirectory, "LastHour.Modules.*.dll")
                .Select(Assembly.LoadFrom)
                .ToArray()
            : Array.Empty<Assembly>();
    }
}
