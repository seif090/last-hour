using LastHour.BuildingBlocks.Application.Contracts;
using LastHour.Modules.Identity.Application;
using LastHour.Modules.Identity.Domain.Roles;
using LastHour.Modules.Identity.Domain.Users;
using Microsoft.Extensions.DependencyInjection;

namespace LastHour.Api.Tests.Support;

/// <summary>
/// Seeds the authorization reference data and role assignments directly into the test database
/// through the module's own abstractions. The authorization bootstrap is the same idempotent
/// service the API composition root runs (it creates the catalog permissions, the
/// <c>Administrator</c> role and the grants); assignments go through the user aggregate's
/// <see cref="User.AssignRole"/> so every domain invariant holds. Tests that exercise the
/// role-management endpoints use this to establish the admin surface before the first HTTP
/// call, mirroring how a future protected provisioning flow would bootstrap it.
/// </summary>
public static class RoleSeeder
{
    /// <summary>
    /// Seeds the authorization bootstrap (catalog permissions, the administrator role and the
    /// grants) into the test database. Idempotent: safe to call repeatedly.
    /// </summary>
    /// <param name="factory">The API factory whose database is seeded.</param>
    /// <returns>A task that completes when seeding has finished.</returns>
    public static async Task SeedAuthorizationAsync(SqliteApiFactory factory)
    {
        using IServiceScope scope = factory.Services.CreateScope();
        IAuthorizationBootstrapService bootstrap = scope.ServiceProvider
            .GetRequiredService<IAuthorizationBootstrapService>();

        await bootstrap.SeedAsync();
    }

    /// <summary>
    /// Assigns the role with the given name to the given user.
    /// </summary>
    /// <param name="factory">The API factory whose database is seeded.</param>
    /// <param name="userId">The user receiving the role.</param>
    /// <param name="roleName">The role name (matched case-insensitively through the domain
    /// normalization rule).</param>
    /// <returns>A task that completes when the assignment has been saved.</returns>
    public static async Task AssignRoleAsync(SqliteApiFactory factory, Guid userId, string roleName)
    {
        using IServiceScope scope = factory.Services.CreateScope();
        IUserRepository users = scope.ServiceProvider.GetRequiredService<IUserRepository>();
        IRoleRepository roles = scope.ServiceProvider.GetRequiredService<IRoleRepository>();
        IUnitOfWork unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        User? user = await users.GetByIdTrackedAsync(UserId.Create(userId));
        Role? role = await roles.GetByNormalizedNameAsync(RoleName.Normalize(roleName));

        user!.AssignRole(role!.Id);
        await unitOfWork.SaveChangesAsync();
    }

    /// <summary>
    /// Creates a custom role with specific permissions and saves it to the database.
    /// </summary>
    /// <param name="factory">The API factory whose database is seeded.</param>
    /// <param name="roleName">The unique role name.</param>
    /// <param name="permissionCodes">The list of permission codes to grant to this role.</param>
    /// <returns>The generated role identifier.</returns>
    public static async Task<Guid> CreateRoleWithPermissionsAsync(
        SqliteApiFactory factory,
        string roleName,
        params string[] permissionCodes)
    {
        using IServiceScope scope = factory.Services.CreateScope();
        IRoleRepository roles = scope.ServiceProvider.GetRequiredService<IRoleRepository>();
        IReadRepository<Permission, PermissionId> permissions = scope.ServiceProvider
            .GetRequiredService<IReadRepository<Permission, PermissionId>>();
        IUnitOfWork unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        IReadOnlyList<Permission> allPermissions = await permissions.ListAsync();
        Dictionary<string, Permission> byCode = allPermissions.ToDictionary(p => p.Code, StringComparer.Ordinal);

        Role role = Role.Create(roleName, $"Custom test role for {roleName}");
        foreach (string code in permissionCodes)
        {
            if (byCode.TryGetValue(code, out Permission? permission))
            {
                role.AssignPermission(permission.Id);
            }
        }

        roles.Add(role);
        await unitOfWork.SaveChangesAsync();
        return role.Id.Value;
    }
}
