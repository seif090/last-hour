using LastHour.BuildingBlocks.Application.Cqrs;

namespace LastHour.Modules.Identity.Application.Queries;

/// <summary>
/// Resolves the effective permissions of a user: every permission granted to any role the user
/// holds. This is the authorization resolution foundation that future policy checks build on,
/// so the result is the user's live permission set — it reflects role and permission changes
/// immediately and never goes stale the way a token-embedded claim list would.
/// </summary>
public sealed record GetUserPermissionsQuery : IQuery<IReadOnlyList<PermissionInfo>>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GetUserPermissionsQuery"/> class.
    /// </summary>
    /// <param name="userId">The user whose effective permissions are resolved.</param>
    public GetUserPermissionsQuery(Guid userId)
    {
        UserId = userId;
    }

    /// <summary>
    /// Gets the user whose effective permissions are resolved.
    /// </summary>
    public Guid UserId { get; }
}
