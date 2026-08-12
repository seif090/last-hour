namespace LastHour.Modules.Identity.Application.RefreshTokens;

/// <summary>
/// A freshly generated refresh-token secret and its stored representation. The raw token is
/// handed to the client exactly once and is never persisted or logged; the hash is what the
/// database stores and what every subsequent lookup compares against.
/// </summary>
public sealed record RefreshTokenSecret
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RefreshTokenSecret"/> class.
    /// </summary>
    /// <param name="rawToken">The raw secret delivered to the client.</param>
    /// <param name="tokenHash">The hash of the raw secret, stored for verification.</param>
    public RefreshTokenSecret(string rawToken, string tokenHash)
    {
        RawToken = rawToken;
        TokenHash = tokenHash;
    }

    /// <summary>
    /// Gets the raw secret. Treat as a credential: hand to the client once, never log or persist.
    /// </summary>
    public string RawToken { get; }

    /// <summary>
    /// Gets the hash of the raw secret, used for storage and lookups.
    /// </summary>
    public string TokenHash { get; }
}
