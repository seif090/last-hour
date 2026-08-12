using LastHour.Modules.Identity.Domain.RefreshTokens;
using LastHour.Modules.Identity.Domain.Users;

namespace LastHour.Modules.Identity.Tests.Application.TestSupport;

/// <summary>
/// Deterministic in-memory stand-in for the refresh-token repository, keyed by stored hash
/// exactly like the production lookup. Refresh and logout tests drive the real aggregate
/// through this fake, so no database is needed.
/// </summary>
public sealed class FakeRefreshTokenRepository : IRefreshTokenRepository
{
    private readonly List<RefreshToken> _tokens = new();

    /// <summary>
    /// Gets all token records, for assertions.
    /// </summary>
    public IReadOnlyList<RefreshToken> All => _tokens;

    /// <summary>
    /// Adds a token record directly (used to seed the fake).
    /// </summary>
    /// <param name="token">The token record to add.</param>
    public void Seed(RefreshToken token) => _tokens.Add(token);

    /// <inheritdoc/>
    public void Add(RefreshToken aggregateRoot) => _tokens.Add(aggregateRoot);

    /// <inheritdoc/>
    public void Update(RefreshToken aggregateRoot)
    {
        int index = _tokens.FindIndex(token => token.Id == aggregateRoot.Id);
        if (index >= 0)
        {
            _tokens[index] = aggregateRoot;
        }
    }

    /// <inheritdoc/>
    public void Remove(RefreshToken aggregateRoot) => _tokens.Remove(aggregateRoot);

    /// <inheritdoc/>
    public void RemoveRange(IEnumerable<RefreshToken> aggregateRoots)
    {
        foreach (RefreshToken token in aggregateRoots)
        {
            Remove(token);
        }
    }

    /// <inheritdoc/>
    public Task<RefreshToken?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default)
        => Task.FromResult(_tokens.FirstOrDefault(token => token.TokenHash == tokenHash));

    /// <inheritdoc/>
    public Task<IReadOnlyList<RefreshToken>> GetActiveByFamilyIdAsync(Guid familyId, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<RefreshToken>>(
            _tokens.Where(token => token.FamilyId == familyId && token.RevokedAtUtc is null).ToList());

    /// <inheritdoc/>
    public Task<IReadOnlyList<RefreshToken>> GetActiveByUserIdAsync(UserId userId, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<RefreshToken>>(
            _tokens.Where(token => token.UserId == userId && token.RevokedAtUtc is null).ToList());

    /// <inheritdoc/>
    public Task<IReadOnlyList<RefreshToken>> GetAllByUserIdAsync(UserId userId, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<RefreshToken>>(
            _tokens.Where(token => token.UserId == userId).ToList());
}
