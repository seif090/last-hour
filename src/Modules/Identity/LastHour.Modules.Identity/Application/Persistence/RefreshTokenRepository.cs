using LastHour.BuildingBlocks.Application.Contracts;
using LastHour.Modules.Identity.Domain.RefreshTokens;
using LastHour.Modules.Identity.Domain.Users;

namespace LastHour.Modules.Identity.Application.Persistence;

/// <summary>
/// The <see cref="IRefreshTokenRepository"/> implementation, composed purely from the shared
/// repository contracts. Writes delegate to the tracking <see cref="IRepository{TAggregateRoot}"/>
/// and queries to the no-tracking <see cref="IReadRepository{TEntity, TId}"/>, so the module
/// needs no infrastructure reference. Token lookups always run against the stored hash value,
/// never the raw secret.
/// </summary>
public sealed class RefreshTokenRepository : IRefreshTokenRepository
{
    private readonly IRepository<RefreshToken> _repository;
    private readonly IReadRepository<RefreshToken, RefreshTokenId> _readRepository;

    /// <summary>
    /// Initializes a new instance of the <see cref="RefreshTokenRepository"/> class.
    /// </summary>
    /// <param name="repository">The shared write repository for <see cref="RefreshToken"/> records.</param>
    /// <param name="readRepository">The shared read repository for <see cref="RefreshToken"/> records.</param>
    /// <exception cref="ArgumentNullException">Any argument is <see langword="null"/>.</exception>
    public RefreshTokenRepository(
        IRepository<RefreshToken> repository,
        IReadRepository<RefreshToken, RefreshTokenId> readRepository)
    {
        ArgumentNullException.ThrowIfNull(repository);
        ArgumentNullException.ThrowIfNull(readRepository);
        _repository = repository;
        _readRepository = readRepository;
    }

    /// <inheritdoc/>
    public void Add(RefreshToken aggregateRoot) => _repository.Add(aggregateRoot);

    /// <inheritdoc/>
    public void Update(RefreshToken aggregateRoot) => _repository.Update(aggregateRoot);

    /// <inheritdoc/>
    public void Remove(RefreshToken aggregateRoot) => _repository.Remove(aggregateRoot);

    /// <inheritdoc/>
    public void RemoveRange(IEnumerable<RefreshToken> aggregateRoots) => _repository.RemoveRange(aggregateRoots);

    /// <inheritdoc/>
    public Task<RefreshToken?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default)
        => _readRepository.FirstOrDefaultAsync(
            token => token.TokenHash == tokenHash,
            cancellationToken);

    /// <inheritdoc/>
    public Task<IReadOnlyList<RefreshToken>> GetActiveByFamilyIdAsync(Guid familyId, CancellationToken cancellationToken = default)
        => _readRepository.ListAsync(
            token => token.FamilyId == familyId && token.RevokedAtUtc == null,
            cancellationToken);

    /// <inheritdoc/>
    public Task<IReadOnlyList<RefreshToken>> GetActiveByUserIdAsync(UserId userId, CancellationToken cancellationToken = default)
        => _readRepository.ListAsync(
            token => token.UserId == userId && token.RevokedAtUtc == null,
            cancellationToken);

    /// <inheritdoc/>
    public Task<IReadOnlyList<RefreshToken>> GetAllByUserIdAsync(UserId userId, CancellationToken cancellationToken = default)
        => _readRepository.ListAsync(
            token => token.UserId == userId,
            cancellationToken);
}
