namespace LastHour.Modules.Identity.Application;

/// <summary>
/// The password policy enforced when passwords are created or changed. Bound from the
/// <c>PasswordPolicy</c> configuration section. The policy deliberately uses length-based
/// requirements only: composition rules (uppercase, lowercase, digit, symbol) are rejected by
/// modern guidance because they push users toward weaker, predictable passwords. Passwords are
/// verified exactly as entered — they are never trimmed, lower-cased or otherwise normalized.
/// </summary>
public sealed class PasswordPolicy
{
    /// <summary>
    /// Gets the name of the configuration section this type binds to.
    /// </summary>
    public const string SectionName = "PasswordPolicy";

    /// <summary>
    /// Gets or sets the minimum password length. The default follows the OWASP minimum of 12
    /// characters.
    /// </summary>
    public int MinimumLength { get; set; } = 12;

    /// <summary>
    /// Gets or sets the maximum password length. The cap protects against denial-of-service via
    /// extremely long inputs; 128 characters comfortably exceeds every reasonable password while
    /// staying below hashing limits.
    /// </summary>
    public int MaximumLength { get; set; } = 128;
}
