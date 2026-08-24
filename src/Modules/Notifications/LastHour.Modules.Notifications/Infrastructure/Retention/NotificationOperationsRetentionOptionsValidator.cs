using LastHour.Modules.Notifications.Application.Retention;
using Microsoft.Extensions.Options;

namespace LastHour.Modules.Notifications.Infrastructure.Retention;

/// <summary>
/// Startup validator for <see cref="NotificationOperationsRetentionOptions"/>.
/// </summary>
public sealed class NotificationOperationsRetentionOptionsValidator : IValidateOptions<NotificationOperationsRetentionOptions>
{
    /// <inheritdoc/>
    public ValidateOptionsResult Validate(string? name, NotificationOperationsRetentionOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var errors = new List<string>();

        if (options.AttemptRetentionDays is < 1 or > 3650)
        {
            errors.Add("AttemptRetentionDays must be between 1 and 3650 days.");
        }

        if (options.AuditRetentionDays is < 1 or > 3650)
        {
            errors.Add("AuditRetentionDays must be between 1 and 3650 days.");
        }

        if (options.CleanupBatchSize is < 1 or > 1000)
        {
            errors.Add("CleanupBatchSize must be between 1 and 1000 records.");
        }

        if (options.ExecutionIntervalMinutes is < 1 or > 1440)
        {
            errors.Add("ExecutionIntervalMinutes must be between 1 and 1440 minutes.");
        }

        return errors.Count > 0
            ? ValidateOptionsResult.Fail(errors)
            : ValidateOptionsResult.Success;
    }
}
