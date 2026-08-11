namespace LastHour.Modules.Identity.Domain.Users;

/// <summary>
/// The lifecycle status of a <see cref="User"/> account. This models permanent or
/// semi-permanent account state only; temporary, time-boxed lockout (failed sign-in
/// throttling) is authentication behavior and deliberately not part of this model — it
/// belongs to a future authentication sprint.
/// </summary>
public enum AccountStatus
{
    /// <summary>
    /// The account was created but has not been activated (for example, the address has not
    /// been verified yet). A pending account cannot authenticate.
    /// </summary>
    Pending = 0,

    /// <summary>
    /// The account is active and can authenticate.
    /// </summary>
    Active = 1,

    /// <summary>
    /// The account is temporarily restricted (for example, a policy review). A suspended
    /// account cannot authenticate and can return to <see cref="Active"/>.
    /// </summary>
    Suspended = 2,

    /// <summary>
    /// The account is permanently deactivated. A disabled account cannot authenticate and has
    /// no transition out of this state in the current model (administrative re-enablement is a
    /// future decision).
    /// </summary>
    Disabled = 3,
}
