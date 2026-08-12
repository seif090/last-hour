using FluentAssertions;
using LastHour.BuildingBlocks.SharedKernel.Results;
using LastHour.Modules.Identity.Application.Commands;
using LastHour.Modules.Identity.Application.RefreshTokens;
using LastHour.Modules.Identity.Domain.RefreshTokens;
using LastHour.Modules.Identity.Domain.Users;
using LastHour.Modules.Identity.Tests.Application.TestSupport;
using Microsoft.Extensions.Logging.Abstractions;

namespace LastHour.Modules.Identity.Tests.Application.Commands;

public class LogoutCommandHandlerTests
{
    private static readonly DateTime FixedNow = new DateTime(2026, 8, 12, 10, 0, 0, DateTimeKind.Utc);

    private static readonly RefreshTokenOptions Options = new RefreshTokenOptions();

    private static LogoutCommandHandler CreateHandler(FakeRefreshTokenRepository tokens)
        => new LogoutCommandHandler(
            tokens,
            new StubRefreshTokenService(),
            new FixedClock(FixedNow),
            NullLogger<LogoutCommandHandler>.Instance);

    private static (LogoutCommandHandler Handler, FakeRefreshTokenRepository Tokens, string Raw) CreateSeededHandler()
    {
        var tokens = new FakeRefreshTokenRepository();
        var service = new StubRefreshTokenService();
        var user = User.Create(Email.From("user@example.com"));
        string raw = "session-token";
        tokens.Seed(RefreshToken.Create(user.Id, Guid.NewGuid(), service.Hash(raw), Options.Lifetime, FixedNow));
        return (CreateHandler(tokens), tokens, raw);
    }

    [Fact]
    public async Task Handle_ActiveToken_RevokesSessionWithLogoutReason()
    {
        (LogoutCommandHandler handler, FakeRefreshTokenRepository tokens, string raw) = CreateSeededHandler();

        Result result = await handler.Handle(new LogoutCommand(raw), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        RefreshToken token = tokens.All.Should().ContainSingle().Subject;
        token.RevokedAtUtc.Should().Be(FixedNow);
        token.RevocationReason.Should().Be(TokenRevocationReason.Logout);
        token.ReplacedByTokenId.Should().BeNull();
    }

    [Fact]
    public async Task Handle_UnknownToken_StillSucceedsIdempotently()
    {
        (LogoutCommandHandler handler, FakeRefreshTokenRepository tokens, _) = CreateSeededHandler();

        Result result = await handler.Handle(new LogoutCommand("never-issued"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        tokens.All.Single().RevokedAtUtc.Should().BeNull("logout must not reveal whether a token existed");
    }

    [Fact]
    public async Task Handle_AlreadyRevokedToken_StillSucceedsIdempotently()
    {
        (LogoutCommandHandler handler, FakeRefreshTokenRepository tokens, string raw) = CreateSeededHandler();
        tokens.All.Single().Revoke(TokenRevocationReason.Rotation, null, FixedNow);

        Result result = await handler.Handle(new LogoutCommand(raw), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        tokens.All.Single().RevocationReason.Should().Be(TokenRevocationReason.Rotation, "an already-revoked token is not re-revoked");
    }
}
