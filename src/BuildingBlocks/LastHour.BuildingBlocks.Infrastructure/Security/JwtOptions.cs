namespace LastHour.BuildingBlocks.Infrastructure.Security;

/// <summary>
/// Configures the JWT access-token issuer: the identity values the token claims, the signing
/// credential and the token lifetime. The signing key is deliberately not embedded in this
/// class or any settings file — it is resolved from the secrets surface at runtime, so an
/// empty or missing key fails startup validation instead of silently producing unsigned or
/// weakly signed tokens.
/// </summary>
public sealed class JwtOptions
{
    /// <summary>
    /// The configuration section the options are bound from.
    /// </summary>
    public const string SectionName = "Jwt";

    /// <summary>
    /// The minimum length of a signing key in characters. HS256 requires a key of at least
    /// 256 bits; this bound keeps accidental short keys from passing configuration.
    /// </summary>
    public const int MinimumSigningKeyLength = 32;

    /// <summary>
    /// The longest access-token lifetime accepted by configuration, as a sanity bound against
    /// accidental multi-day tokens. The default lifetime remains the short-lived 15 minutes.
    /// </summary>
    public static readonly TimeSpan MaximumAccessTokenLifetime = TimeSpan.FromHours(24);

    /// <summary>
    /// The largest clock skew tolerated during validation. Skew exists only to absorb
    /// legitimate clock drift between the issuer and the validator; large values defeat token
    /// expiration and are rejected at startup.
    /// </summary>
    public static readonly TimeSpan MaximumClockSkew = TimeSpan.FromMinutes(15);

    /// <summary>
    /// Gets or sets the issuer the token is issued by and validated against.
    /// </summary>
    public string Issuer { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the audience the token is intended for and validated against.
    /// </summary>
    public string Audience { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the symmetric signing key, resolved from the secrets surface. Never placed
    /// in source control; validated at startup to be non-empty and at least
    /// <see cref="MinimumSigningKeyLength"/> characters.
    /// </summary>
    public string SigningKey { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the access-token lifetime. Short by design; the default is 15 minutes.
    /// </summary>
    public TimeSpan AccessTokenLifetime { get; set; } = TimeSpan.FromMinutes(15);

    /// <summary>
    /// Gets or sets the validation clock skew. Kept small by design; the default is 1 minute.
    /// </summary>
    public TimeSpan ClockSkew { get; set; } = TimeSpan.FromMinutes(1);
}
