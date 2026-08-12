using LastHour.BuildingBlocks.Application.Contracts;
using LastHour.Modules.Identity.Domain.Users;

namespace LastHour.Modules.Identity.Domain.RefreshTokens;

/// <summary>
/// The persistence contract of the <see cref="RefreshToken"/> records. It extends the shared
/// write repository with the security lookups the refresh and logout flows need: resolution
/// by stored hash (the only identifier a client ever presents) and the active members of a
/// token family (used by reuse detection to revoke an entire session).
/// </summary>
public interface IRefreshTokenRepository : IRepository<RefreshToken>
{
    /// <summary>
    /// Retrieves the token record with the specified hash, or <see langword="null"/> when none
    /// exists. Lookups always run against the stored hash of the submitted secret, never the
    /// raw value.
    /// </summary>
    /// <param name="tokenHash">The hash of the submitted token secret.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>The matching token record, or <see langword="null"/> when not found.</returns>
    Task<RefreshToken?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all active (not revoked) token records of a family.
    /// </summary>
    /// <param name="familyId">The family identifier to query.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>The active token records of the family.</returns>
    Task<IReadOnlyList<RefreshToken>> GetActiveByFamilyIdAsync(Guid familyId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all active (not revoked) token records of a user. Used by logout-all and
    /// password-change invalidation, which revoke every outstanding session of an account, and
    /// by the session-inspection query.
    /// </summary>
    /// <param name="userId">The user whose sessions are queried.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>The active token records of the user.</returns>
    Task<IReadOnlyList<RefreshToken>> GetActiveByUserIdAsync(UserId userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves every token record of a user, active and revoked, for the session-inspection
    /// query. Only safe metadata is exposed from the results — never the stored hashes.
    /// </summary>
    /// <param name="userId">The user whose sessions are queried.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>Every token record of the user.</returns>
    Task<IReadOnlyList<RefreshToken>> GetAllByUserIdAsync(UserId userId, CancellationToken cancellationToken = default);
}
