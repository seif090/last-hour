using FluentAssertions;
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

public class ChangePasswordCommandHandlerTests
{
    private const string CurrentPassword = "correct horse battery staple";
    private const string NewPassword = "correct horse battery staple 2";

    private static readonly DateTime FixedNow = new DateTime(2026, 8, 12, 10, 0, 0, DateTimeKind.Utc);

    private static ChangePasswordCommandHandler CreateHandler(
        FakeUserRepository users,
        FakeRefreshTokenRepository refreshTokens)
    {
        return new ChangePasswordCommandHandler(
            users,
            new PasswordCredentialService(new StubPasswordHasher(), Options.Create(new PasswordPolicy())),
            refreshTokens,
            new FixedClock(FixedNow),
            NullLogger<ChangePasswordCommandHandler>.Instance);
    }

    private static RefreshToken CreateActiveSession(User user, string rawToken)
        => RefreshToken.Create(
            user.Id,
            Guid.NewGuid(),
            new StubRefreshTokenService().Hash(rawToken),
            new RefreshTokenOptions().Lifetime,
            FixedNow);

    [Fact]
    public async Task Handle_CorrectCurrentPassword_ChangesCredentialAndRecordsChange()
    {
        var users = new FakeUserRepository();
        User user = users.AddActiveUser("user@example.com", CurrentPassword);
        var handler = CreateHandler(users, new FakeRefreshTokenRepository());

        Result result = await handler.Handle(
            new ChangePasswordCommand(user.Id.Value, CurrentPassword, NewPassword),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        user.PasswordCredential!.Hash.Should().Be(new StubPasswordHasher().Hash(NewPassword));
        user.LastPasswordChangedAtUtc.Should().Be(FixedNow);
        user.Version.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task Handle_WrongCurrentPassword_FailsWithoutChanging()
    {
        var users = new FakeUserRepository();
        User user = users.AddActiveUser("user@example.com", CurrentPassword);
        var handler = CreateHandler(users, new FakeRefreshTokenRepository());

        Result result = await handler.Handle(
            new ChangePasswordCommand(user.Id.Value, "wrong-current-password", NewPassword),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.FirstError!.Code.Should().Be("Identity.InvalidCurrentPassword");
        user.PasswordCredential!.Hash.Should().Be(new StubPasswordHasher().Hash(CurrentPassword));
    }

    [Fact]
    public async Task Handle_WeakNewPassword_FailsPolicyValidation()
    {
        var users = new FakeUserRepository();
        User user = users.AddActiveUser("user@example.com", CurrentPassword);
        var handler = CreateHandler(users, new FakeRefreshTokenRepository());

        Result result = await handler.Handle(
            new ChangePasswordCommand(user.Id.Value, CurrentPassword, "short"),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.FirstError!.Code.Should().Be("Identity.PasswordTooShort");
        user.PasswordCredential!.Hash.Should().Be(new StubPasswordHasher().Hash(CurrentPassword));
    }

    [Fact]
    public async Task Handle_IdenticalPassword_RejectedSafely()
    {
        var users = new FakeUserRepository();
        User user = users.AddActiveUser("user@example.com", CurrentPassword);
        var handler = CreateHandler(users, new FakeRefreshTokenRepository());

        Result result = await handler.Handle(
            new ChangePasswordCommand(user.Id.Value, CurrentPassword, CurrentPassword),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.FirstError!.Code.Should().Be("Identity.PasswordUnchanged");
        user.PasswordCredential!.Hash.Should().Be(new StubPasswordHasher().Hash(CurrentPassword));
    }

    [Fact]
    public async Task Handle_UnknownUser_ReturnsAuthenticationFailure()
    {
        var handler = CreateHandler(new FakeUserRepository(), new FakeRefreshTokenRepository());

        Result result = await handler.Handle(
            new ChangePasswordCommand(Guid.NewGuid(), CurrentPassword, NewPassword),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.FirstError!.Code.Should().Be("UserNotFound");
    }

    [Fact]
    public async Task Handle_SuccessfulChange_RevokesEveryActiveSessionWithPasswordChangedReason()
    {
        var users = new FakeUserRepository();
        User user = users.AddActiveUser("user@example.com", CurrentPassword);
        var refreshTokens = new FakeRefreshTokenRepository();
        refreshTokens.Seed(CreateActiveSession(user, "session-one"));
        refreshTokens.Seed(CreateActiveSession(user, "session-two"));
        var handler = CreateHandler(users, refreshTokens);

        Result result = await handler.Handle(
            new ChangePasswordCommand(user.Id.Value, CurrentPassword, NewPassword),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        refreshTokens.All.Should().OnlyContain(session =>
            session.RevokedAtUtc == FixedNow
            && session.RevocationReason == TokenRevocationReason.PasswordChanged);
    }
}
