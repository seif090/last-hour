using LastHour.BuildingBlocks.Application.Contracts;
using LastHour.BuildingBlocks.Application.Events;
using LastHour.BuildingBlocks.Contracts.Events;
using LastHour.Modules.Notifications.Application.Delivery;
using LastHour.Modules.Notifications.Application.Persistence;
using LastHour.Modules.Notifications.Domain.Notifications;
using LastHour.Modules.Notifications.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LastHour.Modules.Notifications.Application.Consumers;

/// <summary>
/// Event consumer that handles <see cref="MerchantStatusChangedDomainEvent"/> notifications by persisting
/// a merchant lifecycle status change notification in the Notifications module under consumer idempotency controls.
/// </summary>
public sealed class MerchantStatusChangedNotificationHandler : INotificationHandler<NotificationMessage<MerchantStatusChangedDomainEvent>>
{
    /// <summary>
    /// The canonical consumer identifier used for consumer-scoped idempotency boundaries.
    /// </summary>
    public const string ConsumerName = "Notifications.MerchantStatusChanged";

    private const string NotificationType = "MerchantStatusChanged";

    private static readonly Action<ILogger, Guid, Guid, string, string, Exception?> MerchantStatusChangedNotificationCreated =
        LoggerMessage.Define<Guid, Guid, string, string>(
            LogLevel.Information,
            new EventId(1, nameof(MerchantStatusChangedNotificationCreated)),
            "Created merchant status change notification {NotificationId} for merchant {MerchantId} (status: {PreviousStatus} -> {CurrentStatus}).");

    private static readonly Action<ILogger, Guid, Guid, Exception?> MerchantStatusChangedNotificationAlreadyExists =
        LoggerMessage.Define<Guid, Guid>(
            LogLevel.Information,
            new EventId(2, nameof(MerchantStatusChangedNotificationAlreadyExists)),
            "Merchant status change notification already exists for event {EventId} and merchant {MerchantId}; skipping creation.");

    private readonly IConsumerIdempotencyService _idempotencyService;
    private readonly INotificationRepository _notificationRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly ILogger<MerchantStatusChangedNotificationHandler> _logger;
    private readonly INotificationDeliveryPolicyResolver _policyResolver;
    private readonly INotificationDeliveryRepository? _deliveryRepository;
    private readonly INotificationQuietHoursResolver? _quietHoursResolver;

    public MerchantStatusChangedNotificationHandler(
        IConsumerIdempotencyService idempotencyService,
        INotificationRepository notificationRepository,
        IUnitOfWork unitOfWork,
        IClock clock,
        ILogger<MerchantStatusChangedNotificationHandler> logger,
        INotificationDeliveryPolicyResolver? policyResolver = null,
        INotificationDeliveryRepository? deliveryRepository = null,
        INotificationQuietHoursResolver? quietHoursResolver = null)
    {
        _idempotencyService = idempotencyService ?? throw new ArgumentNullException(nameof(idempotencyService));
        _notificationRepository = notificationRepository ?? throw new ArgumentNullException(nameof(notificationRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _policyResolver = policyResolver ?? new NotificationDeliveryPolicyResolver();
        _deliveryRepository = deliveryRepository;
        _quietHoursResolver = quietHoursResolver;
    }

    /// <inheritdoc/>
    public async Task Handle(
        NotificationMessage<MerchantStatusChangedDomainEvent> notification,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(notification);
        MerchantStatusChangedDomainEvent domainEvent = notification.Message;

        await _idempotencyService.ExecuteAsync(
            domainEvent,
            ConsumerName,
            async ct =>
            {
                // Pre-check for existing business effect
                bool alreadyExists = await _notificationRepository
                    .ExistsByEventIdAndTypeAsync(domainEvent.EventId, NotificationType, ct)
                    .ConfigureAwait(false);

                if (alreadyExists)
                {
                    MerchantStatusChangedNotificationAlreadyExists(_logger, domainEvent.EventId, domainEvent.MerchantId, null);
                    return;
                }

                (string title, string body) = ResolveNotificationContent(domainEvent.PreviousStatus, domainEvent.CurrentStatus);

                var record = Notification.Create(
                    domainEvent.EventId,
                    domainEvent.MerchantId,
                    domainEvent.MerchantId.ToString("D"),
                    title,
                    body,
                    NotificationType,
                    _clock.UtcNow);

                await _notificationRepository.AddAsync(record, ct).ConfigureAwait(false);

                if (_deliveryRepository is not null)
                {
                    IReadOnlyList<DeliveryChannel> channels = await _policyResolver.ResolveChannelsAsync(record, ct).ConfigureAwait(false);
                    foreach (DeliveryChannel channel in channels)
                    {
                        DateTime? scheduledAt = _quietHoursResolver is not null
                            ? await _quietHoursResolver.CalculateScheduledAttemptAtAsync(record.RecipientId, channel, _clock.UtcNow, ct).ConfigureAwait(false)
                            : _clock.UtcNow;

                        var delivery = NotificationDelivery.Create(record.Id, channel, scheduledAt ?? _clock.UtcNow);
                        await _deliveryRepository.AddAsync(delivery, ct).ConfigureAwait(false);
                    }
                }

                try
                {
                    await _unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);
                    MerchantStatusChangedNotificationCreated(
                        _logger,
                        record.Id,
                        domainEvent.MerchantId,
                        domainEvent.PreviousStatus,
                        domainEvent.CurrentStatus,
                        null);
                }
                catch (DbUpdateException ex) when (NotificationDbExceptionDetector.IsNotificationUniqueConstraintViolation(ex))
                {
                    // Database uniqueness constraint detected existing business effect from concurrent or prior committed worker.
                    MerchantStatusChangedNotificationAlreadyExists(_logger, domainEvent.EventId, domainEvent.MerchantId, ex);
                }
            },
            cancellationToken).ConfigureAwait(false);
    }

    private static (string Title, string Body) ResolveNotificationContent(string? previousStatus, string? currentStatus)
    {
        string prev = previousStatus?.Trim() ?? string.Empty;
        string curr = currentStatus?.Trim() ?? string.Empty;

        return (prev, curr) switch
        {
            ("Pending", "Active") => (
                "Merchant Account Activated",
                "Your merchant account has been activated."),
            ("Active", "Suspended") => (
                "Merchant Account Suspended",
                "Your merchant account has been suspended."),
            ("Suspended", "Active") => (
                "Merchant Account Reactivated",
                "Your merchant account has been reactivated."),
            (_, "Closed") => (
                "Merchant Account Closed",
                "Your merchant account has been closed."),
            _ => (
                $"Merchant Account Status Changed: {curr}",
                $"Your merchant account status changed from '{prev}' to '{curr}'.")
        };
    }
}
