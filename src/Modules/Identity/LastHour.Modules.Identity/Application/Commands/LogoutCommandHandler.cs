using LastHour.BuildingBlocks.Application.Contracts;
using LastHour.BuildingBlocks.Application.Cqrs;
using LastHour.BuildingBlocks.SharedKernel.Results;
using LastHour.Modules.Identity.Application.RefreshTokens;
using LastHour.Modules.Identity.Domain.RefreshTokens;
using Microsoft.Extensions.Logging;

namespace LastHour.Modules.Identity.Application.Commands;

/// <summary>
/// Ends a session by revoking its refresh credential with the logout reason. The operation is
/// deliberately idempotent and never fails for unknown or already-revoked tokens, so logout
/// cannot be used to probe whether a refresh token was ever valid. Only the session's refresh
/// credential is revoked — outstanding access tokens remain valid until they expire.
/// </summary>
public sealed class LogoutCommandHandler : ICommandHandler<LogoutCommand>
{
    private static readonly Action<ILogger, Guid, Exception?> SessionEnded =
        LoggerMessage.Define<Guid>(
            LogLevel.Information,
            new EventId(2006, nameof(LogoutCommandHandler)),
            "Session ended for user {UserId}.");

    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IRefreshTokenService _refreshTokens;
    private readonly IClock _clock;
    private readonly ILogger<LogoutCommandHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="LogoutCommandHandler"/> class.
    /// </summary>
    /// <param name="refreshTokenRepository">The repository holding the token records.</param>
    /// <param name="refreshTokens">The refresh-token service used to hash the submitted token.</param>
    /// <param name="clock">The clock used to timestamp the revocation.</param>
    /// <param name="logger">The logger used to record the session end without sensitive values.</param>
    /// <exception cref="ArgumentNullException">Any argument is <see langword="null"/>.</exception>
    public LogoutCommandHandler(
        IRefreshTokenRepository refreshTokenRepository,
        IRefreshTokenService refreshTokens,
        IClock clock,
        ILogger<LogoutCommandHandler> logger)
    {
        ArgumentNullException.ThrowIfNull(refreshTokenRepository);
        ArgumentNullException.ThrowIfNull(refreshTokens);
        ArgumentNullException.ThrowIfNull(clock);
        ArgumentNullException.ThrowIfNull(logger);
        _refreshTokenRepository = refreshTokenRepository;
        _refreshTokens = refreshTokens;
        _clock = clock;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<Result> Handle(LogoutCommand command, CancellationToken cancellationToken)
    {
        string tokenHash = _refreshTokens.Hash(command.RefreshToken);
        RefreshToken? token = await _refreshTokenRepository
            .GetByTokenHashAsync(tokenHash, cancellationToken)
            .ConfigureAwait(false);

        if (token is null || token.RevokedAtUtc is not null)
        {
            // Nothing to revoke: the token never existed or was already consumed. Succeed
            // silently so logout stays idempotent and leak-free.
            return Result.Success();
        }

        token.Revoke(TokenRevocationReason.Logout, null, _clock.UtcNow);
        _refreshTokenRepository.Update(token);
        SessionEnded(_logger, token.UserId.Value, null);

        return Result.Success();
    }
}
