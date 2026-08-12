using FluentAssertions;
using LastHour.BuildingBlocks.SharedKernel.Results;
using LastHour.Modules.Identity.Application.Commands;
using LastHour.Modules.Identity.Application.RefreshTokens;
using LastHour.Modules.Identity.Domain.RefreshTokens;
using LastHour.Modules.Identity.Domain.Users;
using LastHour.Modules.Identity.Tests.Application.TestSupport;
using Microsoft.Extensions.Logging.Abstractions;

namespace LastHour.Modules.Identity.Tests.Application.Commands;

public class LogoutAllCommandHandlerTests
{
    private static readonly DateTime FixedNow = new DateTime(2026, 8, 12, 10, 0, 0, DateTimeKind.Utc);

    private static LogoutAllCommandHandler CreateHandler(FakeRefreshTokenRepository refreshTokens)
        => new LogoutAllCommandHandler(
            refreshTokens,
            new FixedClock(FixedNow),
            NullLogger<LogoutAllCommandHandler>.Instance);

    private static RefreshToken CreateActiveSession(User user, string rawToken)
        => RefreshToken.Create(
            user.Id,
            Guid.NewGuid(),
            new StubRefreshTokenService().Hash(rawToken),
            new RefreshTokenOptions().Lifetime,
            FixedNow);

    [Fact]
    public async Task Handle_ActiveSessions_RevokesAllWithAllSessionsRevokedReason()
    {
        var users = new FakeUserRepository();
        User user = users.AddActiveUser("user@example.com", "password-value");
        var refreshTokens = new FakeRefreshTokenRepository();
        refreshTokens.Seed(CreateActiveSession(user, "session-one"));
        refreshTokens.Seed(CreateActiveSession(user, "session-two"));

        Result result = await CreateHandler(refreshTokens).Handle(
            new LogoutAllCommand(user.Id.Value),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        refreshTokens.All.Should().OnlyContain(session =>
            session.RevokedAtUtc == FixedNow
            && session.RevocationReason == TokenRevocationReason.AllSessionsRevoked);
    }

    [Fact]
    public async Task Handle_NoActiveSessions_SucceedsIdempotently()
    {
        var users = new FakeUserRepository();
        User user = users.AddActiveUser("user@example.com", "password-value");
        var refreshTokens = new FakeRefreshTokenRepository();

        Result result = await CreateHandler(refreshTokens).Handle(
            new LogoutAllCommand(user.Id.Value),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_OnlyRevokesSessionsOfTheRequestedUser()
    {
        var users = new FakeUserRepository();
        User owner = users.AddActiveUser("owner@example.com", "password-value");
        User other = users.AddActiveUser("other@example.com", "password-value");
        var refreshTokens = new FakeRefreshTokenRepository();
        refreshTokens.Seed(CreateActiveSession(owner, "owner-session"));
        refreshTokens.Seed(CreateActiveSession(other, "other-session"));

        Result result = await CreateHandler(refreshTokens).Handle(
            new LogoutAllCommand(owner.Id.Value),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        RefreshToken ownerSession = refreshTokens.All.Single(session => session.UserId == owner.Id);
        ownerSession.RevokedAtUtc.Should().Be(FixedNow);
        refreshTokens.All.Single(session => session.UserId == other.Id).RevokedAtUtc.Should().BeNull();
    }

    [Fact]
    public async Task Handle_AlreadyRevokedSession_StaysRevoked()
    {
        var users = new FakeUserRepository();
        User user = users.AddActiveUser("user@example.com", "password-value");
        var refreshTokens = new FakeRefreshTokenRepository();
        RefreshToken revoked = CreateActiveSession(user, "session");
        revoked.Revoke(TokenRevocationReason.Logout, null, FixedNow.AddMinutes(-1));
        refreshTokens.Seed(revoked);

        Result result = await CreateHandler(refreshTokens).Handle(
            new LogoutAllCommand(user.Id.Value),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        refreshTokens.All.Single().RevocationReason.Should().Be(TokenRevocationReason.Logout);
    }
}
