using LastHour.BuildingBlocks.Application.Cqrs;

namespace LastHour.Modules.Identity.Application.Queries;

/// <summary>
/// Resolves the roles assigned to a user. The user identifier is injected by a protected flow
/// (in the future, from validated token claims or a management context) — never from public
/// input.
/// </summary>
public sealed record GetUserRolesQuery : IQuery<IReadOnlyList<RoleInfo>>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GetUserRolesQuery"/> class.
    /// </summary>
    /// <param name="userId">The user whose roles are resolved.</param>
    public GetUserRolesQuery(Guid userId)
    {
        UserId = userId;
    }

    /// <summary>
    /// Gets the user whose roles are resolved.
    /// </summary>
    public Guid UserId { get; }
}
