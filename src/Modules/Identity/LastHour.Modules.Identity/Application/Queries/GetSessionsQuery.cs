using LastHour.BuildingBlocks.Application.Cqrs;

namespace LastHour.Modules.Identity.Application.Queries;

/// <summary>
/// Lists the sessions (refresh-token records) of the authenticated user for the session-
/// inspection endpoint. The user identifier is injected by the API layer from the validated
/// access-token claims — never accepted from the request — so a caller can only ever inspect
/// their own sessions. Only safe metadata is returned; stored hashes and internal state never
/// leave the query.
/// </summary>
public sealed record GetSessionsQuery : IQuery<IReadOnlyList<SessionInfo>>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GetSessionsQuery"/> class.
    /// </summary>
    /// <param name="userId">The authenticated user's identifier, resolved from the access-token claims.</param>
    public GetSessionsQuery(Guid userId)
    {
        UserId = userId;
    }

    /// <summary>
    /// Gets the authenticated user's identifier.
    /// </summary>
    public Guid UserId { get; }
}
