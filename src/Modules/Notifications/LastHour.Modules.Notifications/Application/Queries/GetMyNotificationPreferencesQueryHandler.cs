using LastHour.BuildingBlocks.Application.Cqrs;
using LastHour.BuildingBlocks.SharedKernel.Results;
using LastHour.Modules.Notifications.Application.Models;
using LastHour.Modules.Notifications.Application.Persistence;
using LastHour.Modules.Notifications.Domain.Notifications;

namespace LastHour.Modules.Notifications.Application.Queries;

/// <summary>
/// Handler for <see cref="GetMyNotificationPreferencesQuery"/>.
/// </summary>
public sealed class GetMyNotificationPreferencesQueryHandler : IQueryHandler<GetMyNotificationPreferencesQuery, IReadOnlyList<NotificationPreferenceDto>>
{
    private readonly INotificationPreferenceRepository _preferenceRepository;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetMyNotificationPreferencesQueryHandler"/> class.
    /// </summary>
    public GetMyNotificationPreferencesQueryHandler(INotificationPreferenceRepository preferenceRepository)
    {
        _preferenceRepository = preferenceRepository ?? throw new ArgumentNullException(nameof(preferenceRepository));
    }

    /// <inheritdoc/>
    public async Task<Result<IReadOnlyList<NotificationPreferenceDto>>> Handle(GetMyNotificationPreferencesQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.UserId == Guid.Empty)
        {
            return Result<IReadOnlyList<NotificationPreferenceDto>>.Failure(
                Error.Unauthorized("Authentication.Required", "User identifier is required."));
        }

        IReadOnlyList<NotificationPreference> preferences = await _preferenceRepository
            .GetByUserAsync(request.UserId, cancellationToken)
            .ConfigureAwait(false);

        IReadOnlyList<NotificationPreferenceDto> dtos = preferences
            .Select(p => new NotificationPreferenceDto(p.NotificationType, p.Channel.ToString(), p.IsEnabled, p.UpdatedAtUtc))
            .ToList();

        return Result<IReadOnlyList<NotificationPreferenceDto>>.Success(dtos);
    }
}
