using LastHour.BuildingBlocks.Application.Contracts;
using LastHour.Modules.Identity.Application.Permissions;
using LastHour.Modules.Identity.Domain.Roles;

namespace LastHour.Modules.Identity.Application;

/// <summary>
/// Seeds the authorization bootstrap through the module's own abstractions: permissions are
/// upserted by stable code, the bootstrap role by normalized name, and the grants are applied
/// through the role aggregate so every domain invariant holds. A re-run can never duplicate
/// rows — the upsert keys and the aggregate's set semantics guarantee idempotence — and the
/// whole bootstrap commits in one unit-of-work save so it is atomic. The service creates no
/// user accounts and no credentials; assigning the role is a future protected flow.
/// </summary>
public sealed class AuthorizationBootstrapService : IAuthorizationBootstrapService
{
    private readonly IRepository<Permission> _permissionWrites;
    private readonly IReadRepository<Permission, PermissionId> _permissionReads;
    private readonly IRoleRepository _roles;
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>
    /// Initializes a new instance of the <see cref="AuthorizationBootstrapService"/> class.
    /// </summary>
    /// <param name="permissionWrites">The write repository used to register missing permissions.</param>
    /// <param name="permissionReads">The read repository used to find existing permissions.</param>
    /// <param name="roles">The role repository used to upsert the bootstrap role and its grants.</param>
    /// <param name="unitOfWork">The unit of work that commits the bootstrap atomically.</param>
    /// <exception cref="ArgumentNullException">Any argument is <see langword="null"/>.</exception>
    public AuthorizationBootstrapService(
        IRepository<Permission> permissionWrites,
        IReadRepository<Permission, PermissionId> permissionReads,
        IRoleRepository roles,
        IUnitOfWork unitOfWork)
    {
        ArgumentNullException.ThrowIfNull(permissionWrites);
        ArgumentNullException.ThrowIfNull(permissionReads);
        ArgumentNullException.ThrowIfNull(roles);
        ArgumentNullException.ThrowIfNull(unitOfWork);
        _permissionWrites = permissionWrites;
        _permissionReads = permissionReads;
        _roles = roles;
        _unitOfWork = unitOfWork;
    }

    /// <inheritdoc/>
    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        // Permissions: upsert by stable code. The ids are generated in memory, so existing
        // grants can be linked against them before the single commit below.
        IReadOnlyList<Permission> existingPermissions = await _permissionReads
            .ListAsync(cancellationToken)
            .ConfigureAwait(false);
        Dictionary<string, Permission> permissionsByCode = existingPermissions.ToDictionary(
            permission => permission.Code,
            StringComparer.Ordinal);

        foreach (PermissionDefinition definition in PermissionCatalog.All)
        {
            if (!permissionsByCode.ContainsKey(definition.Code))
            {
                Permission permission = Permission.Create(definition.Code, definition.Description);
                permissionsByCode[definition.Code] = permission;
                _permissionWrites.Add(permission);
            }
        }

        // Role: upsert by normalized name; the aggregate owns the name invariant and grants.
        Role? administrator = await _roles
            .GetByNormalizedNameAsync(RoleName.Normalize(BootstrapRoles.Administrator), cancellationToken)
            .ConfigureAwait(false);

        if (administrator is null)
        {
            administrator = Role.Create(
                BootstrapRoles.Administrator,
                "The bootstrap administrator role holding every catalog permission.");
            _roles.Add(administrator);
        }
        else
        {
            administrator = await _roles
                .GetByIdTrackedAsync(administrator.Id, cancellationToken)
                .ConfigureAwait(false)
                ?? administrator;
        }

        // Grants: link the role to every catalog permission it does not already hold.
        IReadOnlyList<Permission> granted = await _roles
            .GetPermissionsForRoleAsync(administrator.Id, cancellationToken)
            .ConfigureAwait(false);
        HashSet<string> grantedCodes = granted.Select(permission => permission.Code).ToHashSet(StringComparer.Ordinal);

        foreach (Permission permission in permissionsByCode.Values)
        {
            if (!grantedCodes.Contains(permission.Code))
            {
                administrator.AssignPermission(permission.Id);
            }
        }

        // One atomic commit for the whole bootstrap.
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
