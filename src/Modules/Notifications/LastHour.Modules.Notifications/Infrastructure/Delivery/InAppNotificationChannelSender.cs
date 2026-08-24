using LastHour.BuildingBlocks.SharedKernel.Results;
using LastHour.Modules.Notifications.Application.Delivery;
using LastHour.Modules.Notifications.Domain.Notifications;
using Microsoft.Extensions.Logging;

namespace LastHour.Modules.Notifications.Infrastructure.Delivery;

/// <summary>
/// Infrastructure adapter handling In-App notification delivery.
/// Since the <see cref="Notification"/> aggregate itself is persisted in the database as the user-visible in-app notification,
/// this sender records delivery completion deterministically without external network calls.
/// </summary>
public sealed class InAppNotificationChannelSender : INotificationChannelSender
{
    private static readonly Action<ILogger, Guid, Exception?> InAppDeliveryCompleted =
        LoggerMessage.Define<Guid>(
            LogLevel.Debug,
            new EventId(101, nameof(InAppDeliveryCompleted)),
            "In-app notification delivery completed for delivery {DeliveryId}.");

    private readonly ILogger<InAppNotificationChannelSender> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="InAppNotificationChannelSender"/> class.
    /// </summary>
    public InAppNotificationChannelSender(ILogger<InAppNotificationChannelSender> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc/>
    public DeliveryChannel Channel => DeliveryChannel.InApp;

    /// <inheritdoc/>
    public Task<Result> SendAsync(Notification notification, NotificationDelivery delivery, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(notification);
        ArgumentNullException.ThrowIfNull(delivery);

        InAppDeliveryCompleted(_logger, delivery.Id, null);
        return Task.FromResult(Result.Success());
    }
}
