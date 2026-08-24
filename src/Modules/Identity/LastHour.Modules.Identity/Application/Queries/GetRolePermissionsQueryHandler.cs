using LastHour.BuildingBlocks.Application.Cqrs;
using LastHour.BuildingBlocks.SharedKernel.Results;
using LastHour.Modules.Identity.Domain.Roles;

namespace LastHour.Modules.Identity.Application.Queries;

/// <summary>
/// Resolves the permissions granted to a role and projects them onto safe
/// <see cref="PermissionInfo"/> values. An unknown role yields a not-found result; a known
/// role without grants yields an empty list.
/// </summary>
public sealed class GetRolePermissionsQueryHandler : IQueryHandler<GetRolePermissionsQuery, IReadOnlyList<PermissionInfo>>
{
    private readonly IRoleRepository _roles;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetRolePermissionsQueryHandler"/> class.
    /// </summary>
    /// <param name="roles">The role repository used to validate the role and resolve its permissions.</param>
    /// <exception cref="ArgumentNullException"><paramref name="roles"/> is <see langword="null"/>.</exception>
    public GetRolePermissionsQueryHandler(IRoleRepository roles)
    {
        ArgumentNullException.ThrowIfNull(roles);
        _roles = roles;
    }

    /// <inheritdoc/>
    public async Task<Result<IReadOnlyList<PermissionInfo>>> Handle(
        GetRolePermissionsQuery query,
        CancellationToken cancellationToken)
    {
        RoleId roleId = RoleId.Create(query.RoleId);

        if (!await _roles.ExistsByIdAsync(roleId, cancellationToken).ConfigureAwait(false))
        {
            return Result<IReadOnlyList<PermissionInfo>>.Failure(Error.NotFound(
                "Roles.NotFound",
                "The role could not be found."));
        }

        IReadOnlyList<Permission> permissions = await _roles
            .GetPermissionsForRoleAsync(roleId, cancellationToken)
            .ConfigureAwait(false);

        return Result<IReadOnlyList<PermissionInfo>>.Success(
            permissions.Select(permission => new PermissionInfo(
                permission.Id.Value,
                permission.Code,
                permission.Description)).ToArray());
    }
}
