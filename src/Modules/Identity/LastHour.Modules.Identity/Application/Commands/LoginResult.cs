namespace LastHour.Modules.Identity.Application.Commands;

/// <summary>
/// The outcome of a successful login: the issued access token with its expiration and the raw
/// refresh token. Both tokens are credentials and must never be logged or persisted; the API
/// maps the expiration to the public <c>expiresIn</c> value and delivers the refresh token to
/// the client once.
/// </summary>
public sealed record LoginResult
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LoginResult"/> class.
    /// </summary>
    /// <param name="accessToken">The issued access token.</param>
    /// <param name="expiresAtUtc">The moment the access token expires, in UTC.</param>
    /// <param name="refreshToken">The raw refresh token, delivered to the client exactly once.</param>
    public LoginResult(string accessToken, DateTimeOffset expiresAtUtc, string refreshToken)
    {
        AccessToken = accessToken;
        ExpiresAtUtc = expiresAtUtc;
        RefreshToken = refreshToken;
    }

    /// <summary>
    /// Gets the issued access token.
    /// </summary>
    public string AccessToken { get; }

    /// <summary>
    /// Gets the moment the access token expires, in UTC.
    /// </summary>
    public DateTimeOffset ExpiresAtUtc { get; }

    /// <summary>
    /// Gets the raw refresh token.
    /// </summary>
    public string RefreshToken { get; }
}
