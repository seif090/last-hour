using Microsoft.Extensions.Options;

namespace LastHour.Modules.Notifications.Infrastructure.Delivery;

/// <summary>
/// Startup validator for <see cref="NotificationDeliveryOptions"/>.
/// </summary>
public sealed class NotificationDeliveryOptionsValidator : IValidateOptions<NotificationDeliveryOptions>
{
    /// <inheritdoc/>
    public ValidateOptionsResult Validate(string? name, NotificationDeliveryOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var errors = new List<string>();

        if (options.BatchSize <= 0)
        {
            errors.Add("BatchSize must be greater than 0.");
        }

        if (options.PollingInterval <= TimeSpan.Zero)
        {
            errors.Add("PollingInterval must be greater than zero.");
        }

        if (options.LeaseDuration <= TimeSpan.Zero)
        {
            errors.Add("LeaseDuration must be greater than zero.");
        }

        if (options.BaseRetryDelay <= TimeSpan.Zero)
        {
            errors.Add("BaseRetryDelay must be greater than zero.");
        }

        if (options.MaximumRetryDelay < options.BaseRetryDelay)
        {
            errors.Add("MaximumRetryDelay must be greater than or equal to BaseRetryDelay.");
        }

        if (options.MaxRetryCount < 0)
        {
            errors.Add("MaxRetryCount cannot be negative.");
        }

        return errors.Count > 0
            ? ValidateOptionsResult.Fail(errors)
            : ValidateOptionsResult.Success;
    }
}
