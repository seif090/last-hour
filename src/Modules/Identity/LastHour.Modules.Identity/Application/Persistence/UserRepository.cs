using LastHour.BuildingBlocks.Application.Contracts;
using LastHour.Modules.Identity.Domain.Users;

namespace LastHour.Modules.Identity.Application.Persistence;

/// <summary>
/// The <see cref="IUserRepository"/> implementation, composed purely from the shared repository
/// contracts. Writes delegate to the tracking <see cref="IRepository{TAggregateRoot}"/> and
/// queries to the no-tracking <see cref="IReadRepository{TEntity, TId}"/>, so the module needs
/// no infrastructure reference. Lookups always run against the stored normalized email value,
/// computed by the domain's single normalization rule.
/// </summary>
public sealed class UserRepository : IUserRepository
{
    private readonly IRepository<User> _repository;
    private readonly IReadRepository<User, UserId> _readRepository;
    private readonly ITrackedRepository<User, UserId> _trackedRepository;

    /// <summary>
    /// Initializes a new instance of the <see cref="UserRepository"/> class.
    /// </summary>
    /// <param name="repository">The shared write repository for the <see cref="User"/> aggregate.</param>
    /// <param name="readRepository">The shared read repository for <see cref="User"/> entities.</param>
    /// <param name="trackedRepository">The shared tracked repository used to load the aggregate
    /// for mutation with optimistic-concurrency protection.</param>
    /// <exception cref="ArgumentNullException">Any argument is <see langword="null"/>.</exception>
    public UserRepository(
        IRepository<User> repository,
        IReadRepository<User, UserId> readRepository,
        ITrackedRepository<User, UserId> trackedRepository)
    {
        ArgumentNullException.ThrowIfNull(repository);
        ArgumentNullException.ThrowIfNull(readRepository);
        ArgumentNullException.ThrowIfNull(trackedRepository);
        _repository = repository;
        _readRepository = readRepository;
        _trackedRepository = trackedRepository;
    }

    /// <inheritdoc/>
    public void Add(User user) => _repository.Add(user);

    /// <inheritdoc/>
    public void Update(User user) => _repository.Update(user);

    /// <inheritdoc/>
    public void Remove(User user) => _repository.Remove(user);

    /// <inheritdoc/>
    public void RemoveRange(IEnumerable<User> aggregateRoots) => _repository.RemoveRange(aggregateRoots);

    /// <inheritdoc/>
    public Task<User?> GetByEmailAsync(Email email, CancellationToken cancellationToken = default)
        => _readRepository.FirstOrDefaultAsync(
            user => user.Email.NormalizedValue == email.NormalizedValue,
            cancellationToken);

    /// <inheritdoc/>
    public Task<bool> ExistsByEmailAsync(Email email, CancellationToken cancellationToken = default)
        => _readRepository.AnyAsync(
            user => user.Email.NormalizedValue == email.NormalizedValue,
            cancellationToken);

    /// <inheritdoc/>
    public Task<User?> GetByIdAsync(UserId id, CancellationToken cancellationToken = default)
        => _readRepository.GetByIdAsync(id, cancellationToken);

    /// <inheritdoc/>
    public Task<User?> GetByIdTrackedAsync(UserId id, CancellationToken cancellationToken = default)
        => _trackedRepository.GetByIdAsync(id, cancellationToken);

    /// <inheritdoc/>
    public Task ReloadTrackedAsync(User user, CancellationToken cancellationToken = default)
        => _trackedRepository.ReloadAsync(user, cancellationToken);
}
