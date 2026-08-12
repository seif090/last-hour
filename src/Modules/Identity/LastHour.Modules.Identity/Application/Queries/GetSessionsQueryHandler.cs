using LastHour.BuildingBlocks.Application.Contracts;
using LastHour.BuildingBlocks.Application.Cqrs;
using LastHour.BuildingBlocks.SharedKernel.Results;
using LastHour.Modules.Identity.Domain.RefreshTokens;
using LastHour.Modules.Identity.Domain.Users;

namespace LastHour.Modules.Identity.Application.Queries;

/// <summary>
/// Resolves the session-inspection query: every refresh-token record of the authenticated user
/// is projected onto safe <see cref="SessionInfo"/> metadata (id, creation, expiration, active
/// state). The projection is deliberately lossy — the stored token hashes never leave the
/// persistence boundary.
/// </summary>
public sealed class GetSessionsQueryHandler : IQueryHandler<GetSessionsQuery, IReadOnlyList<SessionInfo>>
{
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IClock _clock;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetSessionsQueryHandler"/> class.
    /// </summary>
    /// <param name="refreshTokenRepository">The repository of the session records.</param>
    /// <param name="clock">The clock used to determine which sessions are still active.</param>
    /// <exception cref="ArgumentNullException">Any argument is <see langword="null"/>.</exception>
    public GetSessionsQueryHandler(
        IRefreshTokenRepository refreshTokenRepository,
        IClock clock)
    {
        ArgumentNullException.ThrowIfNull(refreshTokenRepository);
        ArgumentNullException.ThrowIfNull(clock);
        _refreshTokenRepository = refreshTokenRepository;
        _clock = clock;
    }

    /// <inheritdoc/>
    public async Task<Result<IReadOnlyList<SessionInfo>>> Handle(
        GetSessionsQuery query,
        CancellationToken cancellationToken)
    {
        UserId userId = UserId.Create(query.UserId);
        IReadOnlyList<RefreshToken> sessions = await _refreshTokenRepository
            .GetAllByUserIdAsync(userId, cancellationToken)
            .ConfigureAwait(false);

        DateTime utcNow = _clock.UtcNow;
        SessionInfo[] result = sessions
            .OrderByDescending(session => session.CreatedAtUtc)
            .Select(session => new SessionInfo(
                session.Id.Value,
                session.CreatedAtUtc,
                session.ExpiresAtUtc,
                session.IsActive(utcNow)))
            .ToArray();

        return Result<IReadOnlyList<SessionInfo>>.Success(result);
    }
}
