namespace LastHour.Modules.Identity.Application;

/// <summary>
/// The reason an authentication attempt failed. The application layer may distinguish these
/// causes internally; the API layer decides what, if anything, is safe to expose to the
/// unauthenticated client (future login sprint).
/// </summary>
public enum AuthenticationFailureReason
{
    /// <summary>
    /// The credentials do not match: either no account exists for the supplied identifier or
    /// the password is wrong. Both cases surface identically to avoid account enumeration.
    /// </summary>
    InvalidCredentials,

    /// <summary>
    /// The account exists and the password is correct, but the account has not been activated
    /// yet.
    /// </summary>
    AccountPending,

    /// <summary>
    /// The account exists and the password is correct, but the account is suspended.
    /// </summary>
    AccountSuspended,

    /// <summary>
    /// The account exists and the password is correct, but the account is permanently disabled.
    /// </summary>
    AccountDisabled,

    /// <summary>
    /// The account exists but is temporarily locked out after too many failed attempts. The
    /// lockout is time-boxed and separate from the account status lifecycle.
    /// </summary>
    AccountLocked,
}

/// <summary>
/// The outcome of an authentication attempt. It deliberately carries no user data, hashes or
/// tokens; the future login flow maps it to the public response it deems safe.
/// </summary>
public sealed record AuthenticationResult
{
    private AuthenticationResult(bool succeeded, AuthenticationFailureReason? failureReason)
    {
        Succeeded = succeeded;
        FailureReason = failureReason;
    }

    /// <summary>
    /// Gets a value indicating whether authentication succeeded.
    /// </summary>
    public bool Succeeded { get; }

    /// <summary>
    /// Gets the reason for a failed attempt, or <see langword="null"/> on success.
    /// </summary>
    public AuthenticationFailureReason? FailureReason { get; }

    /// <summary>
    /// Creates a successful authentication result.
    /// </summary>
    /// <returns>A successful <see cref="AuthenticationResult"/>.</returns>
    public static AuthenticationResult Success() => new AuthenticationResult(true, null);

    /// <summary>
    /// Creates a failed authentication result with the given reason.
    /// </summary>
    /// <param name="reason">The reason for the failure.</param>
    /// <returns>A failed <see cref="AuthenticationResult"/>.</returns>
    public static AuthenticationResult Failure(AuthenticationFailureReason reason) => new AuthenticationResult(false, reason);
}
