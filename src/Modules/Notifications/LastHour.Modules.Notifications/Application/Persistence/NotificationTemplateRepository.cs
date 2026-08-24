using LastHour.BuildingBlocks.Application.Contracts;
using LastHour.Modules.Notifications.Domain.Notifications;

namespace LastHour.Modules.Notifications.Application.Persistence;

/// <summary>
/// EF Core repository implementation for versioned <see cref="NotificationTemplate"/> management.
/// </summary>
public sealed class NotificationTemplateRepository : INotificationTemplateRepository
{
    private readonly IRepository<NotificationTemplate> _repository;
    private readonly IReadRepository<NotificationTemplate, Guid> _readRepository;

    /// <summary>
    /// Initializes a new instance of the <see cref="NotificationTemplateRepository"/> class.
    /// </summary>
    public NotificationTemplateRepository(
        IRepository<NotificationTemplate> repository,
        IReadRepository<NotificationTemplate, Guid> readRepository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _readRepository = readRepository ?? throw new ArgumentNullException(nameof(readRepository));
    }

    /// <inheritdoc/>
    public Task AddAsync(NotificationTemplate notificationTemplate, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(notificationTemplate);
        _repository.Add(notificationTemplate);
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public async Task<NotificationTemplate?> GetActiveTemplateAsync(string notificationType, DeliveryChannel channel, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(notificationType))
        {
            return null;
        }

        string normalizedType = notificationType.Trim();
        IReadOnlyList<NotificationTemplate> activeTemplates = await _readRepository.ListAsync(
            t => t.NotificationType == normalizedType && t.Channel == channel && t.IsActive,
            cancellationToken).ConfigureAwait(false);

        if (activeTemplates.Count > 1)
        {
            throw new InvalidOperationException(
                $"Data integrity violation: Multiple ({activeTemplates.Count}) active templates found for type '{normalizedType}' on channel '{channel}'.");
        }

        return activeTemplates.Count == 0 ? null : activeTemplates[0];
    }

    /// <inheritdoc/>
    public async Task<NotificationTemplate?> GetByKeyAndVersionAsync(string templateKey, DeliveryChannel channel, int version, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(templateKey))
        {
            return null;
        }

        string normalizedKey = templateKey.Trim();
        IReadOnlyList<NotificationTemplate> results = await _readRepository.ListAsync(
            t => t.TemplateKey == normalizedKey && t.Channel == channel && t.Version == version,
            cancellationToken).ConfigureAwait(false);

        return results.Count == 0 ? null : results[0];
    }

    /// <inheritdoc/>
    public Task<IReadOnlyList<NotificationTemplate>> ListVersionsAsync(string templateKey, DeliveryChannel channel, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(templateKey))
        {
            return Task.FromResult<IReadOnlyList<NotificationTemplate>>(Array.Empty<NotificationTemplate>());
        }

        string normalizedKey = templateKey.Trim();
        return _readRepository.ListAsync(
            t => t.TemplateKey == normalizedKey && t.Channel == channel,
            t => t.Version,
            sortDescending: true,
            skip: 0,
            take: 100,
            cancellationToken: cancellationToken);
    }

    /// <inheritdoc/>
    public Task<IReadOnlyList<NotificationTemplate>> ListAllAsync(CancellationToken cancellationToken = default)
        => _readRepository.ListAsync(t => true, cancellationToken);
}
