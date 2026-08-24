using Microsoft.Extensions.Options;

namespace LastHour.BuildingBlocks.Infrastructure.Persistence.Configuration;

/// <summary>
/// Validates <see cref="ConsumerIdempotencyOptions"/> so misconfiguration fails fast at startup.
/// </summary>
public sealed class ConsumerIdempotencyOptionsValidator : IValidateOptions<ConsumerIdempotencyOptions>
{
    /// <inheritdoc/>
    public ValidateOptionsResult Validate(string? name, ConsumerIdempotencyOptions options)
    {
        var errors = new List<string>();

        if (options.LeaseDuration < TimeSpan.FromSeconds(1))
        {
            errors.Add($"'{ConsumerIdempotencyOptions.SectionName}:LeaseDuration' must be at least one second.");
        }

        return errors.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(errors);
    }
}
