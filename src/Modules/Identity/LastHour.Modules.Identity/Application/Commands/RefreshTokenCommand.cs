using LastHour.BuildingBlocks.Application.Cqrs;

namespace LastHour.Modules.Identity.Application.Commands;

/// <summary>
/// Rotates a refresh-token credential: validates the submitted token, revokes it, persists its
/// successor and issues a fresh access token. The use case is a command because rotation
/// mutates persisted state — the transaction pipeline commits the revoke-and-insert atomically.
/// Only the raw refresh token is accepted; everything else is server-owned.
/// </summary>
public sealed record RefreshTokenCommand : ICommand<RefreshTokenResult>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RefreshTokenCommand"/> class.
    /// </summary>
    /// <param name="refreshToken">The raw refresh token to validate and rotate.</param>
    public RefreshTokenCommand(string refreshToken)
    {
        RefreshToken = refreshToken;
    }

    /// <summary>
    /// Gets the raw refresh token to validate and rotate.
    /// </summary>
    public string RefreshToken { get; }
}
