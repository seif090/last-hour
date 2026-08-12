using Microsoft.Extensions.Options;

namespace LastHour.Modules.Identity.Application.RefreshTokens;

/// <summary>
/// Validates the <see cref="RefreshTokenOptions"/> at startup so a misconfigured token
/// lifetime fails fast: the lifetime must be positive and bounded.
/// </summary>
public sealed class RefreshTokenOptionsValidator : IValidateOptions<RefreshTokenOptions>
{
    /// <inheritdoc/>
    public ValidateOptionsResult Validate(string? name, RefreshTokenOptions options)
    {
        var failures = new List<string>();

        if (options.Lifetime <= TimeSpan.Zero)
        {
            failures.Add("RefreshToken:Lifetime must be positive.");
        }
        else if (options.Lifetime > RefreshTokenOptions.MaximumLifetime)
        {
            failures.Add($"RefreshToken:Lifetime cannot exceed {RefreshTokenOptions.MaximumLifetime}.");
        }

        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }
}
