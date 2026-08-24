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
/// Event consumer that handles <see cref="UserRegisteredDomainEvent"/> lifecycle events by persisting
/// a welcome notification in the Notifications module under consumer idempotency controls.
/// </summary>
public sealed class UserRegisteredNotificationHandler : INotificationHandler<NotificationMessage<UserRegisteredDomainEvent>>
{
    /// <summary>
    /// The canonical consumer identifier used for consumer-scoped idempotency boundaries.
    /// </summary>
    public const string ConsumerName = "Notifications.UserRegistered";

    private const string NotificationType = "UserRegistered";
    private const string NotificationTitle = "Welcome to LastHour";
    private const string NotificationBody = "Your account has been created successfully. Welcome to LastHour.";

    private static readonly Action<ILogger, Guid, Guid, Exception?> UserRegisteredNotificationCreated =
        LoggerMessage.Define<Guid, Guid>(
            LogLevel.Information,
            new EventId(1, nameof(UserRegisteredNotificationCreated)),
            "Created user registration welcome notification {NotificationId} for user {UserId}.");

    private static readonly Action<ILogger, Guid, Guid, Exception?> UserRegisteredNotificationAlreadyExists =
        LoggerMessage.Define<Guid, Guid>(
            LogLevel.Information,
            new EventId(2, nameof(UserRegisteredNotificationAlreadyExists)),
            "User registration welcome notification already exists for event {EventId} and user {UserId}; skipping creation.");

    private readonly IConsumerIdempotencyService _idempotencyService;
    private readonly INotificationRepository _notificationRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly ILogger<UserRegisteredNotificationHandler> _logger;
    private readonly INotificationDeliveryPolicyResolver _policyResolver;
    private readonly INotificationDeliveryRepository? _deliveryRepository;
    private readonly INotificationQuietHoursResolver? _quietHoursResolver;

    public UserRegisteredNotificationHandler(
        IConsumerIdempotencyService idempotencyService,
        INotificationRepository notificationRepository,
        IUnitOfWork unitOfWork,
        IClock clock,
        ILogger<UserRegisteredNotificationHandler> logger,
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
        NotificationMessage<UserRegisteredDomainEvent> notification,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(notification);
        UserRegisteredDomainEvent domainEvent = notification.Message;

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
                    UserRegisteredNotificationAlreadyExists(_logger, domainEvent.EventId, domainEvent.UserId, null);
                    return;
                }

                var record = Notification.Create(
                    domainEvent.EventId,
                    domainEvent.UserId,
                    domainEvent.UserId.ToString("D"),
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
                    UserRegisteredNotificationCreated(
                        _logger,
                        record.Id,
                        domainEvent.UserId,
                        null);
                }
                catch (DbUpdateException ex) when (NotificationDbExceptionDetector.IsNotificationUniqueConstraintViolation(ex))
                {
                    // Database uniqueness constraint detected existing business effect from concurrent or prior committed worker.
                    UserRegisteredNotificationAlreadyExists(_logger, domainEvent.EventId, domainEvent.UserId, ex);
                }
            },
            cancellationToken).ConfigureAwait(false);
    }
}
