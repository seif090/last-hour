namespace LastHour.Modules.Notifications.Infrastructure.Delivery;

/// <summary>
/// Configuration options governing the background notification delivery processor.
/// </summary>
public sealed class NotificationDeliveryOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether the notification delivery processor background service is enabled.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the maximum number of delivery items processed per polling cycle.
    /// </summary>
    public int BatchSize { get; set; } = 50;

    /// <summary>
    /// Gets or sets the delay interval between processing polling cycles.
    /// </summary>
    public TimeSpan PollingInterval { get; set; } = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Gets or sets the duration of worker processing leases before expiration.
    /// </summary>
    public TimeSpan LeaseDuration { get; set; } = TimeSpan.FromMinutes(2);

    /// <summary>
    /// Gets or sets the base delay used for exponential retry backoff.
    /// </summary>
    public TimeSpan BaseRetryDelay { get; set; } = TimeSpan.FromSeconds(10);

    /// <summary>
    /// Gets or sets the maximum retry delay cap.
    /// </summary>
    public TimeSpan MaximumRetryDelay { get; set; } = TimeSpan.FromHours(1);

    /// <summary>
    /// Gets or sets the maximum retry attempts before transitioning to permanent failure.
    /// </summary>
    public int MaxRetryCount { get; set; } = 3;
}
