using FluentAssertions;
using LastHour.BuildingBlocks.Infrastructure.Security;
using Microsoft.Extensions.Options;

namespace LastHour.BuildingBlocks.Infrastructure.Tests.Configuration;

public class JwtOptionsValidatorTests
{
    private readonly JwtOptionsValidator _validator = new();

    private static JwtOptions ValidOptions() => new JwtOptions
    {
        Issuer = "https://lasthour.dev",
        Audience = "lasthour-api",
        SigningKey = "0123456789abcdef0123456789abcdef0123456789abcdef",
    };

    [Fact]
    public void Validate_ValidOptions_Passes()
    {
        ValidateOptionsResult result = _validator.Validate(null, ValidOptions());

        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public void Validate_MissingIssuer_Fails()
    {
        JwtOptions options = ValidOptions();
        options.Issuer = string.Empty;

        ValidateOptionsResult result = _validator.Validate(null, options);

        result.Failed.Should().BeTrue();
        result.FailureMessage.Should().Contain("Issuer");
    }

    [Fact]
    public void Validate_MissingAudience_Fails()
    {
        JwtOptions options = ValidOptions();
        options.Audience = string.Empty;

        ValidateOptionsResult result = _validator.Validate(null, options);

        result.Failed.Should().BeTrue();
        result.FailureMessage.Should().Contain("Audience");
    }

    [Fact]
    public void Validate_EmptySigningKey_Fails()
    {
        JwtOptions options = ValidOptions();
        options.SigningKey = string.Empty;

        ValidateOptionsResult result = _validator.Validate(null, options);

        result.Failed.Should().BeTrue();
        result.FailureMessage.Should().Contain("SigningKey");
    }

    [Fact]
    public void Validate_ShortSigningKey_Fails()
    {
        JwtOptions options = ValidOptions();
        options.SigningKey = "too-short";

        ValidateOptionsResult result = _validator.Validate(null, options);

        result.Failed.Should().BeTrue();
        result.FailureMessage.Should().Contain("SigningKey");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_NonPositiveLifetime_Fails(double lifetimeMinutes)
    {
        JwtOptions options = ValidOptions();
        options.AccessTokenLifetime = TimeSpan.FromMinutes(lifetimeMinutes);

        ValidateOptionsResult result = _validator.Validate(null, options);

        result.Failed.Should().BeTrue();
        result.FailureMessage.Should().Contain("AccessTokenLifetime");
    }

    [Fact]
    public void Validate_ExcessiveLifetime_Fails()
    {
        JwtOptions options = ValidOptions();
        options.AccessTokenLifetime = JwtOptions.MaximumAccessTokenLifetime.Add(TimeSpan.FromMinutes(1));

        ValidateOptionsResult result = _validator.Validate(null, options);

        result.Failed.Should().BeTrue();
        result.FailureMessage.Should().Contain("AccessTokenLifetime");
    }

    [Fact]
    public void Validate_ExcessiveClockSkew_Fails()
    {
        JwtOptions options = ValidOptions();
        options.ClockSkew = JwtOptions.MaximumClockSkew.Add(TimeSpan.FromMinutes(1));

        ValidateOptionsResult result = _validator.Validate(null, options);

        result.Failed.Should().BeTrue();
        result.FailureMessage.Should().Contain("ClockSkew");
    }
}
