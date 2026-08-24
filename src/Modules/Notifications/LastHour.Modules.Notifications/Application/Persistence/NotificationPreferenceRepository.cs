using LastHour.BuildingBlocks.Application.Contracts;
using LastHour.Modules.Notifications.Domain.Notifications;

namespace LastHour.Modules.Notifications.Application.Persistence;

/// <summary>
/// EF Core repository implementation for user <see cref="NotificationPreference"/> management.
/// </summary>
public sealed class NotificationPreferenceRepository : INotificationPreferenceRepository
{
    private readonly IRepository<NotificationPreference> _repository;
    private readonly IReadRepository<NotificationPreference, Guid> _readRepository;

    /// <summary>
    /// Initializes a new instance of the <see cref="NotificationPreferenceRepository"/> class.
    /// </summary>
    public NotificationPreferenceRepository(
        IRepository<NotificationPreference> repository,
        IReadRepository<NotificationPreference, Guid> readRepository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _readRepository = readRepository ?? throw new ArgumentNullException(nameof(readRepository));
    }

    /// <inheritdoc/>
    public Task AddAsync(NotificationPreference preference, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(preference);
        _repository.Add(preference);
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task UpdateAsync(NotificationPreference preference, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(preference);
        _repository.Update(preference);
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public async Task<NotificationPreference?> GetByUserAndTypeAndChannelAsync(Guid userId, string notificationType, DeliveryChannel channel, CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty)
        {
            return null;
        }

        string normalizedType = notificationType.Trim();
        IReadOnlyList<NotificationPreference> results = await _readRepository.ListAsync(
            p => p.UserId == userId && p.NotificationType == normalizedType && p.Channel == channel,
            cancellationToken).ConfigureAwait(false);

        return results.Count == 0 ? null : results[0];
    }

    /// <inheritdoc/>
    public Task<IReadOnlyList<NotificationPreference>> GetByUserAndTypeAsync(Guid userId, string notificationType, CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty)
        {
            return Task.FromResult<IReadOnlyList<NotificationPreference>>(Array.Empty<NotificationPreference>());
        }

        string normalizedType = notificationType.Trim();
        return _readRepository.ListAsync(p => p.UserId == userId && p.NotificationType == normalizedType, cancellationToken);
    }

    /// <inheritdoc/>
    public Task<IReadOnlyList<NotificationPreference>> GetByUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty)
        {
            return Task.FromResult<IReadOnlyList<NotificationPreference>>(Array.Empty<NotificationPreference>());
        }

        return _readRepository.ListAsync(p => p.UserId == userId, cancellationToken);
    }
}
