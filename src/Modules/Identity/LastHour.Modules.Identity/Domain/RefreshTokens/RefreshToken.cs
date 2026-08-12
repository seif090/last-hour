using LastHour.BuildingBlocks.SharedKernel.Domain;
using LastHour.Modules.Identity.Domain.Users;

namespace LastHour.Modules.Identity.Domain.RefreshTokens;

/// <summary>
/// A single refresh-token record of a user session. The aggregate stores only the hash of the
/// raw token secret — the raw value exists solely in the client's hands and can never be
/// recovered from the database. Rotation links each token to its successor, and every token
/// carries a <see cref="FamilyId"/>: the identifier of the session's first token, which reuse
/// detection uses to revoke an entire session when a rotated token is presented again. The
/// database enforces that at most one token per family is active at a time (a partial unique
/// index), which is what makes concurrent rotations safe. The aggregate deliberately holds
/// no plaintext secrets, device metadata or access tokens.
/// </summary>
public sealed class RefreshToken : AggregateRoot<RefreshTokenId>
{
    /// <summary>
    /// The maximum length of a stored token hash (SHA-256, lowercase hex).
    /// </summary>
    public const int HashLength = 64;

    private RefreshToken()
    {
        // EF Core materialization only.
    }

    private RefreshToken(
        RefreshTokenId id,
        UserId userId,
        Guid familyId,
        string tokenHash,
        DateTime createdAtUtc,
        DateTime expiresAtUtc)
    {
        Id = id;
        UserId = userId;
        FamilyId = familyId;
        TokenHash = tokenHash;
        CreatedAtUtc = createdAtUtc;
        ExpiresAtUtc = expiresAtUtc;
    }

    /// <summary>
    /// Gets the identifier of the account the token belongs to.
    /// </summary>
    public UserId UserId { get; private set; } = null!;

    /// <summary>
    /// Gets the hash of the raw token secret, used for lookups and verification. The raw
    /// secret itself is never stored.
    /// </summary>
    public string TokenHash { get; private set; } = null!;

    /// <summary>
    /// Gets the identifier of the session's token family: the id of the first token of the
    /// session. Every rotated successor keeps the same family id, so a reused rotated token
    /// can be detected and the whole family revoked.
    /// </summary>
    public Guid FamilyId { get; private set; }

    /// <summary>
    /// Gets the moment the token was issued, in UTC.
    /// </summary>
    public DateTime CreatedAtUtc { get; private set; }

    /// <summary>
    /// Gets the moment the token expires, in UTC. An expired token can never be used to refresh.
    /// </summary>
    public DateTime ExpiresAtUtc { get; private set; }

    /// <summary>
    /// Gets the moment the token was revoked, or <see langword="null"/> while the token is active.
    /// </summary>
    public DateTime? RevokedAtUtc { get; private set; }

    /// <summary>
    /// Gets the identifier of the token that replaced this one after rotation, or
    /// <see langword="null"/> when no replacement exists.
    /// </summary>
    public RefreshTokenId? ReplacedByTokenId { get; private set; }

    /// <summary>
    /// Gets the reason the token was revoked, or <see langword="null"/> while the token is active.
    /// </summary>
    public TokenRevocationReason? RevocationReason { get; private set; }

    /// <summary>
    /// Creates a new refresh-token record for a session.
    /// </summary>
    /// <param name="userId">The account the token belongs to.</param>
    /// <param name="familyId">The session family identifier; a new session uses a fresh id.</param>
    /// <param name="tokenHash">The hash of the raw token secret.</param>
    /// <param name="lifetime">The token lifetime; the expiration is computed from it.</param>
    /// <param name="utcNow">The current instant, in UTC.</param>
    /// <returns>A new active <see cref="RefreshToken"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="userId"/> or <paramref name="tokenHash"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="tokenHash"/> is empty or exceeds
    /// <see cref="HashLength"/> characters.</exception>
    public static RefreshToken Create(
        UserId userId,
        Guid familyId,
        string tokenHash,
        TimeSpan lifetime,
        DateTime utcNow)
    {
        ArgumentNullException.ThrowIfNull(userId);
        ArgumentNullException.ThrowIfNull(tokenHash);

        if (string.IsNullOrWhiteSpace(tokenHash))
        {
            throw new ArgumentException("A token hash is required.", nameof(tokenHash));
        }

        if (tokenHash.Length > HashLength)
        {
            throw new ArgumentException($"A token hash cannot exceed {HashLength} characters.", nameof(tokenHash));
        }

        return new RefreshToken(
            RefreshTokenId.New(),
            userId,
            familyId,
            tokenHash,
            utcNow,
            utcNow.Add(lifetime));
    }

    /// <summary>
    /// Determines whether the token is currently usable.
    /// </summary>
    /// <param name="utcNow">The current instant, in UTC.</param>
    /// <returns><see langword="true"/> when the token is not revoked and not expired.</returns>
    public bool IsActive(DateTime utcNow) => RevokedAtUtc is null && ExpiresAtUtc > utcNow;

    /// <summary>
    /// Revokes the token, recording the reason and optionally the replacing token. Revocation
    /// is permanent; the caller is responsible for only revoking tokens that are currently
    /// active (the domain does not silently ignore a second revocation).
    /// </summary>
    /// <param name="reason">The reason for the revocation.</param>
    /// <param name="replacedByTokenId">The id of the replacing token, when this revocation is a rotation.</param>
    /// <param name="utcNow">The current instant, in UTC.</param>
    public void Revoke(TokenRevocationReason reason, RefreshTokenId? replacedByTokenId, DateTime utcNow)
    {
        RevokedAtUtc = utcNow;
        RevocationReason = reason;
        ReplacedByTokenId = replacedByTokenId;
    }
}
