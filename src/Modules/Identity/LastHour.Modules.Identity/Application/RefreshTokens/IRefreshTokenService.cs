namespace LastHour.Modules.Identity.Application.RefreshTokens;

/// <summary>
/// Generates and hashes refresh-token secrets. The raw secret is always high-entropy and
/// random, so the hashing strategy is deliberately different from password hashing: a fast,
/// deterministic cryptographic hash (SHA-256) is appropriate for secrets that carry hundreds
/// of bits of entropy, whereas slow memory-hard hashing exists to defend low-entropy
/// passwords. The raw secret is produced by a cryptographic RNG and is never stored or logged.
/// </summary>
public interface IRefreshTokenService
{
    /// <summary>
    /// Generates a new refresh-token secret and its storage hash.
    /// </summary>
    /// <returns>The raw secret (client-bound) and its hash (storage-bound).</returns>
    RefreshTokenSecret Generate();

    /// <summary>
    /// Computes the storage hash of a submitted raw token. Deterministic: the same raw token
    /// always hashes identically, which is what makes hash-based lookup and comparison work.
    /// </summary>
    /// <param name="rawToken">The raw token to hash.</param>
    /// <returns>The storage hash.</returns>
    string Hash(string rawToken);
}
