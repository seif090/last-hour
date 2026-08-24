using Microsoft.Extensions.Options;

namespace LastHour.BuildingBlocks.Infrastructure.Persistence.Configuration;

/// <summary>
/// Validates <see cref="OutboxOptions"/> so misconfiguration fails fast at startup.
/// </summary>
public sealed class OutboxOptionsValidator : IValidateOptions<OutboxOptions>
{
    /// <inheritdoc/>
    public ValidateOptionsResult Validate(string? name, OutboxOptions options)
    {
        var errors = new List<string>();

        if (options.ProcessingInterval < TimeSpan.FromSeconds(1))
        {
            errors.Add($"'{OutboxOptions.SectionName}:ProcessingInterval' must be at least one second.");
        }

        if (options.BatchSize <= 0)
        {
            errors.Add($"'{OutboxOptions.SectionName}:BatchSize' must be greater than zero.");
        }

        if (options.MaxRetryCount < 0)
        {
            errors.Add($"'{OutboxOptions.SectionName}:MaxRetryCount' must not be negative.");
        }

        if (options.LeaseDuration < TimeSpan.FromSeconds(1))
        {
            errors.Add($"'{OutboxOptions.SectionName}:LeaseDuration' must be at least one second.");
        }

        if (options.BaseRetryDelay < TimeSpan.FromSeconds(1))
        {
            errors.Add($"'{OutboxOptions.SectionName}:BaseRetryDelay' must be at least one second.");
        }

        if (options.MaximumRetryDelay < TimeSpan.FromSeconds(1))
        {
            errors.Add($"'{OutboxOptions.SectionName}:MaximumRetryDelay' must be at least one second.");
        }

        if (options.MaximumRetryDelay < options.BaseRetryDelay)
        {
            errors.Add($"'{OutboxOptions.SectionName}:MaximumRetryDelay' must be greater than or equal to '{OutboxOptions.SectionName}:BaseRetryDelay'.");
        }

        return errors.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(errors);
    }
}
