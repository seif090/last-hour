using LastHour.BuildingBlocks.Application.Cqrs;
using LastHour.BuildingBlocks.SharedKernel.Results;
using LastHour.Modules.Identity.Domain.Roles;
using LastHour.Modules.Identity.Domain.Users;

namespace LastHour.Modules.Identity.Application.Queries;

/// <summary>
/// Resolves the roles of a user and projects them onto safe <see cref="RoleInfo"/> values. An
/// unknown user yields a not-found result; a known user without roles yields an empty list (an
/// account is legitimately role-less until a protected flow assigns one). The repository
/// resolves the set in a single query.
/// </summary>
public sealed class GetUserRolesQueryHandler : IQueryHandler<GetUserRolesQuery, IReadOnlyList<RoleInfo>>
{
    private readonly IUserRepository _users;
    private readonly IRoleRepository _roles;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetUserRolesQueryHandler"/> class.
    /// </summary>
    /// <param name="users">The user repository used to validate the user exists.</param>
    /// <param name="roles">The role repository used to resolve the user's roles.</param>
    /// <exception cref="ArgumentNullException">Any argument is <see langword="null"/>.</exception>
    public GetUserRolesQueryHandler(
        IUserRepository users,
        IRoleRepository roles)
    {
        ArgumentNullException.ThrowIfNull(users);
        ArgumentNullException.ThrowIfNull(roles);
        _users = users;
        _roles = roles;
    }

    /// <inheritdoc/>
    public async Task<Result<IReadOnlyList<RoleInfo>>> Handle(
        GetUserRolesQuery query,
        CancellationToken cancellationToken)
    {
        UserId userId = UserId.Create(query.UserId);

        if (await _users.GetByIdAsync(userId, cancellationToken).ConfigureAwait(false) is null)
        {
            return Result<IReadOnlyList<RoleInfo>>.Failure(Error.NotFound(
                "Users.NotFound",
                "The user could not be found."));
        }

        IReadOnlyList<Role> roles = await _roles
            .GetRolesForUserAsync(userId, cancellationToken)
            .ConfigureAwait(false);

        return Result<IReadOnlyList<RoleInfo>>.Success(
            roles.Select(role => new RoleInfo(role.Id.Value, role.Name.Value)).ToArray());
    }
}
