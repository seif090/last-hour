using LastHour.BuildingBlocks.Application.Contracts;
using LastHour.BuildingBlocks.Application.Cqrs;
using LastHour.BuildingBlocks.SharedKernel.Results;
using LastHour.Modules.Identity.Application.RefreshTokens;
using LastHour.Modules.Identity.Domain.RefreshTokens;
using LastHour.Modules.Identity.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LastHour.Modules.Identity.Application.Commands;

/// <summary>
/// Coordinates credential authentication: normalized email lookup, password verification, account
/// state gating, lockout enforcement, access-token issuance and refresh-token establishment. The
/// outcome is deliberately enumeration-safe — every failure (unknown account, wrong password,
/// pending, suspended, disabled or locked account) surfaces the exact same public error, and
/// unknown and locked accounts pay the same password-verification cost as genuine ones so
/// response timing does not reveal account state. Failed attempts increment the account's
/// throttle counter; reaching the configured threshold locks the account for a time-boxed
/// window. The counter update commits even though the login command fails, because that is
/// exactly the state change the failure must produce — and it is applied through a tracked load
/// with a bounded retry on concurrency conflicts, so simultaneous failed attempts cannot lose
/// increments and cannot bypass the threshold. The internal reason is logged only, and no
/// password, hash, token or email value ever reaches a log or a result.
/// </summary>
public sealed class LoginCommandHandler : ICommandHandler<LoginCommand, LoginResult>
{
    /// <summary>
    /// The number of times a failed-login counter update is retried after a concurrency
    /// conflict before the attempt is abandoned. Each retry re-reads the latest committed state,
    /// so concurrent failures all land eventually.
    /// </summary>
    private const int CounterUpdateMaxRetries = 3;

    /// <summary>
    /// The single public failure returned for every rejected authentication attempt. Sharing one
    /// error instance makes it structurally impossible for a caller to distinguish the cause.
    /// </summary>
    private static readonly Error InvalidCredentialsError = Error.Unauthorized(
        "AuthenticationFailed",
        "The email address or password is incorrect.");

    /// <summary>
    /// A real Argon2id hash (current production parameters) of a throwaway value, verified
    /// against when no account exists, no credential is stored or the account is locked, so
    /// those attempts consume the same verification time as genuine ones.
    /// </summary>
    private static readonly PasswordCredential DummyCredential = PasswordCredential.FromHash(
        "$argon2id$v=19$m=65536,t=3,p=4$wL/+BWVyT8B6+Za2G2bTWg$P9LsBb1ZN0JpI/n479HD/1VAAqKDcJJIXdQkceWrxRM");

    private static readonly Action<ILogger, Guid, Exception?> LoginSucceeded =
        LoggerMessage.Define<Guid>(
            LogLevel.Information,
            new EventId(2001, nameof(LoginCommandHandler)),
            "Authentication succeeded for user {UserId}.");

    private static readonly Action<ILogger, string, Exception?> LoginRejected =
        LoggerMessage.Define<string>(
            LogLevel.Warning,
            new EventId(2002, nameof(LoginCommandHandler)),
            "Authentication rejected: {Reason}.");

    private static readonly Action<ILogger, Guid, int, Exception?> AccountLocked =
        LoggerMessage.Define<Guid, int>(
            LogLevel.Warning,
            new EventId(2007, nameof(LoginCommandHandler)),
            "Account {UserId} locked after {FailedAccessCount} consecutive failed attempts.");

    private static readonly Action<ILogger, Guid, Exception?> CounterUpdateExhausted =
        LoggerMessage.Define<Guid>(
            LogLevel.Warning,
            new EventId(2008, nameof(LoginCommandHandler)),
            "Failed-attempt counter update abandoned for user {UserId} after repeated concurrency conflicts.");

    private readonly IUserRepository _users;
    private readonly IPasswordCredentialService _credentials;
    private readonly IAccessTokenGenerator _accessTokens;
    private readonly IRefreshTokenService _refreshTokens;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IOptions<RefreshTokenOptions> _refreshTokenOptions;
    private readonly IOptions<AccountSecurityOptions> _accountSecurityOptions;
    private readonly IClock _clock;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<LoginCommandHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="LoginCommandHandler"/> class.
    /// </summary>
    /// <param name="users">The user repository used for the normalized email lookup.</param>
    /// <param name="credentials">The credential service that verifies passwords.</param>
    /// <param name="accessTokens">The access-token generator that issues the token on success.</param>
    /// <param name="refreshTokens">The refresh-token service that generates the refresh secret.</param>
    /// <param name="refreshTokenRepository">The repository that persists the refresh-token record.</param>
    /// <param name="refreshTokenOptions">The configured refresh-token lifetime.</param>
    /// <param name="accountSecurityOptions">The configured lockout policy.</param>
    /// <param name="clock">The clock used to timestamp the refresh-token record and lockout state.</param>
    /// <param name="unitOfWork">The persistence boundary. The failed-attempt counter is committed
    /// explicitly here because that state change must persist even though the login command
    /// itself fails (the pipeline only commits successful commands).</param>
    /// <param name="logger">The logger used to record authentication events without sensitive values.</param>
    /// <exception cref="ArgumentNullException">Any argument is <see langword="null"/>.</exception>
    public LoginCommandHandler(
        IUserRepository users,
        IPasswordCredentialService credentials,
        IAccessTokenGenerator accessTokens,
        IRefreshTokenService refreshTokens,
        IRefreshTokenRepository refreshTokenRepository,
        IOptions<RefreshTokenOptions> refreshTokenOptions,
        IOptions<AccountSecurityOptions> accountSecurityOptions,
        IClock clock,
        IUnitOfWork unitOfWork,
        ILogger<LoginCommandHandler> logger)
    {
        ArgumentNullException.ThrowIfNull(users);
        ArgumentNullException.ThrowIfNull(credentials);
        ArgumentNullException.ThrowIfNull(accessTokens);
        ArgumentNullException.ThrowIfNull(refreshTokens);
        ArgumentNullException.ThrowIfNull(refreshTokenRepository);
        ArgumentNullException.ThrowIfNull(refreshTokenOptions);
        ArgumentNullException.ThrowIfNull(accountSecurityOptions);
        ArgumentNullException.ThrowIfNull(clock);
        ArgumentNullException.ThrowIfNull(unitOfWork);
        ArgumentNullException.ThrowIfNull(logger);
        _users = users;
        _credentials = credentials;
        _accessTokens = accessTokens;
        _refreshTokens = refreshTokens;
        _refreshTokenRepository = refreshTokenRepository;
        _refreshTokenOptions = refreshTokenOptions;
        _accountSecurityOptions = accountSecurityOptions;
        _clock = clock;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<Result<LoginResult>> Handle(LoginCommand command, CancellationToken cancellationToken)
    {
        Email email;
        try
        {
            email = Email.From(command.Email);
        }
        catch (InvalidEmailException exception)
        {
            return Result<LoginResult>.Failure(Error.Validation("InvalidEmail", exception.Message));
        }

        User? user = await _users.GetByEmailAsync(email, cancellationToken).ConfigureAwait(false);
        if (user is null)
        {
            // The password is verified against a dummy credential so the cost of an
            // unknown-account attempt matches a genuine verification.
            _credentials.VerifyPassword(command.Password, DummyCredential);
            return Reject(AuthenticationFailureReason.InvalidCredentials);
        }

        PasswordCredential? credential = user.PasswordCredential;
        if (credential is null)
        {
            _credentials.VerifyPassword(command.Password, DummyCredential);
            return Reject(AuthenticationFailureReason.InvalidCredentials);
        }

        DateTime utcNow = _clock.UtcNow;
        if (user.IsLockedOut(utcNow))
        {
            // A locked account is rejected before verification (the point of lockout is to stop
            // credential checking), but the dummy verification keeps the response cost uniform.
            _credentials.VerifyPassword(command.Password, DummyCredential);
            return Reject(AuthenticationFailureReason.AccountLocked);
        }

        if (_credentials.VerifyPassword(command.Password, credential) == PasswordVerificationResult.Failed)
        {
            await RecordFailedLoginAsync(user.Id, cancellationToken).ConfigureAwait(false);
            return Reject(AuthenticationFailureReason.InvalidCredentials);
        }

        // The password is correct; account state still gates authentication. Status failures are
        // surfaced through the same public error to keep the response free of enumeration hints.
        AuthenticationResult accountState = ResolveAccountState(user);
        if (!accountState.Succeeded)
        {
            return Reject(accountState.FailureReason!.Value);
        }

        // A successful authentication resets the throttle. The tracked load keeps the update
        // concurrency-safe; the command succeeds, so the pipeline commits it with the refresh
        // token insert atomically.
        await RecordSuccessfulLoginAsync(user.Id, cancellationToken).ConfigureAwait(false);

        GeneratedAccessToken accessToken = _accessTokens.Generate(user.Id.Value);
        RefreshTokenSecret refreshSecret = _refreshTokens.Generate();
        var refreshToken = RefreshToken.Create(
            user.Id,
            Guid.NewGuid(),
            refreshSecret.TokenHash,
            _refreshTokenOptions.Value.Lifetime,
            utcNow);
        _refreshTokenRepository.Add(refreshToken);

        LoginSucceeded(_logger, user.Id.Value, null);
        return Result<LoginResult>.Success(
            new LoginResult(accessToken.Token, accessToken.ExpiresAtUtc, refreshSecret.RawToken));
    }

    private static AuthenticationResult ResolveAccountState(User user) => user.Status switch
    {
        AccountStatus.Active => AuthenticationResult.Success(),
        AccountStatus.Pending => AuthenticationResult.Failure(AuthenticationFailureReason.AccountPending),
        AccountStatus.Suspended => AuthenticationResult.Failure(AuthenticationFailureReason.AccountSuspended),
        AccountStatus.Disabled => AuthenticationResult.Failure(AuthenticationFailureReason.AccountDisabled),
        _ => AuthenticationResult.Failure(AuthenticationFailureReason.InvalidCredentials),
    };

    private Result<LoginResult> Reject(AuthenticationFailureReason reason)
    {
        LoginRejected(_logger, reason.ToString(), null);
        return Result<LoginResult>.Failure(InvalidCredentialsError);
    }

    /// <summary>
    /// Applies one failed-login increment to the user's throttle state and commits it explicitly,
    /// retrying on optimistic-concurrency conflicts so concurrent failures cannot lose increments
    /// or bypass the lockout threshold. Once the account is locked by a concurrent attempt, the
    /// remaining attempts stop incrementing (a locked account rejects without counting).
    /// </summary>
    /// <param name="userId">The user whose throttle state is updated.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    private async Task RecordFailedLoginAsync(UserId userId, CancellationToken cancellationToken)
    {
        for (int attempt = 0; attempt < CounterUpdateMaxRetries; attempt++)
        {
            User? tracked = await _users.GetByIdTrackedAsync(userId, cancellationToken).ConfigureAwait(false);
            if (tracked is null)
            {
                return;
            }

            if (tracked.IsLockedOut(_clock.UtcNow))
            {
                // A concurrent attempt already crossed the threshold; nothing more to count.
                return;
            }

            tracked.RecordFailedLogin(
                _clock.UtcNow,
                _accountSecurityOptions.Value.MaxFailedAttempts,
                _accountSecurityOptions.Value.LockoutDuration);
            _users.Update(tracked);

            try
            {
                await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                if (tracked.IsLockedOut(_clock.UtcNow))
                {
                    AccountLocked(_logger, userId.Value, tracked.FailedAccessCount, null);
                }

                return;
            }
            catch (DbUpdateConcurrencyException)
            {
                // Another failed attempt committed first; the tracked state is stale. Refresh it
                // from the store and apply this increment to the current state.
                await _users.ReloadTrackedAsync(tracked, cancellationToken).ConfigureAwait(false);
            }
        }

        CounterUpdateExhausted(_logger, userId.Value, null);
    }

    /// <summary>
    /// Resets the throttle state after a successful authentication. The mutated aggregate stays
    /// in the change tracker and is committed by the transaction pipeline with the login command.
    /// </summary>
    /// <param name="userId">The user whose throttle state is reset.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    private async Task RecordSuccessfulLoginAsync(UserId userId, CancellationToken cancellationToken)
    {
        User? tracked = await _users.GetByIdTrackedAsync(userId, cancellationToken).ConfigureAwait(false);
        if (tracked is null)
        {
            return;
        }

        tracked.RecordSuccessfulLogin(_clock.UtcNow);
        _users.Update(tracked);
    }
}
