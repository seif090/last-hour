using Microsoft.Extensions.Options;

namespace LastHour.Modules.Identity.Application;

/// <summary>
/// Validates <see cref="PasswordPolicy"/> so an invalid policy fails fast at startup rather
/// than silently enforcing contradictory rules.
/// </summary>
public sealed class PasswordPolicyValidator : IValidateOptions<PasswordPolicy>
{
    /// <inheritdoc/>
    public ValidateOptionsResult Validate(string? name, PasswordPolicy options)
    {
        if (options.MinimumLength < 8)
        {
            return ValidateOptionsResult.Fail(
                $"'{PasswordPolicy.SectionName}:MinimumLength' must be at least 8 characters.");
        }

        if (options.MaximumLength > 1024)
        {
            return ValidateOptionsResult.Fail(
                $"'{PasswordPolicy.SectionName}:MaximumLength' cannot exceed 1024 characters.");
        }

        if (options.MinimumLength > options.MaximumLength)
        {
            return ValidateOptionsResult.Fail(
                $"'{PasswordPolicy.SectionName}:MinimumLength' cannot exceed " +
                $"'{PasswordPolicy.SectionName}:MaximumLength'.");
        }

        return ValidateOptionsResult.Success;
    }
}
