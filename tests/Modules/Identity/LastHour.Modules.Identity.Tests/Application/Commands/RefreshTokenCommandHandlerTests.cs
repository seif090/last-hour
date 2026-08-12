using FluentAssertions;
using LastHour.BuildingBlocks.SharedKernel.Results;
using LastHour.Modules.Identity.Application.Commands;
using LastHour.Modules.Identity.Application.RefreshTokens;
using LastHour.Modules.Identity.Domain.RefreshTokens;
using LastHour.Modules.Identity.Domain.Users;
using LastHour.Modules.Identity.Tests.Application.TestSupport;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace LastHour.Modules.Identity.Tests.Application.Commands;

public class RefreshTokenCommandHandlerTests
{
    private static readonly DateTime FixedNow = new DateTime(2026, 8, 12, 10, 0, 0, DateTimeKind.Utc);

    private static readonly RefreshTokenOptions DefaultOptions = new RefreshTokenOptions();

    private static RefreshTokenCommandHandler CreateHandler(
        FakeRefreshTokenRepository refreshTokens,
        FakeUserRepository users,
        FakeUnitOfWork? unitOfWork = null)
    {
        return new RefreshTokenCommandHandler(
            refreshTokens,
            new StubRefreshTokenService(),
            users,
            new StubAccessTokenGenerator(),
            Options.Create(DefaultOptions),
            new FixedClock(FixedNow),
            unitOfWork ?? new FakeUnitOfWork(),
            NullLogger<RefreshTokenCommandHandler>.Instance);
    }

    private static FakeUserRepository CreateUsersWithActiveAccount(out User user)
    {
        var users = new FakeUserRepository();
        user = users.AddActiveUser("user@example.com", "correct horse battery staple");
        return users;
    }

    private static RefreshToken CreateActiveToken(User user, string tokenHash, Guid? familyId = null)
        => RefreshToken.Create(user.Id, familyId ?? Guid.NewGuid(), tokenHash, DefaultOptions.Lifetime, FixedNow);

    [Fact]
    public async Task Handle_ActiveToken_RotatesAtomicallyAndReturnsNewPair()
    {
        var users = CreateUsersWithActiveAccount(out User user);
        var tokens = new FakeRefreshTokenRepository();
        var service = new StubRefreshTokenService();
        string raw = "raw-token";
        tokens.Seed(CreateActiveToken(user, service.Hash(raw)));

        var handler = new RefreshTokenCommandHandler(
            tokens,
            service,
            users,
            new StubAccessTokenGenerator(),
            Options.Create(DefaultOptions),
            new FixedClock(FixedNow),
            new FakeUnitOfWork(),
            NullLogger<RefreshTokenCommandHandler>.Instance);

        Result<RefreshTokenResult> result = await handler.Handle(
            new RefreshTokenCommand(raw),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.RefreshToken.Should().Be("stub-refresh-token-1");

        RefreshToken oldToken = tokens.All.Single(token => token.TokenHash == service.Hash(raw));
        oldToken.RevokedAtUtc.Should().Be(FixedNow);
        oldToken.RevocationReason.Should().Be(TokenRevocationReason.Rotation);
        oldToken.ReplacedByTokenId.Should().NotBeNull();

        RefreshToken successor = tokens.All.Single(token => token.Id == oldToken.ReplacedByTokenId);
        successor.FamilyId.Should().Be(oldToken.FamilyId, "rotation keeps the session family");
        successor.UserId.Should().Be(user.Id);
        successor.IsActive(FixedNow).Should().BeTrue();
    }

    [Fact]
    public async Task Handle_UnknownToken_ReturnsUniformFailure()
    {
        var users = CreateUsersWithActiveAccount(out _);
        var tokens = new FakeRefreshTokenRepository();

        Result<RefreshTokenResult> result = await CreateHandler(tokens, users).Handle(
            new RefreshTokenCommand("never-issued"),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.FirstError!.Type.Should().Be(ErrorType.Unauthorized);
        result.FirstError!.Code.Should().Be("InvalidRefreshToken");
    }

    [Fact]
    public async Task Handle_ExpiredToken_ReturnsUniformFailureWithoutRotation()
    {
        var users = CreateUsersWithActiveAccount(out User user);
        var tokens = new FakeRefreshTokenRepository();
        var service = new StubRefreshTokenService();
        string raw = "expired-token";
        // A token whose lifetime already elapsed: not revoked, simply past its expiration.
        RefreshToken expired = RefreshToken.Create(user.Id, Guid.NewGuid(), service.Hash(raw), TimeSpan.FromDays(-1), FixedNow);
        tokens.Seed(expired);

        Result<RefreshTokenResult> result = await CreateHandler(tokens, users).Handle(
            new RefreshTokenCommand(raw),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.FirstError!.Code.Should().Be("InvalidRefreshToken");
        tokens.All.Should().HaveCount(1, "no successor may be created for an expired token");
        tokens.All.Single().RevokedAtUtc.Should().BeNull("expiration is not a revocation; no row is written");
    }

    [Fact]
    public async Task Handle_RevokedToken_ReturnsUniformFailure()
    {
        var users = CreateUsersWithActiveAccount(out User user);
        var tokens = new FakeRefreshTokenRepository();
        var service = new StubRefreshTokenService();
        string raw = "logged-out-token";
        RefreshToken revoked = CreateActiveToken(user, service.Hash(raw));
        revoked.Revoke(TokenRevocationReason.Logout, null, FixedNow.AddDays(-1));
        tokens.Seed(revoked);

        Result<RefreshTokenResult> result = await CreateHandler(tokens, users).Handle(
            new RefreshTokenCommand(raw),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.FirstError!.Code.Should().Be("InvalidRefreshToken");
    }

    [Fact]
    public async Task Handle_ReusedRotatedToken_RevokesWholeFamilyAndFails()
    {
        var users = CreateUsersWithActiveAccount(out User user);
        var tokens = new FakeRefreshTokenRepository();
        var service = new StubRefreshTokenService();
        var family = Guid.NewGuid();

        string oldRaw = "old-token";
        RefreshToken oldToken = CreateActiveToken(user, service.Hash(oldRaw), family);
        tokens.Seed(oldToken);

        string newRaw = "new-token";
        RefreshToken successor = CreateActiveToken(user, service.Hash(newRaw), family);
        tokens.Seed(successor);

        // Simulate a completed rotation: the old token was revoked and replaced.
        oldToken.Revoke(TokenRevocationReason.Rotation, successor.Id, FixedNow);

        var unitOfWork = new FakeUnitOfWork();
        Result<RefreshTokenResult> result = await CreateHandler(tokens, users, unitOfWork).Handle(
            new RefreshTokenCommand(oldRaw),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.FirstError!.Code.Should().Be("InvalidRefreshToken");

        successor.RevokedAtUtc.Should().Be(FixedNow, "the whole family is revoked on reuse");
        successor.RevocationReason.Should().Be(TokenRevocationReason.ReuseDetected);
        unitOfWork.SaveChangesCount.Should().Be(1, "the family revocation commits even though the command fails");
    }

    [Fact]
    public async Task Handle_DisabledAccount_RevokesSessionAndFails()
    {
        var users = new FakeUserRepository();
        User user = users.AddUserWithStatus("user@example.com", "correct horse battery staple", AccountStatus.Disabled);
        var tokens = new FakeRefreshTokenRepository();
        var service = new StubRefreshTokenService();
        string raw = "session-token";
        tokens.Seed(CreateActiveToken(user, service.Hash(raw)));

        Result<RefreshTokenResult> result = await CreateHandler(tokens, users).Handle(
            new RefreshTokenCommand(raw),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.FirstError!.Code.Should().Be("InvalidRefreshToken");
        RefreshToken token = tokens.All.Should().ContainSingle().Subject;
        token.RevokedAtUtc.Should().Be(FixedNow);
        token.RevocationReason.Should().Be(TokenRevocationReason.SecurityEvent);
    }

    [Fact]
    public async Task Handle_EveryFailure_ProducesIdenticalPublicError()
    {
        var users = CreateUsersWithActiveAccount(out User user);
        var tokens = new FakeRefreshTokenRepository();
        var service = new StubRefreshTokenService();
        string raw = "token";
        tokens.Seed(CreateActiveToken(user, service.Hash(raw)));

        var handler = CreateHandler(tokens, users);
        Error unknown = (await handler.Handle(new RefreshTokenCommand("missing"), CancellationToken.None)).FirstError!;

        RefreshToken revoked = CreateActiveToken(user, service.Hash("revoked"));
        revoked.Revoke(TokenRevocationReason.Logout, null, FixedNow);
        tokens.Seed(revoked);
        Error revokedError = (await handler.Handle(new RefreshTokenCommand("revoked"), CancellationToken.None)).FirstError!;

        unknown.Code.Should().Be(revokedError.Code);
        unknown.Description.Should().Be(revokedError.Description);
        unknown.Type.Should().Be(revokedError.Type);
    }

    /// <summary>
    /// In-memory user repository keyed by normalized email, mirroring the production lookup,
    /// supporting the account-state check the refresh flow performs.
    /// </summary>
    private sealed class FakeUserRepository : IUserRepository
    {
        private readonly Dictionary<string, User> _users = new(StringComparer.Ordinal);

        public void Add(User user) => _users[user.Email.NormalizedValue] = user;

        public User AddActiveUser(string email, string password)
        {
            User user = User.Create(
                Email.From(email),
                PasswordCredential.FromHash(new StubPasswordHasher().Hash(password)));
            user.Activate();
            Add(user);
            return user;
        }

        public User AddUserWithStatus(string email, string password, AccountStatus status)
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

            Add(user);
            return user;
        }

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
    }
}
