namespace LastHour.Modules.Identity.Domain.RefreshTokens;

/// <summary>
/// The auditable reason a <see cref="RefreshToken"/> was revoked. Revocation is permanent —
/// a revoked token can never be re-activated — and the reason is retained so security
/// operations can distinguish normal lifecycle (rotation, logout) from suspicious activity
/// (reuse). Expiration is not a revocation reason: an expired token simply carries an
/// <c>ExpiresAtUtc</c> in the past and needs no revocation row.
/// </summary>
public enum TokenRevocationReason
{
    /// <summary>
    /// The token was consumed by a successful refresh and replaced by its successor.
    /// </summary>
    Rotation = 0,

    /// <summary>
    /// The session was explicitly ended by the user (logout).
    /// </summary>
    Logout = 1,

    /// <summary>
    /// A rotated token was presented again, indicating the secret may have been compromised;
    /// the whole token family is revoked as a consequence.
    /// </summary>
    ReuseDetected = 2,

    /// <summary>
    /// The session was terminated because the account can no longer authenticate (for example
    /// the account was disabled or suspended after the token was issued).
    /// </summary>
    SecurityEvent = 3,

    /// <summary>
    /// The account password was changed, which revokes every outstanding session.
    /// </summary>
    PasswordChanged = 4,

    /// <summary>
    /// The user requested that every session of the account be ended at once.
    /// </summary>
    AllSessionsRevoked = 5,
}
