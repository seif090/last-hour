using LastHour.BuildingBlocks.Application.Contracts;
using LastHour.BuildingBlocks.Application.Cqrs;
using LastHour.BuildingBlocks.SharedKernel.Results;
using LastHour.Modules.Identity.Domain.RefreshTokens;
using LastHour.Modules.Identity.Domain.Users;
using Microsoft.Extensions.Logging;

namespace LastHour.Modules.Identity.Application.Commands;

/// <summary>
/// Ends every session of the authenticated user by revoking all of their active refresh tokens
/// with the <c>AllSessionsRevoked</c> reason. The operation is scoped strictly to the
/// authenticated user — the identifier comes from the validated token claims, never from the
/// request — so no other account can be affected. It is idempotent: an account with no active
/// sessions still succeeds. Already-issued access tokens remain valid until they expire.
/// </summary>
public sealed class LogoutAllCommandHandler : ICommandHandler<LogoutAllCommand>
{
    private static readonly Action<ILogger, Guid, int, Exception?> AllSessionsEnded =
        LoggerMessage.Define<Guid, int>(
            LogLevel.Information,
            new EventId(2010, nameof(LogoutAllCommandHandler)),
            "All {SessionCount} sessions ended for user {UserId}.");

    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IClock _clock;
    private readonly ILogger<LogoutAllCommandHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="LogoutAllCommandHandler"/> class.
    /// </summary>
    /// <param name="refreshTokenRepository">The repository of the sessions to revoke.</param>
    /// <param name="clock">The clock used to timestamp the revocations.</param>
    /// <param name="logger">The logger used to record the security event without sensitive values.</param>
    /// <exception cref="ArgumentNullException">Any argument is <see langword="null"/>.</exception>
    public LogoutAllCommandHandler(
        IRefreshTokenRepository refreshTokenRepository,
        IClock clock,
        ILogger<LogoutAllCommandHandler> logger)
    {
        ArgumentNullException.ThrowIfNull(refreshTokenRepository);
        ArgumentNullException.ThrowIfNull(clock);
        ArgumentNullException.ThrowIfNull(logger);
        _refreshTokenRepository = refreshTokenRepository;
        _clock = clock;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<Result> Handle(LogoutAllCommand command, CancellationToken cancellationToken)
    {
        UserId userId = UserId.Create(command.UserId);
        IReadOnlyList<RefreshToken> sessions = await _refreshTokenRepository
            .GetActiveByUserIdAsync(userId, cancellationToken)
            .ConfigureAwait(false);

        DateTime utcNow = _clock.UtcNow;
        foreach (RefreshToken session in sessions)
        {
            session.Revoke(TokenRevocationReason.AllSessionsRevoked, null, utcNow);
            _refreshTokenRepository.Update(session);
        }

        AllSessionsEnded(_logger, command.UserId, sessions.Count, null);
        return Result.Success();
    }
}
