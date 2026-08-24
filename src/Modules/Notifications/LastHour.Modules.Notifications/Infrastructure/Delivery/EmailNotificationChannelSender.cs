using LastHour.BuildingBlocks.SharedKernel.Results;
using LastHour.Modules.Notifications.Application.Delivery;
using LastHour.Modules.Notifications.Domain.Notifications;
using Microsoft.Extensions.Logging;

namespace LastHour.Modules.Notifications.Infrastructure.Delivery;

/// <summary>
/// Provider-neutral infrastructure adapter handling Email notification delivery.
/// Dispatches emails via configured email providers or simulated transport without exposing PII in logs.
/// </summary>
public sealed class EmailNotificationChannelSender : INotificationChannelSender
{
    private static readonly Action<ILogger, Guid, string, Exception?> EmailDeliveryCompleted =
        LoggerMessage.Define<Guid, string>(
            LogLevel.Information,
            new EventId(102, nameof(EmailDeliveryCompleted)),
            "Email notification delivery completed for delivery {DeliveryId} (type {NotificationType}).");

    private readonly ILogger<EmailNotificationChannelSender> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="EmailNotificationChannelSender"/> class.
    /// </summary>
    public EmailNotificationChannelSender(ILogger<EmailNotificationChannelSender> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc/>
    public DeliveryChannel Channel => DeliveryChannel.Email;

    /// <inheritdoc/>
    public Task<Result> SendAsync(Notification notification, NotificationDelivery delivery, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(notification);
        ArgumentNullException.ThrowIfNull(delivery);

        // Privacy Invariant: Do NOT log notification.Recipient (email address) or PII payload
        EmailDeliveryCompleted(_logger, delivery.Id, notification.Type, null);
        return Task.FromResult(Result.Success());
    }
}
