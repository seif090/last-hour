using LastHour.BuildingBlocks.Application.Contracts;
using LastHour.Modules.Identity.Domain.Users;

namespace LastHour.Modules.Identity.Domain.Roles;

/// <summary>
/// The persistence contract of the <see cref="Role"/> aggregate and the authorization
/// resolution surface. It extends the shared write repository with role lookups (by id and by
/// normalized name) and the resolution queries that map a user to their roles and effective
/// permissions. Implementations must resolve roles by the normalized name only, so lookups
/// never depend on the case or whitespace of the caller's input, and the permission queries
/// must execute as single projections — never N+1 loops — because they sit on the future
/// authorization-check path.
/// </summary>
public interface IRoleRepository : IRepository<Role>
{
    /// <summary>
    /// Retrieves the role with the specified identifier, or <see langword="null"/> when no
    /// such role exists.
    /// </summary>
    /// <param name="id">The role identifier to look up.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>The matching role, or <see langword="null"/> when not found.</returns>
    Task<Role?> GetByIdAsync(RoleId id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves the role with the specified normalized name, or <see langword="null"/> when
    /// no such role exists.
    /// </summary>
    /// <param name="normalizedName">The normalized role name to look up (see
    /// <see cref="RoleName.Normalize"/>).</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>The matching role, or <see langword="null"/> when not found.</returns>
    Task<Role?> GetByNormalizedNameAsync(string normalizedName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Determines whether a role with the specified identifier exists.
    /// </summary>
    /// <param name="id">The role identifier to check.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns><see langword="true"/> when the role exists; otherwise <see langword="false"/>.</returns>
    Task<bool> ExistsByIdAsync(RoleId id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Determines whether a role with the specified normalized name already exists.
    /// </summary>
    /// <param name="normalizedName">The normalized role name to check (see
    /// <see cref="RoleName.Normalize"/>).</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns><see langword="true"/> when a role with the normalized name exists; otherwise <see langword="false"/>.</returns>
    Task<bool> ExistsByNormalizedNameAsync(string normalizedName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves the role with the specified identifier and places the aggregate in the change
    /// tracker, or <see langword="null"/> when no such role exists. Used by flows that mutate
    /// the role (permission grants) and must rely on the optimistic-concurrency token to make
    /// concurrent writes safe.
    /// </summary>
    /// <param name="id">The role identifier to look up.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>The matching, tracked role, or <see langword="null"/> when not found.</returns>
    Task<Role?> GetByIdTrackedAsync(RoleId id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Refreshes the tracked state of a role from the store, discarding any pending changes.
    /// </summary>
    /// <param name="role">The tracked role to refresh.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task ReloadTrackedAsync(Role role, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves every role assigned to the specified user.
    /// </summary>
    /// <param name="userId">The user whose roles are queried.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>The roles assigned to the user.</returns>
    Task<IReadOnlyList<Role>> GetRolesForUserAsync(UserId userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves every permission granted to the specified role.
    /// </summary>
    /// <param name="roleId">The role whose permissions are queried.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>The permissions granted to the role.</returns>
    Task<IReadOnlyList<Permission>> GetPermissionsForRoleAsync(RoleId roleId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves the effective permissions of a user: every permission granted to any role the
    /// user holds. This is the authorization resolution foundation that future policy checks
    /// will build on. The implementation must resolve the whole set in a single projection so
    /// the check path never degrades into N+1 queries.
    /// </summary>
    /// <param name="userId">The user whose effective permissions are queried.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>The effective permission set of the user.</returns>
    Task<IReadOnlyList<Permission>> GetPermissionsForUserAsync(UserId userId, CancellationToken cancellationToken = default);
}
