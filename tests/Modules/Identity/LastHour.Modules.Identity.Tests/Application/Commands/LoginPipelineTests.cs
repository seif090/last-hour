using FluentAssertions;
using LastHour.BuildingBlocks.SharedKernel.Results;
using LastHour.Modules.Identity.Application;
using LastHour.Modules.Identity.Application.Commands;
using LastHour.Modules.Identity.Domain.Users;
using LastHour.Modules.Identity.Tests.Application.Commands.TestSupport;

namespace LastHour.Modules.Identity.Tests.Application.Commands;

/// <summary>
/// Exercises the lockout flow over the real CQRS pipeline and EF persistence (tracked reads,
/// the failed-attempt counter, the lockout window): repeated wrong passwords lock the account,
/// and a locked account rejects even the correct password until the window expires.
/// </summary>
public class LoginPipelineTests
{
    private const string Email = "lockout@example.com";
    private const string Password = "correct horse battery staple";

    [Fact]
    public async Task Pipeline_WrongPasswords_EventuallyLockTheAccount()
    {
        await using TestAuthPipeline pipeline = await TestAuthPipeline.CreateAsync();
        await pipeline.RegisterAsync(Email, Password);
        await pipeline.ActivateUserAsync("lockout@example.com");

        var options = new AccountSecurityOptions();
        for (int attempt = 0; attempt < options.MaxFailedAttempts; attempt++)
        {
            Result<LoginResult> result = await pipeline.LoginAsync(Email, "wrong-password-value");
            result.IsFailure.Should().BeTrue();
            result.FirstError!.Code.Should().Be("AuthenticationFailed");
        }

        User user = (await pipeline.FindByNormalizedEmailAsync("lockout@example.com"))!;
        user.FailedAccessCount.Should().Be(options.MaxFailedAttempts);
        user.IsLockedOut(user.LockoutUntilUtc!.Value.AddMinutes(-1)).Should().BeTrue();

        // Even the correct password is rejected while the lockout is active, with the same
        // uniform failure, and the counter does not grow further.
        Result<LoginResult> lockedAttempt = await pipeline.LoginAsync(Email, Password);
        lockedAttempt.IsFailure.Should().BeTrue();
        lockedAttempt.FirstError!.Code.Should().Be("AuthenticationFailed");

        User afterLockedAttempt = (await pipeline.FindByNormalizedEmailAsync("lockout@example.com"))!;
        afterLockedAttempt.FailedAccessCount.Should().Be(options.MaxFailedAttempts);
    }
}
