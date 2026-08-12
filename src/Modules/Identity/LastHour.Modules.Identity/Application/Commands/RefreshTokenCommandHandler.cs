using LastHour.BuildingBlocks.Application.Contracts;
using LastHour.BuildingBlocks.Application.Cqrs;
using LastHour.BuildingBlocks.SharedKernel.Results;
using LastHour.Modules.Identity.Application.RefreshTokens;
using LastHour.Modules.Identity.Domain.RefreshTokens;
using LastHour.Modules.Identity.Domain.Users;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LastHour.Modules.Identity.Application.Commands;

/// <summary>
/// Validates and rotates a refresh-token credential. Every failure — unknown, revoked, expired,
/// reused or account-unavailable — surfaces the same public error, so the endpoint cannot be
/// used to probe the state of a token. A rotated token presented again is treated as possible
/// compromise: the whole token family is revoked and a security event is logged. The rotation
/// itself is atomic (revoke old, insert successor in one transaction) and concurrency-safe: two
/// simultaneous refreshes with the same token cannot both succeed, because the old record's
/// optimistic concurrency token lets only one commit land. No raw token, hash or access token is
/// ever logged.
/// </summary>
public sealed class RefreshTokenCommandHandler : ICommandHandler<RefreshTokenCommand, RefreshTokenResult>
{
    /// <summary>
    /// The single public failure returned for every rejected refresh attempt. Sharing one error
    /// instance makes it structurally impossible for a caller to distinguish the cause.
    /// </summary>
    private static readonly Error InvalidRefreshTokenError = Error.Unauthorized(
        "InvalidRefreshToken",
        "The refresh token is invalid or has expired.");

    private static readonly Action<ILogger, Guid, Exception?> RefreshSucceeded =
        LoggerMessage.Define<Guid>(
            LogLevel.Information,
            new EventId(2003, nameof(RefreshTokenCommandHandler)),
            "Refresh token rotated for user {UserId}.");

    private static readonly Action<ILogger, string, Exception?> RefreshRejected =
        LoggerMessage.Define<string>(
            LogLevel.Warning,
            new EventId(2004, nameof(RefreshTokenCommandHandler)),
            "Refresh rejected: {Reason}.");

    private static readonly Action<ILogger, Guid, Exception?> ReuseDetected =
        LoggerMessage.Define<Guid>(
            LogLevel.Warning,
            new EventId(2005, nameof(RefreshTokenCommandHandler)),
            "Refresh-token reuse detected for user {UserId}; the token family has been revoked.");

    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IRefreshTokenService _refreshTokens;
    private readonly IUserRepository _users;
    private readonly IAccessTokenGenerator _accessTokens;
    private readonly IOptions<RefreshTokenOptions> _refreshTokenOptions;
    private readonly IClock _clock;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<RefreshTokenCommandHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="RefreshTokenCommandHandler"/> class.
    /// </summary>
    /// <param name="refreshTokenRepository">The repository holding the token records.</param>
    /// <param name="refreshTokens">The refresh-token service used to hash the submitted token and generate its successor.</param>
    /// <param name="users">The user repository used to re-check the account state.</param>
    /// <param name="accessTokens">The access-token generator that issues the new access token.</param>
    /// <param name="refreshTokenOptions">The configured refresh-token lifetime.</param>
    /// <param name="clock">The clock used to timestamp revocations and expirations.</param>
    /// <param name="unitOfWork">The persistence boundary. The reuse path commits the family
    /// revocation explicitly, because that security response must persist even though the
    /// command itself fails (the pipeline only commits successful commands).</param>
    /// <param name="logger">The logger used to record security events without sensitive values.</param>
    /// <exception cref="ArgumentNullException">Any argument is <see langword="null"/>.</exception>
    public RefreshTokenCommandHandler(
        IRefreshTokenRepository refreshTokenRepository,
        IRefreshTokenService refreshTokens,
        IUserRepository users,
        IAccessTokenGenerator accessTokens,
        IOptions<RefreshTokenOptions> refreshTokenOptions,
        IClock clock,
        IUnitOfWork unitOfWork,
        ILogger<RefreshTokenCommandHandler> logger)
    {
        ArgumentNullException.ThrowIfNull(refreshTokenRepository);
        ArgumentNullException.ThrowIfNull(refreshTokens);
        ArgumentNullException.ThrowIfNull(users);
        ArgumentNullException.ThrowIfNull(accessTokens);
        ArgumentNullException.ThrowIfNull(refreshTokenOptions);
        ArgumentNullException.ThrowIfNull(clock);
        ArgumentNullException.ThrowIfNull(unitOfWork);
        ArgumentNullException.ThrowIfNull(logger);
        _refreshTokenRepository = refreshTokenRepository;
        _refreshTokens = refreshTokens;
        _users = users;
        _accessTokens = accessTokens;
        _refreshTokenOptions = refreshTokenOptions;
        _clock = clock;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<Result<RefreshTokenResult>> Handle(
        RefreshTokenCommand command,
        CancellationToken cancellationToken)
    {
        string tokenHash = _refreshTokens.Hash(command.RefreshToken);
        RefreshToken? token = await _refreshTokenRepository
            .GetByTokenHashAsync(tokenHash, cancellationToken)
            .ConfigureAwait(false);

        if (token is null)
        {
            return Reject("UnknownToken");
        }

        if (token.RevokedAtUtc is not null)
        {
            // A rotated token presented again is the classic compromise signal: the original
            // secret escaped, so the entire session is revoked.
            if (token.RevocationReason == TokenRevocationReason.Rotation)
            {
                await RevokeFamilyAsync(token.FamilyId, cancellationToken).ConfigureAwait(false);

                // The family revocation is the complete security response to a compromised
                // credential and must persist even though this command fails, so it is
                // committed explicitly here (the pipeline only commits successful commands).
                await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                ReuseDetected(_logger, token.UserId.Value, null);
                return Reject("ReuseDetected");
            }

            return Reject("RevokedToken");
        }

        DateTime utcNow = _clock.UtcNow;
        if (token.ExpiresAtUtc <= utcNow)
        {
            return Reject("ExpiredToken");
        }

        // The session outlives a single login, so the account must still be able to
        // authenticate before it is allowed to refresh.
        User? user = await _users.GetByIdAsync(token.UserId, cancellationToken).ConfigureAwait(false);
        if (user is null || user.Status != AccountStatus.Active)
        {
            token.Revoke(TokenRevocationReason.SecurityEvent, null, utcNow);
            _refreshTokenRepository.Update(token);
            return Reject("AccountUnavailable");
        }

        GeneratedAccessToken accessToken = _accessTokens.Generate(user.Id.Value);
        RefreshTokenSecret refreshSecret = _refreshTokens.Generate();
        RefreshToken successor = RefreshToken.Create(
            user.Id,
            token.FamilyId,
            refreshSecret.TokenHash,
            _refreshTokenOptions.Value.Lifetime,
            utcNow);

        token.Revoke(TokenRevocationReason.Rotation, successor.Id, utcNow);
        _refreshTokenRepository.Add(successor);
        _refreshTokenRepository.Update(token);

        RefreshSucceeded(_logger, user.Id.Value, null);
        return Result<RefreshTokenResult>.Success(
            new RefreshTokenResult(accessToken.Token, accessToken.ExpiresAtUtc, refreshSecret.RawToken));
    }

    private async Task RevokeFamilyAsync(Guid familyId, CancellationToken cancellationToken)
    {
        IReadOnlyList<RefreshToken> activeMembers = await _refreshTokenRepository
            .GetActiveByFamilyIdAsync(familyId, cancellationToken)
            .ConfigureAwait(false);

        DateTime utcNow = _clock.UtcNow;
        foreach (RefreshToken member in activeMembers)
        {
            member.Revoke(TokenRevocationReason.ReuseDetected, null, utcNow);
            _refreshTokenRepository.Update(member);
        }
    }

    private Result<RefreshTokenResult> Reject(string reason)
    {
        RefreshRejected(_logger, reason, null);
        return Result<RefreshTokenResult>.Failure(InvalidRefreshTokenError);
    }
}
