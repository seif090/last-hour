using LastHour.BuildingBlocks.SharedKernel.Results;
using LastHour.Modules.Notifications.Application.Delivery;
using LastHour.Modules.Notifications.Domain.Notifications;
using Microsoft.Extensions.Logging;

namespace LastHour.Modules.Notifications.Infrastructure.Delivery;

/// <summary>
/// Provider-neutral infrastructure adapter handling SMS notification delivery.
/// Dispatches SMS messages via configured SMS providers or simulated transport without exposing PII in logs.
/// </summary>
public sealed class SmsNotificationChannelSender : INotificationChannelSender
{
    private static readonly Action<ILogger, Guid, string, Exception?> SmsDeliveryCompleted =
        LoggerMessage.Define<Guid, string>(
            LogLevel.Information,
            new EventId(103, nameof(SmsDeliveryCompleted)),
            "SMS notification delivery completed for delivery {DeliveryId} (type {NotificationType}).");

    private readonly ILogger<SmsNotificationChannelSender> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="SmsNotificationChannelSender"/> class.
    /// </summary>
    public SmsNotificationChannelSender(ILogger<SmsNotificationChannelSender> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc/>
    public DeliveryChannel Channel => DeliveryChannel.Sms;

    /// <inheritdoc/>
    public Task<Result> SendAsync(Notification notification, NotificationDelivery delivery, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(notification);
        ArgumentNullException.ThrowIfNull(delivery);

        // Privacy Invariant: Do NOT log notification.Recipient (phone number) or PII payload
        SmsDeliveryCompleted(_logger, delivery.Id, notification.Type, null);
        return Task.FromResult(Result.Success());
    }
}
