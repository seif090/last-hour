using LastHour.BuildingBlocks.Infrastructure.Persistence.Seeding;
using LastHour.Modules.Identity.Application;

namespace LastHour.Api.Persistence;

/// <summary>
/// The composition-root adapter that exposes the Identity module's authorization bootstrap to
/// the infrastructure seeding pipeline. All logic lives in
/// <see cref="IAuthorizationBootstrapService"/> (which stays free of infrastructure
/// references); this type only adapts it to the <see cref="IDatabaseSeeder"/> contract that
/// the <see cref="SeederExecutor"/> discovers and runs exactly once per database. It creates
/// no accounts and no credentials.
/// </summary>
public sealed class RolesAndPermissionsSeeder : IDatabaseSeeder
{
    private readonly IAuthorizationBootstrapService _bootstrap;

    /// <summary>
    /// Initializes a new instance of the <see cref="RolesAndPermissionsSeeder"/> class.
    /// </summary>
    /// <param name="bootstrap">The module's authorization bootstrap service.</param>
    /// <exception cref="ArgumentNullException"><paramref name="bootstrap"/> is <see langword="null"/>.</exception>
    public RolesAndPermissionsSeeder(IAuthorizationBootstrapService bootstrap)
    {
        ArgumentNullException.ThrowIfNull(bootstrap);
        _bootstrap = bootstrap;
    }

    /// <inheritdoc/>
    public Task SeedAsync(CancellationToken cancellationToken = default)
        => _bootstrap.SeedAsync(cancellationToken);
}
