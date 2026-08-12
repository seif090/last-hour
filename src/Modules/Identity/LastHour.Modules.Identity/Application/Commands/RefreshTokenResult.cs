namespace LastHour.Modules.Identity.Application.Commands;

/// <summary>
/// The outcome of a successful refresh: the rotated access token with its expiration and the new
/// raw refresh token. Both tokens are credentials and must never be logged or persisted; the API
/// maps the expiration to the public <c>expiresIn</c> value and delivers the refresh token to
/// the client once.
/// </summary>
public sealed record RefreshTokenResult
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RefreshTokenResult"/> class.
    /// </summary>
    /// <param name="accessToken">The newly issued access token.</param>
    /// <param name="expiresAtUtc">The moment the access token expires, in UTC.</param>
    /// <param name="refreshToken">The new raw refresh token, delivered to the client exactly once.</param>
    public RefreshTokenResult(string accessToken, DateTimeOffset expiresAtUtc, string refreshToken)
    {
        AccessToken = accessToken;
        ExpiresAtUtc = expiresAtUtc;
        RefreshToken = refreshToken;
    }

    /// <summary>
    /// Gets the newly issued access token.
    /// </summary>
    public string AccessToken { get; }

    /// <summary>
    /// Gets the moment the access token expires, in UTC.
    /// </summary>
    public DateTimeOffset ExpiresAtUtc { get; }

    /// <summary>
    /// Gets the new raw refresh token.
    /// </summary>
    public string RefreshToken { get; }
}
