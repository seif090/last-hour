using LastHour.BuildingBlocks.Application.Contracts;
using LastHour.BuildingBlocks.Application.Cqrs;
using LastHour.BuildingBlocks.SharedKernel.Results;
using LastHour.Modules.Notifications.Application.Persistence;
using LastHour.Modules.Notifications.Domain.Notifications;

namespace LastHour.Modules.Notifications.Application.Commands;

/// <summary>
/// Handler for <see cref="UpdateNotificationPreferenceCommand"/>.
/// </summary>
public sealed class UpdateNotificationPreferenceCommandHandler : ICommandHandler<UpdateNotificationPreferenceCommand>
{
    private readonly INotificationPreferenceRepository _preferenceRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateNotificationPreferenceCommandHandler"/> class.
    /// </summary>
    public UpdateNotificationPreferenceCommandHandler(
        INotificationPreferenceRepository preferenceRepository,
        IUnitOfWork unitOfWork,
        IClock clock)
    {
        _preferenceRepository = preferenceRepository ?? throw new ArgumentNullException(nameof(preferenceRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
    }

    /// <inheritdoc/>
    public async Task<Result> Handle(UpdateNotificationPreferenceCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.UserId == Guid.Empty)
        {
            return Result.Failure(Error.Unauthorized("Authentication.Required", "User identifier is required."));
        }

        if (string.IsNullOrWhiteSpace(request.NotificationType))
        {
            return Result.Failure(Error.Validation("NotificationPreference.TypeRequired", "Notification type is required."));
        }

        DateTime now = _clock.UtcNow;
        string normalizedType = request.NotificationType.Trim();

        NotificationPreference? preference = await _preferenceRepository
            .GetByUserAndTypeAndChannelAsync(request.UserId, normalizedType, request.Channel, cancellationToken)
            .ConfigureAwait(false);

        if (preference is null)
        {
            preference = NotificationPreference.Create(request.UserId, normalizedType, request.Channel, request.IsEnabled, now);
            await _preferenceRepository.AddAsync(preference, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            if (request.IsEnabled)
            {
                preference.Enable(now);
            }
            else
            {
                preference.Disable(now);
            }

            await _preferenceRepository.UpdateAsync(preference, cancellationToken).ConfigureAwait(false);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }
}
