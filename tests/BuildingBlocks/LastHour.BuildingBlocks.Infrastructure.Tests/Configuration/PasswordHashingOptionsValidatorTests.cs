using FluentAssertions;
using LastHour.BuildingBlocks.Infrastructure.Security;
using Microsoft.Extensions.Options;

namespace LastHour.BuildingBlocks.Infrastructure.Tests.Configuration;

public class PasswordHashingOptionsValidatorTests
{
    private readonly PasswordHashingOptionsValidator _validator = new();

    [Fact]
    public void Validate_ProductionDefaults_Passes()
    {
        var options = new PasswordHashingOptions();

        ValidateOptionsResult result = _validator.Validate(null, options);

        result.Succeeded.Should().BeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_TimeCostBelowOne_Fails(int timeCost)
    {
        var options = new PasswordHashingOptions { TimeCost = timeCost };

        ValidateOptionsResult result = _validator.Validate(null, options);

        result.Failed.Should().BeTrue();
        result.FailureMessage.Should().Contain("TimeCost");
    }

    [Fact]
    public void Validate_MemoryCostBelowEightMegabytes_Fails()
    {
        var options = new PasswordHashingOptions { MemoryCost = 8191 };

        ValidateOptionsResult result = _validator.Validate(null, options);

        result.Failed.Should().BeTrue();
        result.FailureMessage.Should().Contain("MemoryCost");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-2)]
    public void Validate_LanesBelowOne_Fails(int lanes)
    {
        var options = new PasswordHashingOptions { Lanes = lanes };

        ValidateOptionsResult result = _validator.Validate(null, options);

        result.Failed.Should().BeTrue();
        result.FailureMessage.Should().Contain("Lanes");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_ThreadsBelowOne_Fails(int threads)
    {
        var options = new PasswordHashingOptions { Threads = threads };

        ValidateOptionsResult result = _validator.Validate(null, options);

        result.Failed.Should().BeTrue();
        result.FailureMessage.Should().Contain("Threads");
    }

    [Theory]
    [InlineData(8)]
    [InlineData(128)]
    public void Validate_HashLengthOutsideRange_Fails(int hashLength)
    {
        var options = new PasswordHashingOptions { HashLength = hashLength };

        ValidateOptionsResult result = _validator.Validate(null, options);

        result.Failed.Should().BeTrue();
        result.FailureMessage.Should().Contain("HashLength");
    }

    [Fact]
    public void Validate_SaltLengthBelowSixteen_Fails()
    {
        var options = new PasswordHashingOptions { SaltLength = 8 };

        ValidateOptionsResult result = _validator.Validate(null, options);

        result.Failed.Should().BeTrue();
        result.FailureMessage.Should().Contain("SaltLength");
    }
}
