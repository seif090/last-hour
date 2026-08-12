using FluentAssertions;
using LastHour.Modules.Identity.Application.RefreshTokens;
using Microsoft.Extensions.Options;

namespace LastHour.Modules.Identity.Tests.Application.RefreshTokens;

public class RefreshTokenOptionsValidatorTests
{
    private readonly RefreshTokenOptionsValidator _validator = new();

    [Fact]
    public void Validate_DefaultLifetime_Passes()
    {
        ValidateOptionsResult result = _validator.Validate(null, new RefreshTokenOptions());

        result.Succeeded.Should().BeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_NonPositiveLifetime_Fails(double lifetimeDays)
    {
        var options = new RefreshTokenOptions { Lifetime = TimeSpan.FromDays(lifetimeDays) };

        ValidateOptionsResult result = _validator.Validate(null, options);

        result.Failed.Should().BeTrue();
        result.FailureMessage.Should().Contain("Lifetime");
    }

    [Fact]
    public void Validate_ExcessiveLifetime_Fails()
    {
        var options = new RefreshTokenOptions { Lifetime = RefreshTokenOptions.MaximumLifetime.Add(TimeSpan.FromDays(1)) };

        ValidateOptionsResult result = _validator.Validate(null, options);

        result.Failed.Should().BeTrue();
        result.FailureMessage.Should().Contain("Lifetime");
    }
}
