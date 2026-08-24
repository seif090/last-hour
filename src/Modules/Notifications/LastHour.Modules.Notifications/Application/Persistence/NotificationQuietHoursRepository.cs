using LastHour.BuildingBlocks.Application.Contracts;
using LastHour.Modules.Notifications.Domain.Notifications;

namespace LastHour.Modules.Notifications.Application.Persistence;

/// <summary>
/// EF Core repository implementation for user <see cref="NotificationQuietHours"/> management.
/// </summary>
public sealed class NotificationQuietHoursRepository : INotificationQuietHoursRepository
{
    private readonly IRepository<NotificationQuietHours> _repository;
    private readonly IReadRepository<NotificationQuietHours, Guid> _readRepository;

    /// <summary>
    /// Initializes a new instance of the <see cref="NotificationQuietHoursRepository"/> class.
    /// </summary>
    public NotificationQuietHoursRepository(
        IRepository<NotificationQuietHours> repository,
        IReadRepository<NotificationQuietHours, Guid> readRepository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _readRepository = readRepository ?? throw new ArgumentNullException(nameof(readRepository));
    }

    /// <inheritdoc/>
    public Task AddAsync(NotificationQuietHours quietHours, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(quietHours);
        _repository.Add(quietHours);
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task UpdateAsync(NotificationQuietHours quietHours, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(quietHours);
        _repository.Update(quietHours);
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public async Task<NotificationQuietHours?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty)
        {
            return null;
        }

        IReadOnlyList<NotificationQuietHours> results = await _readRepository.ListAsync(q => q.UserId == userId, cancellationToken).ConfigureAwait(false);
        return results.Count == 0 ? null : results[0];
    }
}
