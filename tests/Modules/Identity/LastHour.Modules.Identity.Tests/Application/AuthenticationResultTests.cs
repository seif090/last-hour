using FluentAssertions;
using LastHour.Modules.Identity.Application;

namespace LastHour.Modules.Identity.Tests.Application;

public class AuthenticationResultTests
{
    [Fact]
    public void Success_IsSucceeded_WithNoFailureReason()
    {
        AuthenticationResult result = AuthenticationResult.Success();

        result.Succeeded.Should().BeTrue();
        result.FailureReason.Should().BeNull();
    }

    [Theory]
    [InlineData(AuthenticationFailureReason.InvalidCredentials)]
    [InlineData(AuthenticationFailureReason.AccountPending)]
    [InlineData(AuthenticationFailureReason.AccountSuspended)]
    [InlineData(AuthenticationFailureReason.AccountDisabled)]
    public void Failure_CarriesTheFailureReason(AuthenticationFailureReason reason)
    {
        AuthenticationResult result = AuthenticationResult.Failure(reason);

        result.Succeeded.Should().BeFalse();
        result.FailureReason.Should().Be(reason);
    }
}
