namespace LastHour.Modules.Identity.Application.RefreshTokens;

/// <summary>
/// Configures the refresh-token lifecycle. Only the lifetime is tunable; the entropy, hashing
/// scheme and rotation rules are fixed security properties of the implementation and are not
/// configurable. No secrets belong in this section.
/// </summary>
public sealed class RefreshTokenOptions
{
    /// <summary>
    /// The configuration section the options are bound from.
    /// </summary>
    public const string SectionName = "RefreshToken";

    /// <summary>
    /// The longest refresh-token lifetime accepted by configuration, as a sanity bound against
    /// accidental multi-month sessions.
    /// </summary>
    public static readonly TimeSpan MaximumLifetime = TimeSpan.FromDays(90);

    /// <summary>
    /// Gets or sets the refresh-token lifetime. Defaults to 14 days, long enough to outlive
    /// many short access tokens but short enough to bound the exposure of a stolen secret.
    /// </summary>
    public TimeSpan Lifetime { get; set; } = TimeSpan.FromDays(14);
}
