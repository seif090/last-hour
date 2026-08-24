using FluentAssertions;
using LastHour.BuildingBlocks.Infrastructure.Persistence.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace LastHour.BuildingBlocks.Infrastructure.Tests.Configuration;

public class OutboxOptionsValidatorTests
{
    private readonly OutboxOptionsValidator _validator = new OutboxOptionsValidator();

    [Fact]
    public void Validate_ValidOptions_Succeeds()
    {
        var options = new OutboxOptions();

        ValidateOptionsResult result = _validator.Validate(null, options);

        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public void Validate_ZeroMaxRetryCount_Succeeds()
    {
        var options = new OutboxOptions { MaxRetryCount = 0 };

        ValidateOptionsResult result = _validator.Validate(null, options);

        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public void Validate_NegativeMaxRetryCount_Fails()
    {
        var options = new OutboxOptions { MaxRetryCount = -1 };

        ValidateOptionsResult result = _validator.Validate(null, options);

        result.Failed.Should().BeTrue();
        result.Failures.Should().Contain(f => f.Contains("MaxRetryCount"));
    }

    [Fact]
    public void Validate_ZeroBatchSize_Fails()
    {
        var options = new OutboxOptions { BatchSize = 0 };

        ValidateOptionsResult result = _validator.Validate(null, options);

        result.Failed.Should().BeTrue();
        result.Failures.Should().Contain(f => f.Contains("BatchSize"));
    }

    [Fact]
    public void Validate_NegativeBatchSize_Fails()
    {
        var options = new OutboxOptions { BatchSize = -5 };

        ValidateOptionsResult result = _validator.Validate(null, options);

        result.Failed.Should().BeTrue();
        result.Failures.Should().Contain(f => f.Contains("BatchSize"));
    }

    [Fact]
    public void Validate_SubSecondProcessingInterval_Fails()
    {
        var options = new OutboxOptions { ProcessingInterval = TimeSpan.FromMilliseconds(500) };

        ValidateOptionsResult result = _validator.Validate(null, options);

        result.Failed.Should().BeTrue();
        result.Failures.Should().Contain(f => f.Contains("ProcessingInterval"));
    }

    [Fact]
    public void Validate_SubSecondLeaseDuration_Fails()
    {
        var options = new OutboxOptions { LeaseDuration = TimeSpan.FromMilliseconds(500) };

        ValidateOptionsResult result = _validator.Validate(null, options);

        result.Failed.Should().BeTrue();
        result.Failures.Should().Contain(f => f.Contains("LeaseDuration"));
    }

    [Fact]
    public void Validate_SubSecondBaseRetryDelay_Fails()
    {
        var options = new OutboxOptions { BaseRetryDelay = TimeSpan.FromMilliseconds(500) };

        ValidateOptionsResult result = _validator.Validate(null, options);

        result.Failed.Should().BeTrue();
        result.Failures.Should().Contain(f => f.Contains("BaseRetryDelay"));
    }

    [Fact]
    public void Validate_SubSecondMaximumRetryDelay_Fails()
    {
        var options = new OutboxOptions { MaximumRetryDelay = TimeSpan.FromMilliseconds(500) };

        ValidateOptionsResult result = _validator.Validate(null, options);

        result.Failed.Should().BeTrue();
        result.Failures.Should().Contain(f => f.Contains("MaximumRetryDelay"));
    }

    [Fact]
    public void Validate_MaximumRetryDelayLessThanBaseRetryDelay_Fails()
    {
        var options = new OutboxOptions
        {
            BaseRetryDelay = TimeSpan.FromSeconds(10),
            MaximumRetryDelay = TimeSpan.FromSeconds(5),
        };

        ValidateOptionsResult result = _validator.Validate(null, options);

        result.Failed.Should().BeTrue();
        result.Failures.Should().Contain(f => f.Contains("MaximumRetryDelay"));
    }

    [Fact]
    public void Validate_MultipleInvalidProperties_CollectsAllFailures()
    {
        var options = new OutboxOptions
        {
            BatchSize = -1,
            MaxRetryCount = -2,
            ProcessingInterval = TimeSpan.Zero,
            LeaseDuration = TimeSpan.Zero,
            BaseRetryDelay = TimeSpan.Zero,
            MaximumRetryDelay = TimeSpan.Zero,
        };

        ValidateOptionsResult result = _validator.Validate(null, options);

        result.Failed.Should().BeTrue();
        result.Failures.Should().HaveCountGreaterThanOrEqualTo(5);
    }

    [Fact]
    public void DependencyInjection_ValidateOnStart_ThrowsOptionsValidationException_WhenConfigurationIsInvalid()
    {
        var invalidConfig = new Dictionary<string, string?>
        {
            ["Outbox:BatchSize"] = "0",
            ["Outbox:MaxRetryCount"] = "-1",
            ["Outbox:ProcessingInterval"] = "00:00:00.100",
        };

        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(invalidConfig)
            .Build();

        var services = new ServiceCollection();
        services.AddOptions<OutboxOptions>()
            .Bind(configuration.GetSection(OutboxOptions.SectionName))
            .ValidateOnStart();
        services.AddSingleton<IValidateOptions<OutboxOptions>, OutboxOptionsValidator>();

        using ServiceProvider serviceProvider = services.BuildServiceProvider();

        Action resolve = () => _ = serviceProvider.GetRequiredService<IOptions<OutboxOptions>>().Value;

        resolve.Should().Throw<OptionsValidationException>()
            .Which.Failures.Should().Contain(f => f.Contains("BatchSize"));
    }
}
