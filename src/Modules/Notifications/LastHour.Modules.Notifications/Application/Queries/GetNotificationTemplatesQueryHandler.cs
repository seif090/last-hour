using LastHour.BuildingBlocks.Application.Cqrs;
using LastHour.BuildingBlocks.SharedKernel.Results;
using LastHour.Modules.Notifications.Application.Models;
using LastHour.Modules.Notifications.Application.Persistence;
using LastHour.Modules.Notifications.Domain.Notifications;

namespace LastHour.Modules.Notifications.Application.Queries;

/// <summary>
/// Handler for <see cref="GetNotificationTemplatesQuery"/>.
/// </summary>
public sealed class GetNotificationTemplatesQueryHandler : IQueryHandler<GetNotificationTemplatesQuery, IReadOnlyList<NotificationTemplateDto>>
{
    private readonly INotificationTemplateRepository _templateRepository;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetNotificationTemplatesQueryHandler"/> class.
    /// </summary>
    public GetNotificationTemplatesQueryHandler(INotificationTemplateRepository templateRepository)
    {
        _templateRepository = templateRepository ?? throw new ArgumentNullException(nameof(templateRepository));
    }

    /// <inheritdoc/>
    public async Task<Result<IReadOnlyList<NotificationTemplateDto>>> Handle(GetNotificationTemplatesQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        IReadOnlyList<NotificationTemplate> templates = await _templateRepository
            .ListAllAsync(cancellationToken)
            .ConfigureAwait(false);

        IReadOnlyList<NotificationTemplateDto> dtos = templates
            .Select(t => new NotificationTemplateDto(
                t.Id,
                t.TemplateKey,
                t.NotificationType,
                t.Channel.ToString(),
                t.Version,
                t.Subject,
                t.Body,
                t.IsActive,
                t.UpdatedAtUtc))
            .ToList();

        return Result<IReadOnlyList<NotificationTemplateDto>>.Success(dtos);
    }
}
