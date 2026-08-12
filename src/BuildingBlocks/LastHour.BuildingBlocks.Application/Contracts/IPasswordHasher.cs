namespace LastHour.BuildingBlocks.Application.Contracts;

/// <summary>
/// Hashes and verifies passwords using a password-specific, memory-hard algorithm. This is the
/// contract consumed by application services; the concrete provider is an infrastructure
/// concern. Implementations must never store, return or log plaintext passwords or hashes in a
/// form that leaks credential material, and must verify passwords in a timing-safe manner for
/// wrong-password attempts.
/// </summary>
public interface IPasswordHasher
{
    /// <summary>
    /// Hashes a password using the configured algorithm and parameters. Each call produces a
    /// new salt, so hashing the same password twice yields different results.
    /// </summary>
    /// <param name="password">The password to hash. It is never stored or exposed; only the
    /// resulting hash string is returned.</param>
    /// <returns>The encoded hash, which embeds the algorithm, parameters, salt and derived key.</returns>
    string Hash(string password);

    /// <summary>
    /// Verifies a password against a stored hash.
    /// </summary>
    /// <param name="password">The password to verify.</param>
    /// <param name="passwordHash">The stored encoded hash to verify against.</param>
    /// <returns><see cref="PasswordVerificationResult.Success"/> when the password matches a
    /// current hash, <see cref="PasswordVerificationResult.SuccessRehashNeeded"/> when it matches
    /// but the hash is stale, and <see cref="PasswordVerificationResult.Failed"/> when the
    /// password is wrong or the stored hash is malformed or unsupported.</returns>
    PasswordVerificationResult Verify(string password, string passwordHash);

    /// <summary>
    /// Determines whether a stored hash should be upgraded to the current algorithm and
    /// parameters. Used after a successful verification so credentials can be transparently
    /// re-hashed without user involvement.
    /// </summary>
    /// <param name="passwordHash">The stored encoded hash to inspect.</param>
    /// <returns><see langword="true"/> when the hash was produced with an older algorithm or
    /// weaker parameters than currently configured; <see langword="false"/> otherwise.</returns>
    bool NeedsRehash(string passwordHash);
}
