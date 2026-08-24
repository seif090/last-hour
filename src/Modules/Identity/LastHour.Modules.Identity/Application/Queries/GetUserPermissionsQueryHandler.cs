using LastHour.BuildingBlocks.Application.Cqrs;
using LastHour.BuildingBlocks.SharedKernel.Results;
using LastHour.Modules.Identity.Domain.Roles;
using LastHour.Modules.Identity.Domain.Users;

namespace LastHour.Modules.Identity.Application.Queries;

/// <summary>
/// Resolves the effective permission set of a user and projects it onto safe
/// <see cref="PermissionInfo"/> values. An unknown user yields a not-found result; a known
/// user with no roles yields an empty set. The repository resolves the whole set in a single
/// query, so the future authorization-check path never degrades into N+1 lookups.
/// </summary>
public sealed class GetUserPermissionsQueryHandler : IQueryHandler<GetUserPermissionsQuery, IReadOnlyList<PermissionInfo>>
{
    private readonly IUserRepository _users;
    private readonly IRoleRepository _roles;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetUserPermissionsQueryHandler"/> class.
    /// </summary>
    /// <param name="users">The user repository used to validate the user exists.</param>
    /// <param name="roles">The role repository used to resolve the user's effective permissions.</param>
    /// <exception cref="ArgumentNullException">Any argument is <see langword="null"/>.</exception>
    public GetUserPermissionsQueryHandler(
        IUserRepository users,
        IRoleRepository roles)
    {
        ArgumentNullException.ThrowIfNull(users);
        ArgumentNullException.ThrowIfNull(roles);
        _users = users;
        _roles = roles;
    }

    /// <inheritdoc/>
    public async Task<Result<IReadOnlyList<PermissionInfo>>> Handle(
        GetUserPermissionsQuery query,
        CancellationToken cancellationToken)
    {
        UserId userId = UserId.Create(query.UserId);

        if (await _users.GetByIdAsync(userId, cancellationToken).ConfigureAwait(false) is null)
        {
            return Result<IReadOnlyList<PermissionInfo>>.Failure(Error.NotFound(
                "Users.NotFound",
                "The user could not be found."));
        }

        IReadOnlyList<Permission> permissions = await _roles
            .GetPermissionsForUserAsync(userId, cancellationToken)
            .ConfigureAwait(false);

        return Result<IReadOnlyList<PermissionInfo>>.Success(
            permissions.Select(permission => new PermissionInfo(
                permission.Id.Value,
                permission.Code,
                permission.Description)).ToArray());
    }
}
