using FluentAssertions;
using LastHour.BuildingBlocks.Application.Contracts;
using LastHour.BuildingBlocks.SharedKernel.Results;
using LastHour.Modules.Identity.Application;
using LastHour.Modules.Identity.Application.Commands;
using LastHour.Modules.Identity.Application.RefreshTokens;
using LastHour.Modules.Identity.Domain.RefreshTokens;
using LastHour.Modules.Identity.Domain.Users;
using LastHour.Modules.Identity.Tests.Application.TestSupport;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace LastHour.Modules.Identity.Tests.Application.Commands;

public class LoginCommandHandlerTests
{
    private const string Password = "correct horse battery staple";

    private static readonly DateTime FixedNow = new DateTime(2026, 8, 12, 10, 0, 0, DateTimeKind.Utc);

    private static readonly AccountSecurityOptions DefaultAccountSecurityOptions = new AccountSecurityOptions();

    private static LoginCommandHandler CreateHandler(
        FakeUserRepository users,
        FakeRefreshTokenRepository? refreshTokens = null,
        CountingPasswordHasher? hasher = null,
        AccountSecurityOptions? accountSecurityOptions = null,
        FakeUnitOfWork? unitOfWork = null,
        FixedClock? clock = null)
    {
        CountingPasswordHasher countingHasher = hasher ?? new CountingPasswordHasher();
        var credentials = new PasswordCredentialService(
            countingHasher,
            Options.Create(new PasswordPolicy()));

        return new LoginCommandHandler(
            users,
            credentials,
            new StubAccessTokenGenerator(),
            new StubRefreshTokenService(),
            refreshTokens ?? new FakeRefreshTokenRepository(),
            Options.Create(new RefreshTokenOptions()),
            Options.Create(accountSecurityOptions ?? DefaultAccountSecurityOptions),
            clock ?? new FixedClock(FixedNow),
            unitOfWork ?? new FakeUnitOfWork(),
            NullLogger<LoginCommandHandler>.Instance);
    }

    [Fact]
    public async Task Handle_WrongPassword_IncrementsCounterAndCommitsDespiteFailure()
    {
        var users = new FakeUserRepository();
        users.AddActiveUser("user@example.com", Password);
        var unitOfWork = new FakeUnitOfWork();

        Result<LoginResult> result = await CreateHandler(users, unitOfWork: unitOfWork).Handle(
            new LoginCommand("user@example.com", "wrong-password-value"),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        users.FindByEmail("user@example.com").FailedAccessCount.Should().Be(1);
        unitOfWork.SaveChangesCount.Should().Be(1, "the counter update commits even though login fails");
    }

    [Fact]
    public async Task Handle_ReachingThreshold_LocksAccount()
    {
        var users = new FakeUserRepository();
        users.AddActiveUser("user@example.com", Password);

        var handler = CreateHandler(users);
        for (int attempt = 0; attempt < DefaultAccountSecurityOptions.MaxFailedAttempts; attempt++)
        {
            await handler.Handle(
                new LoginCommand("user@example.com", "wrong-password-value"),
                CancellationToken.None);
        }

        User user = users.FindByEmail("user@example.com");
        user.FailedAccessCount.Should().Be(DefaultAccountSecurityOptions.MaxFailedAttempts);
        user.IsLockedOut(FixedNow).Should().BeTrue();
        user.LockoutUntilUtc.Should().Be(FixedNow.Add(DefaultAccountSecurityOptions.LockoutDuration));
    }

    [Fact]
    public async Task Handle_LockedAccount_RejectsEvenCorrectPasswordWithoutCounting()
    {
        var users = new FakeUserRepository();
        users.AddActiveUser("user@example.com", Password);
        var handler = CreateHandler(users);

        for (int attempt = 0; attempt < DefaultAccountSecurityOptions.MaxFailedAttempts; attempt++)
        {
            await handler.Handle(
                new LoginCommand("user@example.com", "wrong-password-value"),
                CancellationToken.None);
        }

        Result<LoginResult> result = await handler.Handle(
            new LoginCommand("user@example.com", Password),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.FirstError!.Code.Should().Be("AuthenticationFailed");
        users.FindByEmail("user@example.com").FailedAccessCount.Should().Be(DefaultAccountSecurityOptions.MaxFailedAttempts);
    }

    [Fact]
    public async Task Handle_SuccessfulLogin_ResetsCounterAndLiftsLockout()
    {
        var users = new FakeUserRepository();
        users.AddActiveUser("user@example.com", Password);
        var handler = CreateHandler(users);

        await handler.Handle(new LoginCommand("user@example.com", "wrong-password-value"), CancellationToken.None);
        await handler.Handle(new LoginCommand("user@example.com", "wrong-password-value"), CancellationToken.None);

        Result<LoginResult> result = await handler.Handle(
            new LoginCommand("user@example.com", Password),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        User user = users.FindByEmail("user@example.com");
        user.FailedAccessCount.Should().Be(0);
        user.LockoutUntilUtc.Should().BeNull();
    }

    [Fact]
    public async Task Handle_ExpiredLockout_AllowsAuthenticationAndResetsCounter()
    {
        var users = new FakeUserRepository();
        users.AddActiveUser("user@example.com", Password);
        var handler = CreateHandler(users);
        for (int attempt = 0; attempt < DefaultAccountSecurityOptions.MaxFailedAttempts; attempt++)
        {
            await handler.Handle(
                new LoginCommand("user@example.com", "wrong-password-value"),
                CancellationToken.None);
        }

        // Once the lockout window has elapsed, a correct login succeeds and resets the count.
        var afterExpiry = new FixedClock(
            FixedNow.Add(DefaultAccountSecurityOptions.LockoutDuration).AddMinutes(1));
        LoginCommandHandler laterHandler = CreateHandler(users, clock: afterExpiry);

        Result<LoginResult> result = await laterHandler.Handle(
            new LoginCommand("user@example.com", Password),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        User user = users.FindByEmail("user@example.com");
        user.FailedAccessCount.Should().Be(0);
        user.LockoutUntilUtc.Should().BeNull();
    }

    [Fact]
    public async Task Handle_ActiveUserWithCorrectPassword_ReturnsTokenPairAndPersistsRefreshToken()
    {
        var users = new FakeUserRepository();
        User user = users.AddActiveUser("User@Example.com", Password);
        var accessTokens = new StubAccessTokenGenerator();
        var refreshTokens = new StubRefreshTokenService();
        var refreshTokenRepository = new FakeRefreshTokenRepository();
        var handler = new LoginCommandHandler(
            users,
            new PasswordCredentialService(new CountingPasswordHasher(), Options.Create(new PasswordPolicy())),
            accessTokens,
            refreshTokens,
            refreshTokenRepository,
            Options.Create(new RefreshTokenOptions()),
            Options.Create(new AccountSecurityOptions()),
            new FixedClock(FixedNow),
            new FakeUnitOfWork(),
            NullLogger<LoginCommandHandler>.Instance);

        Result<LoginResult> result = await handler.Handle(
            new LoginCommand("user@example.com", Password),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.AccessToken.Should().Be($"stub-token:{user.Id.Value:N}");
        result.Value.RefreshToken.Should().Be("stub-refresh-token-1");

        RefreshToken persisted = refreshTokenRepository.All.Should().ContainSingle().Subject;
        persisted.UserId.Should().Be(user.Id);
        persisted.TokenHash.Should().Be(refreshTokens.Hash(result.Value.RefreshToken));
        persisted.TokenHash.Should().NotBe(result.Value.RefreshToken);
        persisted.FamilyId.Should().NotBe(Guid.Empty);
        persisted.ExpiresAtUtc.Should().Be(FixedNow.Add(new RefreshTokenOptions().Lifetime));
        persisted.IsActive(FixedNow).Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WrongPassword_ReturnsUniformAuthenticationFailure()
    {
        var users = new FakeUserRepository();
        users.AddActiveUser("user@example.com", Password);

        Result<LoginResult> result = await CreateHandler(users).Handle(
            new LoginCommand("user@example.com", "wrong-password-value"),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.FirstError!.Type.Should().Be(ErrorType.Unauthorized);
        result.FirstError!.Code.Should().Be("AuthenticationFailed");
    }

    [Fact]
    public async Task Handle_UnknownUser_ReturnsSameFailureAndStillVerifiesPassword()
    {
        var hasher = new CountingPasswordHasher();
        var handler = CreateHandler(new FakeUserRepository(), hasher: hasher);

        Result<LoginResult> result = await handler.Handle(
            new LoginCommand("nobody@example.com", Password),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.FirstError!.Code.Should().Be("AuthenticationFailed");
        hasher.VerifyCallCount.Should().Be(1, "unknown accounts pay the same verification cost to hide their existence");
    }

    [Fact]
    public async Task Handle_UserWithoutPasswordCredential_ReturnsSameFailure()
    {
        var users = new FakeUserRepository();
        users.AddActiveUserWithoutCredential("user@example.com");

        Result<LoginResult> result = await CreateHandler(users).Handle(
            new LoginCommand("user@example.com", Password),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.FirstError!.Code.Should().Be("AuthenticationFailed");
    }

    [Theory]
    [InlineData(AccountStatus.Pending)]
    [InlineData(AccountStatus.Suspended)]
    [InlineData(AccountStatus.Disabled)]
    public async Task Handle_NonActiveAccount_ReturnsSameFailure(AccountStatus status)
    {
        var users = new FakeUserRepository();
        users.AddUserWithStatus("user@example.com", Password, status);

        Result<LoginResult> result = await CreateHandler(users).Handle(
            new LoginCommand("user@example.com", Password),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.FirstError!.Code.Should().Be("AuthenticationFailed");
    }

    [Fact]
    public async Task Handle_EveryFailure_ProducesIdenticalPublicError()
    {
        var wrongPassword = new LoginCommand("user@example.com", "wrong-password-value");
        var unknownUser = new LoginCommand("nobody@example.com", Password);

        var users = new FakeUserRepository();
        users.AddActiveUser("user@example.com", Password);

        LoginCommandHandler handler = CreateHandler(users);
        Error wrongPasswordError = (await handler.Handle(wrongPassword, CancellationToken.None)).FirstError!;
        Error unknownUserError = (await handler.Handle(unknownUser, CancellationToken.None)).FirstError!;

        wrongPasswordError.Code.Should().Be(unknownUserError.Code);
        wrongPasswordError.Description.Should().Be(unknownUserError.Description);
        wrongPasswordError.Type.Should().Be(unknownUserError.Type);
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-an-email")]
    [InlineData("user name@example.com")]
    public async Task Handle_MalformedEmail_ReturnsValidationFailure(string email)
    {
        LoginCommandHandler handler = CreateHandler(new FakeUserRepository());

        Result<LoginResult> result = await handler.Handle(
            new LoginCommand(email, Password),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.FirstError!.Type.Should().Be(ErrorType.Validation);
        result.FirstError!.Code.Should().Be("InvalidEmail");
    }

    /// <summary>
    /// Deterministic stand-in for the user repository that holds in-memory users keyed by their
    /// normalized email value, exactly like the production lookup.
    /// </summary>
    private sealed class FakeUserRepository : IUserRepository
    {
        private readonly Dictionary<string, User> _users = new(StringComparer.Ordinal);

        public void Add(User user) => _users[user.Email.NormalizedValue] = user;

        public User AddActiveUser(string email, string password)
        {
            User user = CreateUser(email, password, AccountStatus.Active);
            Add(user);
            return user;
        }

        public User AddActiveUserWithoutCredential(string email)
        {
            User user = User.Create(Email.From(email));
            user.Activate();
            Add(user);
            return user;
        }

        public User AddUserWithStatus(string email, string password, AccountStatus status)
        {
            User user = CreateUser(email, password, status);
            Add(user);
            return user;
        }

        public User FindByEmail(string email)
            => _users[Email.From(email).NormalizedValue];

        public Task<User?> GetByEmailAsync(Email email, CancellationToken cancellationToken = default)
            => Task.FromResult(_users.GetValueOrDefault(email.NormalizedValue));

        public Task<User?> GetByIdAsync(UserId id, CancellationToken cancellationToken = default)
            => Task.FromResult(_users.Values.FirstOrDefault(user => user.Id == id));

        public Task<User?> GetByIdTrackedAsync(UserId id, CancellationToken cancellationToken = default)
            => GetByIdAsync(id, cancellationToken);

        public Task ReloadTrackedAsync(User user, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task<bool> ExistsByEmailAsync(Email email, CancellationToken cancellationToken = default)
            => Task.FromResult(_users.ContainsKey(email.NormalizedValue));

        public void Update(User user) => Add(user);

        public void Remove(User user) => _users.Remove(user.Email.NormalizedValue);

        public void RemoveRange(IEnumerable<User> aggregateRoots)
        {
            foreach (User user in aggregateRoots)
            {
                Remove(user);
            }
        }

        private static User CreateUser(string email, string password, AccountStatus status)
        {
            User user = User.Create(
                Email.From(email),
                PasswordCredential.FromHash(new StubPasswordHasher().Hash(password)));

            switch (status)
            {
                case AccountStatus.Active:
                    user.Activate();
                    break;
                case AccountStatus.Suspended:
                    user.Activate();
                    user.Suspend();
                    break;
                case AccountStatus.Disabled:
                    user.Activate();
                    user.Disable();
                    break;
            }

            return user;
        }
    }

    /// <summary>
    /// Wraps the stub hasher and records verifications, so tests can assert the timing
    /// equalization path really paid the verification cost.
    /// </summary>
    private sealed class CountingPasswordHasher : IPasswordHasher
    {
        private readonly StubPasswordHasher _inner = new StubPasswordHasher();

        public int VerifyCallCount { get; private set; }

        public string Hash(string password) => _inner.Hash(password);

        public PasswordVerificationResult Verify(string password, string passwordHash)
        {
            VerifyCallCount++;
            return _inner.Verify(password, passwordHash);
        }

        public bool NeedsRehash(string passwordHash) => _inner.NeedsRehash(passwordHash);
    }
}
