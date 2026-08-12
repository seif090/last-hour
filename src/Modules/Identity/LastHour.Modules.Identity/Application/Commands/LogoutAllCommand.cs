using LastHour.BuildingBlocks.Application.Cqrs;

namespace LastHour.Modules.Identity.Application.Commands;

/// <summary>
/// Ends every session of the authenticated user by revoking all of their active refresh tokens.
/// The user identifier is injected by the API layer from the validated access-token claims —
/// never accepted from the request body — so an account can only ever revoke its own sessions.
/// Already-issued access tokens remain valid until they expire, because JWT access tokens are
/// stateless.
/// </summary>
public sealed record LogoutAllCommand : ICommand
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LogoutAllCommand"/> class.
    /// </summary>
    /// <param name="userId">The authenticated user's identifier, resolved from the access-token claims.</param>
    public LogoutAllCommand(Guid userId)
    {
        UserId = userId;
    }

    /// <summary>
    /// Gets the authenticated user's identifier.
    /// </summary>
    public Guid UserId { get; }
}
