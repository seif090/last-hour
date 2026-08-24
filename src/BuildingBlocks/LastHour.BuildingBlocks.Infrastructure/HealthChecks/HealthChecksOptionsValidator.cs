using Microsoft.Extensions.Options;

namespace LastHour.BuildingBlocks.Infrastructure.HealthChecks;

/// <summary>
/// Validates the <see cref="HealthChecksOptions"/> so misconfiguration fails fast at startup.
/// </summary>
public sealed class HealthChecksOptionsValidator : IValidateOptions<HealthChecksOptions>
{
    /// <inheritdoc/>
    public ValidateOptionsResult Validate(string? name, HealthChecksOptions options)
    {
        var errors = new List<string>();

        if (options.TimeoutSeconds <= 0)
        {
            errors.Add("HealthChecks:TimeoutSeconds must be a positive number of seconds.");
        }

        if (options.Disk.MinimumFreeMegabytes < 0)
        {
            errors.Add("HealthChecks:Disk:MinimumFreeMegabytes must not be negative.");
        }

        if (options.Memory.MaximumUsedBytes <= 0)
        {
            errors.Add("HealthChecks:Memory:MaximumUsedBytes must be a positive number of bytes.");
        }

        if (options.Outbox.WarningPendingMessageCount < 0)
        {
            errors.Add("HealthChecks:Outbox:WarningPendingMessageCount must not be negative.");
        }

        if (options.Outbox.CriticalPendingMessageCount <= options.Outbox.WarningPendingMessageCount)
        {
            errors.Add("HealthChecks:Outbox:CriticalPendingMessageCount must be greater than WarningPendingMessageCount.");
        }

        if (options.Outbox.WarningOldestMessageAge <= TimeSpan.Zero)
        {
            errors.Add("HealthChecks:Outbox:WarningOldestMessageAge must be greater than zero.");
        }

        if (options.Outbox.CriticalOldestMessageAge <= options.Outbox.WarningOldestMessageAge)
        {
            errors.Add("HealthChecks:Outbox:CriticalOldestMessageAge must be greater than WarningOldestMessageAge.");
        }

        if (options.Outbox.ProcessorHeartbeatTimeout <= TimeSpan.Zero)
        {
            errors.Add("HealthChecks:Outbox:ProcessorHeartbeatTimeout must be greater than zero.");
        }

        if (options.Outbox.WarningAbandonedMessageCount < 0)
        {
            errors.Add("HealthChecks:Outbox:WarningAbandonedMessageCount must not be negative.");
        }

        if (options.Outbox.CriticalAbandonedMessageCount <= options.Outbox.WarningAbandonedMessageCount)
        {
            errors.Add("HealthChecks:Outbox:CriticalAbandonedMessageCount must be greater than WarningAbandonedMessageCount.");
        }

        if (options.ConsumerInbox.WarningStalledLeaseCount < 0)
        {
            errors.Add("HealthChecks:ConsumerInbox:WarningStalledLeaseCount must not be negative.");
        }

        if (options.ConsumerInbox.CriticalStalledLeaseCount <= options.ConsumerInbox.WarningStalledLeaseCount)
        {
            errors.Add("HealthChecks:ConsumerInbox:CriticalStalledLeaseCount must be greater than WarningStalledLeaseCount.");
        }

        if (options.ConsumerInbox.WarningOldestStalledAge <= TimeSpan.Zero)
        {
            errors.Add("HealthChecks:ConsumerInbox:WarningOldestStalledAge must be greater than zero.");
        }

        if (options.ConsumerInbox.CriticalOldestStalledAge <= options.ConsumerInbox.WarningOldestStalledAge)
        {
            errors.Add("HealthChecks:ConsumerInbox:CriticalOldestStalledAge must be greater than WarningOldestStalledAge.");
        }

        return errors.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(errors);
    }
}
