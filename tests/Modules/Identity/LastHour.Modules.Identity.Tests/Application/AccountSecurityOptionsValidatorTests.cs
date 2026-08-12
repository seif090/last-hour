using FluentAssertions;
using LastHour.Modules.Identity.Application;
using Microsoft.Extensions.Options;

namespace LastHour.Modules.Identity.Tests.Application;

public class AccountSecurityOptionsValidatorTests
{
    private readonly AccountSecurityOptionsValidator _validator = new();

    [Fact]
    public void Validate_DefaultOptions_Passes()
    {
        ValidateOptionsResult result = _validator.Validate(null, new AccountSecurityOptions());

        result.Succeeded.Should().BeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void Validate_InvalidMaxFailedAttempts_Fails(int maxFailedAttempts)
    {
        var options = new AccountSecurityOptions { MaxFailedAttempts = maxFailedAttempts };

        ValidateOptionsResult result = _validator.Validate(null, options);

        result.Failed.Should().BeTrue();
        result.FailureMessage.Should().Contain("MaxFailedAttempts");
    }

    [Fact]
    public void Validate_ExcessiveMaxFailedAttempts_Fails()
    {
        var options = new AccountSecurityOptions { MaxFailedAttempts = 101 };

        ValidateOptionsResult result = _validator.Validate(null, options);

        result.Failed.Should().BeTrue();
        result.FailureMessage.Should().Contain("MaxFailedAttempts");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_NonPositiveLockoutDuration_Fails(int durationMinutes)
    {
        var options = new AccountSecurityOptions { LockoutDuration = TimeSpan.FromMinutes(durationMinutes) };

        ValidateOptionsResult result = _validator.Validate(null, options);

        result.Failed.Should().BeTrue();
        result.FailureMessage.Should().Contain("LockoutDuration");
    }

    [Fact]
    public void Validate_ExcessiveLockoutDuration_Fails()
    {
        var options = new AccountSecurityOptions { LockoutDuration = TimeSpan.FromDays(31) };

        ValidateOptionsResult result = _validator.Validate(null, options);

        result.Failed.Should().BeTrue();
        result.FailureMessage.Should().Contain("LockoutDuration");
    }
}
