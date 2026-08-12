using LastHour.BuildingBlocks.Application.Cqrs;

namespace LastHour.Modules.Identity.Application.Commands;

/// <summary>
/// Ends a session by revoking its refresh credential. The operation is idempotent: submitting a
/// missing, already-revoked or expired token still succeeds, because logout must never reveal
/// whether a presented token was ever valid. Already-issued access tokens remain valid until
/// they expire — JWT access tokens are stateless.
/// </summary>
public sealed record LogoutCommand : ICommand
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LogoutCommand"/> class.
    /// </summary>
    /// <param name="refreshToken">The raw refresh token of the session to end.</param>
    public LogoutCommand(string refreshToken)
    {
        RefreshToken = refreshToken;
    }

    /// <summary>
    /// Gets the raw refresh token of the session to end.
    /// </summary>
    public string RefreshToken { get; }
}
