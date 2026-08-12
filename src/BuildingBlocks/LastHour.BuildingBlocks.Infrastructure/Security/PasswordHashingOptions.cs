namespace LastHour.BuildingBlocks.Infrastructure.Security;

/// <summary>
/// Configuration for Argon2id password hashing. Bound from the <c>PasswordHashing</c>
/// configuration section. Parameters are production server-side defaults documented in
/// docs/authentication.md; raising them increases hashing cost and future logins transparently
/// re-hash stored credentials via the rehash strategy.
/// </summary>
public sealed class PasswordHashingOptions
{
    /// <summary>
    /// Gets the name of the configuration section this type binds to.
    /// </summary>
    public const string SectionName = "PasswordHashing";

    /// <summary>
    /// Gets or sets the number of passes over the memory (Argon2 <c>t</c> parameter).
    /// </summary>
    public int TimeCost { get; set; } = 3;

    /// <summary>
    /// Gets or sets the memory usage in kibibytes (Argon2 <c>m</c> parameter). The default of
    /// 65536 KiB equals 64 MiB, the commonly recommended server-side Argon2id budget.
    /// </summary>
    public int MemoryCost { get; set; } = 65536;

    /// <summary>
    /// Gets or sets the number of parallel lanes (Argon2 <c>p</c> parameter).
    /// </summary>
    public int Lanes { get; set; } = 4;

    /// <summary>
    /// Gets or sets the number of threads used when hashing and verifying.
    /// </summary>
    public int Threads { get; set; } = 4;

    /// <summary>
    /// Gets or sets the length of the derived key in bytes.
    /// </summary>
    public int HashLength { get; set; } = 32;

    /// <summary>
    /// Gets or sets the length of the random salt in bytes. Generated per hash with a
    /// cryptographically secure random number generator.
    /// </summary>
    public int SaltLength { get; set; } = 16;
}
