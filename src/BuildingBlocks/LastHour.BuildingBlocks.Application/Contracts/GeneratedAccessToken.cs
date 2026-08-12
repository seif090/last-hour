namespace LastHour.BuildingBlocks.Application.Contracts;

/// <summary>
/// The result of issuing an access token: the encoded token and the moment it expires. The token
/// string is a credential and must never be logged, persisted or included in problem details;
/// the expiration is what callers use to compute the public <c>expiresIn</c> value.
/// </summary>
public sealed record GeneratedAccessToken
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GeneratedAccessToken"/> class.
    /// </summary>
    /// <param name="token">The encoded access token.</param>
    /// <param name="expiresAtUtc">The moment the token expires, in UTC.</param>
    public GeneratedAccessToken(string token, DateTimeOffset expiresAtUtc)
    {
        Token = token;
        ExpiresAtUtc = expiresAtUtc;
    }

    /// <summary>
    /// Gets the encoded access token. Treat as a credential: never log or persist it.
    /// </summary>
    public string Token { get; }

    /// <summary>
    /// Gets the moment the token expires, in UTC.
    /// </summary>
    public DateTimeOffset ExpiresAtUtc { get; }
}
