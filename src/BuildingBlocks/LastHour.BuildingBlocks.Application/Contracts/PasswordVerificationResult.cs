namespace LastHour.BuildingBlocks.Application.Contracts;

/// <summary>
/// The outcome of verifying a password against a stored credential hash.
/// </summary>
public enum PasswordVerificationResult
{
    /// <summary>
    /// The password does not match the stored hash.
    /// </summary>
    Failed = 0,

    /// <summary>
    /// The password matches the stored hash and the hash is current.
    /// </summary>
    Success,

    /// <summary>
    /// The password matches the stored hash, but the hash was produced with parameters that no
    /// longer meet the current policy. The password was verified successfully, so the caller may
    /// transparently re-hash and store the upgraded credential.
    /// </summary>
    SuccessRehashNeeded,
}
