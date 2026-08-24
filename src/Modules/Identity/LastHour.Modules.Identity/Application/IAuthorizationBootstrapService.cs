namespace LastHour.Modules.Identity.Application;

/// <summary>
/// Bootstraps the authorization reference data: every permission in the centralized
/// <see cref="Permissions.PermissionCatalog"/> plus a single privileged role holding the full
/// catalog. The operation is idempotent by construction (upserts keyed on the permission code
/// and the normalized role name) and commits atomically through the unit of work. It never
/// creates accounts or credentials — assigning the role to a real user is a protected
/// provisioning flow. The composition root wraps this service in the infrastructure seeder
/// contract so the module itself stays free of infrastructure references.
/// </summary>
public interface IAuthorizationBootstrapService
{
    /// <summary>
    /// Seeds the authorization reference data.
    /// </summary>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>A task that completes when seeding has finished.</returns>
    Task SeedAsync(CancellationToken cancellationToken = default);
}
