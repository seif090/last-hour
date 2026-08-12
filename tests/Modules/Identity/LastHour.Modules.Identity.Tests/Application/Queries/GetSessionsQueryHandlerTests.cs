using FluentAssertions;
using LastHour.BuildingBlocks.SharedKernel.Results;
using LastHour.Modules.Identity.Application.Queries;
using LastHour.Modules.Identity.Application.RefreshTokens;
using LastHour.Modules.Identity.Domain.RefreshTokens;
using LastHour.Modules.Identity.Domain.Users;
using LastHour.Modules.Identity.Tests.Application.TestSupport;

namespace LastHour.Modules.Identity.Tests.Application.Queries;

public class GetSessionsQueryHandlerTests
{
    private static readonly DateTime FixedNow = new DateTime(2026, 8, 12, 10, 0, 0, DateTimeKind.Utc);

    private static GetSessionsQueryHandler CreateHandler(FakeRefreshTokenRepository refreshTokens)
        => new GetSessionsQueryHandler(refreshTokens, new FixedClock(FixedNow));

    private static RefreshToken CreateActiveSession(User user, string rawToken)
        => RefreshToken.Create(
            user.Id,
            Guid.NewGuid(),
            new StubRefreshTokenService().Hash(rawToken),
            new RefreshTokenOptions().Lifetime,
            FixedNow);

    [Fact]
    public async Task Handle_ReturnsOnlySafeMetadata()
    {
        var users = new FakeUserRepository();
        User user = users.AddActiveUser("user@example.com", "password-value");
        var refreshTokens = new FakeRefreshTokenRepository();
        refreshTokens.Seed(CreateActiveSession(user, "session"));

        Result<IReadOnlyList<SessionInfo>> result = await CreateHandler(refreshTokens).Handle(
            new GetSessionsQuery(user.Id.Value),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        SessionInfo session = result.Value!.Should().ContainSingle().Subject;
        session.SessionId.Should().NotBe(Guid.Empty);
        session.CreatedAtUtc.Should().Be(FixedNow);
        session.ExpiresAtUtc.Should().Be(FixedNow.Add(new RefreshTokenOptions().Lifetime));
        session.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_RevokedAndExpiredSessions_AreMarkedInactive()
    {
        var users = new FakeUserRepository();
        User user = users.AddActiveUser("user@example.com", "password-value");
        var refreshTokens = new FakeRefreshTokenRepository();
        RefreshToken revoked = CreateActiveSession(user, "revoked");
        revoked.Revoke(TokenRevocationReason.Logout, null, FixedNow);
        RefreshToken expired = RefreshToken.Create(
            user.Id,
            Guid.NewGuid(),
            new StubRefreshTokenService().Hash("expired"),
            TimeSpan.FromDays(-1),
            FixedNow);
        refreshTokens.Seed(revoked);
        refreshTokens.Seed(expired);

        Result<IReadOnlyList<SessionInfo>> result = await CreateHandler(refreshTokens).Handle(
            new GetSessionsQuery(user.Id.Value),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Should().OnlyContain(session => !session.IsActive);
    }

    [Fact]
    public async Task Handle_OnlyReturnsSessionsOfTheRequestedUser()
    {
        var users = new FakeUserRepository();
        User owner = users.AddActiveUser("owner@example.com", "password-value");
        User other = users.AddActiveUser("other@example.com", "password-value");
        var refreshTokens = new FakeRefreshTokenRepository();
        refreshTokens.Seed(CreateActiveSession(owner, "owner-session"));
        refreshTokens.Seed(CreateActiveSession(other, "other-session"));

        Result<IReadOnlyList<SessionInfo>> result = await CreateHandler(refreshTokens).Handle(
            new GetSessionsQuery(owner.Id.Value),
            CancellationToken.None);

        result.Value!.Should().ContainSingle();
        result.Value.Single().SessionId.Should().Be(refreshTokens.All.Single(t => t.UserId == owner.Id).Id.Value);
    }

    [Fact]
    public async Task Handle_NoSessions_ReturnsEmptyList()
    {
        var users = new FakeUserRepository();
        User user = users.AddActiveUser("user@example.com", "password-value");

        Result<IReadOnlyList<SessionInfo>> result = await CreateHandler(new FakeRefreshTokenRepository()).Handle(
            new GetSessionsQuery(user.Id.Value),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }
}
