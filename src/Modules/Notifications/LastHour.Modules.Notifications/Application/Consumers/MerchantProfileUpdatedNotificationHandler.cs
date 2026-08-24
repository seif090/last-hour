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
/// Event consumer that handles <see cref="MerchantProfileUpdatedDomainEvent"/> notifications by persisting
/// a profile update notification in the Notifications module under consumer idempotency controls.
/// </summary>
public sealed class MerchantProfileUpdatedNotificationHandler : INotificationHandler<NotificationMessage<MerchantProfileUpdatedDomainEvent>>
{
    /// <summary>
    /// The canonical consumer identifier used for consumer-scoped idempotency boundaries.
    /// </summary>
    public const string ConsumerName = "Notifications.MerchantProfileUpdated";

    private const string NotificationType = "MerchantProfileUpdated";
    private const string NotificationTitle = "Merchant Profile Updated";
    private const string NotificationBody = "Your merchant profile has been updated successfully.";

    private static readonly Action<ILogger, Guid, Guid, Exception?> MerchantProfileUpdatedNotificationCreated =
        LoggerMessage.Define<Guid, Guid>(
            LogLevel.Information,
            new EventId(1, nameof(MerchantProfileUpdatedNotificationCreated)),
            "Created merchant profile update notification {NotificationId} for merchant {MerchantId}.");

    private static readonly Action<ILogger, Guid, Guid, Exception?> MerchantProfileUpdatedNotificationAlreadyExists =
        LoggerMessage.Define<Guid, Guid>(
            LogLevel.Information,
            new EventId(2, nameof(MerchantProfileUpdatedNotificationAlreadyExists)),
            "Merchant profile update notification already exists for event {EventId} and merchant {MerchantId}; skipping creation.");

    private readonly IConsumerIdempotencyService _idempotencyService;
    private readonly INotificationRepository _notificationRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly ILogger<MerchantProfileUpdatedNotificationHandler> _logger;

    private readonly INotificationDeliveryPolicyResolver _policyResolver;
    private readonly INotificationDeliveryRepository? _deliveryRepository;
    private readonly INotificationQuietHoursResolver? _quietHoursResolver;

    public MerchantProfileUpdatedNotificationHandler(
        IConsumerIdempotencyService idempotencyService,
        INotificationRepository notificationRepository,
        IUnitOfWork unitOfWork,
        IClock clock,
        ILogger<MerchantProfileUpdatedNotificationHandler> logger,
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
        NotificationMessage<MerchantProfileUpdatedDomainEvent> notification,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(notification);
        MerchantProfileUpdatedDomainEvent domainEvent = notification.Message;

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
                    MerchantProfileUpdatedNotificationAlreadyExists(_logger, domainEvent.EventId, domainEvent.MerchantId, null);
                    return;
                }

                var record = Notification.Create(
                    domainEvent.EventId,
                    domainEvent.MerchantId,
                    domainEvent.MerchantId.ToString("D"),
                    NotificationTitle,
                    NotificationBody,
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
                    MerchantProfileUpdatedNotificationCreated(
                        _logger,
                        record.Id,
                        domainEvent.MerchantId,
                        null);
                }
                catch (DbUpdateException ex) when (NotificationDbExceptionDetector.IsNotificationUniqueConstraintViolation(ex))
                {
                    // Database uniqueness constraint detected existing business effect from concurrent or prior committed worker.
                    MerchantProfileUpdatedNotificationAlreadyExists(_logger, domainEvent.EventId, domainEvent.MerchantId, ex);
                }
            },
            cancellationToken).ConfigureAwait(false);
    }
}
