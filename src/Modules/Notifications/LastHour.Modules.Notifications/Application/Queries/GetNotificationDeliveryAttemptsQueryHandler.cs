using LastHour.BuildingBlocks.Application.Cqrs;
using LastHour.BuildingBlocks.SharedKernel.Results;
using LastHour.Modules.Notifications.Application.Models;
using LastHour.Modules.Notifications.Application.Persistence;
using LastHour.Modules.Notifications.Domain.Notifications;

namespace LastHour.Modules.Notifications.Application.Queries;

/// <summary>
/// Handles <see cref="GetNotificationDeliveryAttemptsQuery"/> to return attempt history projections.
/// </summary>
public sealed class GetNotificationDeliveryAttemptsQueryHandler : IQueryHandler<GetNotificationDeliveryAttemptsQuery, IReadOnlyCollection<NotificationDeliveryAttemptDto>>
{
    private readonly INotificationDeliveryAttemptRepository _attemptRepository;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetNotificationDeliveryAttemptsQueryHandler"/> class.
    /// </summary>
    public GetNotificationDeliveryAttemptsQueryHandler(INotificationDeliveryAttemptRepository attemptRepository)
    {
        _attemptRepository = attemptRepository ?? throw new ArgumentNullException(nameof(attemptRepository));
    }

    /// <inheritdoc/>
    public async Task<Result<IReadOnlyCollection<NotificationDeliveryAttemptDto>>> Handle(
        GetNotificationDeliveryAttemptsQuery request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        IReadOnlyList<NotificationDeliveryAttempt> attempts = await _attemptRepository.GetByDeliveryIdAsync(request.DeliveryId, cancellationToken);

        IReadOnlyCollection<NotificationDeliveryAttemptDto> dtos = attempts
            .Select(a => new NotificationDeliveryAttemptDto(
                a.Id,
                a.DeliveryId,
                a.AttemptNumber,
                a.Channel,
                a.StartedAtUtc,
                a.CompletedAtUtc,
                a.DurationMilliseconds,
                a.Status,
                a.FailureType,
                a.CreatedAtUtc))
            .ToList();

        return Result<IReadOnlyCollection<NotificationDeliveryAttemptDto>>.Success(dtos);
    }
}
