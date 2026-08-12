using LastHour.BuildingBlocks.Application.Contracts;
using LastHour.BuildingBlocks.Application.Cqrs;
using LastHour.BuildingBlocks.SharedKernel.Results;
using LastHour.Modules.Identity.Domain.RefreshTokens;
using LastHour.Modules.Identity.Domain.Users;
using Microsoft.Extensions.Logging;

namespace LastHour.Modules.Identity.Application.Commands;

/// <summary>
/// Changes the authenticated user's password and invalidates every outstanding session. The
/// current password is verified, the new password is validated and hashed by the credential
/// service, the aggregate records the change (timestamp, throttle reset) and every active
/// refresh token of the account is revoked with the <c>PasswordChanged</c> reason. Because JWT
/// access tokens are stateless, already-issued access tokens remain valid until they expire;
/// the client must re-authenticate to obtain new tokens. All mutations commit atomically with
/// the command. No password, hash or token value is ever logged or returned.
/// </summary>
public sealed class ChangePasswordCommandHandler : ICommandHandler<ChangePasswordCommand>
{
    private static readonly Action<ILogger, Guid, Exception?> PasswordChanged =
        LoggerMessage.Define<Guid>(
            LogLevel.Information,
            new EventId(2009, nameof(ChangePasswordCommandHandler)),
            "Password changed for user {UserId}; all sessions revoked.");

    private readonly IUserRepository _users;
    private readonly IPasswordCredentialService _credentials;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IClock _clock;
    private readonly ILogger<ChangePasswordCommandHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ChangePasswordCommandHandler"/> class.
    /// </summary>
    /// <param name="users">The user repository used to load the account for mutation.</param>
    /// <param name="credentials">The credential service that verifies, validates and hashes the passwords.</param>
    /// <param name="refreshTokenRepository">The repository of the sessions to revoke.</param>
    /// <param name="clock">The clock used to timestamp the change and the revocations.</param>
    /// <param name="logger">The logger used to record the security event without sensitive values.</param>
    /// <exception cref="ArgumentNullException">Any argument is <see langword="null"/>.</exception>
    public ChangePasswordCommandHandler(
        IUserRepository users,
        IPasswordCredentialService credentials,
        IRefreshTokenRepository refreshTokenRepository,
        IClock clock,
        ILogger<ChangePasswordCommandHandler> logger)
    {
        ArgumentNullException.ThrowIfNull(users);
        ArgumentNullException.ThrowIfNull(credentials);
        ArgumentNullException.ThrowIfNull(refreshTokenRepository);
        ArgumentNullException.ThrowIfNull(clock);
        ArgumentNullException.ThrowIfNull(logger);
        _users = users;
        _credentials = credentials;
        _refreshTokenRepository = refreshTokenRepository;
        _clock = clock;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<Result> Handle(ChangePasswordCommand command, CancellationToken cancellationToken)
    {
        User? user = await _users
            .GetByIdTrackedAsync(UserId.Create(command.UserId), cancellationToken)
            .ConfigureAwait(false);

        if (user is null)
        {
            // The authenticated subject no longer exists; the credential cannot be changed and
            // the caller must re-authenticate.
            return Result.Failure(Error.Unauthorized(
                "UserNotFound",
                "The account could not be found."));
        }

        Result changeResult = _credentials.ChangePassword(user, command.CurrentPassword, command.NewPassword);
        if (changeResult.IsFailure)
        {
            return changeResult;
        }

        DateTime utcNow = _clock.UtcNow;
        user.MarkPasswordChanged(utcNow);

        IReadOnlyList<RefreshToken> sessions = await _refreshTokenRepository
            .GetActiveByUserIdAsync(user.Id, cancellationToken)
            .ConfigureAwait(false);

        foreach (RefreshToken session in sessions)
        {
            session.Revoke(TokenRevocationReason.PasswordChanged, null, utcNow);
            _refreshTokenRepository.Update(session);
        }

        PasswordChanged(_logger, user.Id.Value, null);
        return Result.Success();
    }
}
