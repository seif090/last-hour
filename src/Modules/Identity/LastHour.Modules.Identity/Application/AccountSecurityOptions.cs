namespace LastHour.Modules.Identity.Application;

/// <summary>
/// Configures the account-level authentication throttle: how many consecutive failed attempts
/// trigger a temporary lockout and how long the lockout lasts. The lockout is time-boxed and
/// never changes the account's permanent <see cref="Domain.Users.AccountStatus"/> — a locked
/// account returns to normal after the window elapses or after a successful authentication.
/// No secrets belong in this section.
/// </summary>
public sealed class AccountSecurityOptions
{
    /// <summary>
    /// The configuration section the options are bound from.
    /// </summary>
    public const string SectionName = "AccountSecurity";

    /// <summary>
    /// Gets or sets the number of consecutive failed authentication attempts that triggers a
    /// temporary lockout. Defaults to 5.
    /// </summary>
    public int MaxFailedAttempts { get; set; } = 5;

    /// <summary>
    /// Gets or sets how long an account stays locked after the threshold is reached. Defaults
    /// to 15 minutes — long enough to deter automated guessing, short enough that a legitimate
    /// user's lockout does not last the day.
    /// </summary>
    public TimeSpan LockoutDuration { get; set; } = TimeSpan.FromMinutes(15);
}
