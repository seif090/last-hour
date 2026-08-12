using Microsoft.Extensions.Options;

namespace LastHour.Modules.Identity.Application;

/// <summary>
/// Validates <see cref="AccountSecurityOptions"/> so an invalid throttle policy fails fast at
/// startup rather than silently locking accounts on every misconfiguration.
/// </summary>
public sealed class AccountSecurityOptionsValidator : IValidateOptions<AccountSecurityOptions>
{
    /// <inheritdoc/>
    public ValidateOptionsResult Validate(string? name, AccountSecurityOptions options)
    {
        if (options.MaxFailedAttempts < 1)
        {
            return ValidateOptionsResult.Fail(
                $"'{AccountSecurityOptions.SectionName}:MaxFailedAttempts' must be at least 1.");
        }

        if (options.MaxFailedAttempts > 100)
        {
            return ValidateOptionsResult.Fail(
                $"'{AccountSecurityOptions.SectionName}:MaxFailedAttempts' cannot exceed 100.");
        }

        if (options.LockoutDuration <= TimeSpan.Zero)
        {
            return ValidateOptionsResult.Fail(
                $"'{AccountSecurityOptions.SectionName}:LockoutDuration' must be positive.");
        }

        if (options.LockoutDuration > TimeSpan.FromDays(30))
        {
            return ValidateOptionsResult.Fail(
                $"'{AccountSecurityOptions.SectionName}:LockoutDuration' cannot exceed 30 days.");
        }

        return ValidateOptionsResult.Success;
    }
}
