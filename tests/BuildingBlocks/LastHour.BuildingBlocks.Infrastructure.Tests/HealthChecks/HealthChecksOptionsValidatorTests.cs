using FluentAssertions;
using LastHour.BuildingBlocks.Infrastructure.HealthChecks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace LastHour.BuildingBlocks.Infrastructure.Tests.HealthChecks;

public class HealthChecksOptionsValidatorTests
{
    private readonly HealthChecksOptionsValidator _validator = new();

    [Fact]
    public void Validate_Defaults_ReturnsSuccess()
    {
        var options = new HealthChecksOptions();

        ValidateOptionsResult result = _validator.Validate(Options.DefaultName, options);

        result.Succeeded.Should().BeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void Validate_InvalidTimeoutSeconds_Fails(int timeoutSeconds)
    {
        var options = new HealthChecksOptions { TimeoutSeconds = timeoutSeconds };

        ValidateOptionsResult result = _validator.Validate(Options.DefaultName, options);

        result.Failed.Should().BeTrue();
        result.Failures.Should().Contain(failure => failure.Contains(nameof(HealthChecksOptions.TimeoutSeconds), StringComparison.Ordinal));
    }

    [Fact]
    public void Validate_NegativeDiskMinimum_Fails()
    {
        var options = new HealthChecksOptions
        {
            Disk = new DiskHealthCheckOptions { MinimumFreeMegabytes = -1 },
        };

        ValidateOptionsResult result = _validator.Validate(Options.DefaultName, options);

        result.Failed.Should().BeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    public void Validate_InvalidMemoryMaximum_Fails(long maximumUsedBytes)
    {
        var options = new HealthChecksOptions
        {
            Memory = new MemoryHealthCheckOptions { MaximumUsedBytes = maximumUsedBytes },
        };

        ValidateOptionsResult result = _validator.Validate(Options.DefaultName, options);

        result.Failed.Should().BeTrue();
        result.Failures.Should().Contain(failure => failure.Contains(nameof(HealthChecksOptions.Memory), StringComparison.Ordinal));
    }

    [Fact]
    public void Validate_NegativeWarningPendingMessageCount_Fails()
    {
        var options = new HealthChecksOptions
        {
            Outbox = new OutboxHealthCheckOptions { WarningPendingMessageCount = -1 },
        };

        ValidateOptionsResult result = _validator.Validate(Options.DefaultName, options);

        result.Failed.Should().BeTrue();
        result.Failures.Should().Contain(failure => failure.Contains("WarningPendingMessageCount", StringComparison.Ordinal));
    }

    [Fact]
    public void Validate_CriticalPendingMessageCountLessOrEqualToWarning_Fails()
    {
        var options = new HealthChecksOptions
        {
            Outbox = new OutboxHealthCheckOptions
            {
                WarningPendingMessageCount = 50,
                CriticalPendingMessageCount = 50,
            },
        };

        ValidateOptionsResult result = _validator.Validate(Options.DefaultName, options);

        result.Failed.Should().BeTrue();
        result.Failures.Should().Contain(failure => failure.Contains("CriticalPendingMessageCount", StringComparison.Ordinal));
    }

    [Fact]
    public void Validate_NonPositiveWarningOldestMessageAge_Fails()
    {
        var options = new HealthChecksOptions
        {
            Outbox = new OutboxHealthCheckOptions { WarningOldestMessageAge = TimeSpan.Zero },
        };

        ValidateOptionsResult result = _validator.Validate(Options.DefaultName, options);

        result.Failed.Should().BeTrue();
        result.Failures.Should().Contain(failure => failure.Contains("WarningOldestMessageAge", StringComparison.Ordinal));
    }

    [Fact]
    public void Validate_CriticalOldestMessageAgeLessOrEqualToWarning_Fails()
    {
        var options = new HealthChecksOptions
        {
            Outbox = new OutboxHealthCheckOptions
            {
                WarningOldestMessageAge = TimeSpan.FromMinutes(5),
                CriticalOldestMessageAge = TimeSpan.FromMinutes(5),
            },
        };

        ValidateOptionsResult result = _validator.Validate(Options.DefaultName, options);

        result.Failed.Should().BeTrue();
        result.Failures.Should().Contain(failure => failure.Contains("CriticalOldestMessageAge", StringComparison.Ordinal));
    }

    [Fact]
    public void Validate_NonPositiveProcessorHeartbeatTimeout_Fails()
    {
        var options = new HealthChecksOptions
        {
            Outbox = new OutboxHealthCheckOptions { ProcessorHeartbeatTimeout = TimeSpan.Zero },
        };

        ValidateOptionsResult result = _validator.Validate(Options.DefaultName, options);

        result.Failed.Should().BeTrue();
        result.Failures.Should().Contain(failure => failure.Contains("ProcessorHeartbeatTimeout", StringComparison.Ordinal));
    }

    [Fact]
    public void Validate_NegativeWarningAbandonedMessageCount_Fails()
    {
        var options = new HealthChecksOptions
        {
            Outbox = new OutboxHealthCheckOptions { WarningAbandonedMessageCount = -1 },
        };

        ValidateOptionsResult result = _validator.Validate(Options.DefaultName, options);

        result.Failed.Should().BeTrue();
        result.Failures.Should().Contain(failure => failure.Contains("WarningAbandonedMessageCount", StringComparison.Ordinal));
    }

    [Fact]
    public void Validate_CriticalAbandonedMessageCountLessThanWarning_Fails()
    {
        var options = new HealthChecksOptions
        {
            Outbox = new OutboxHealthCheckOptions
            {
                WarningAbandonedMessageCount = 10,
                CriticalAbandonedMessageCount = 5,
            },
        };

        ValidateOptionsResult result = _validator.Validate(Options.DefaultName, options);

        result.Failed.Should().BeTrue();
        result.Failures.Should().Contain(failure => failure.Contains("CriticalAbandonedMessageCount", StringComparison.Ordinal));
    }

    [Fact]
    public void Validate_CriticalAbandonedMessageCountEqualToWarning_Fails()
    {
        var options = new HealthChecksOptions
        {
            Outbox = new OutboxHealthCheckOptions
            {
                WarningAbandonedMessageCount = 10,
                CriticalAbandonedMessageCount = 10,
            },
        };

        ValidateOptionsResult result = _validator.Validate(Options.DefaultName, options);

        result.Failed.Should().BeTrue();
        result.Failures.Should().Contain(failure => failure.Contains("CriticalAbandonedMessageCount", StringComparison.Ordinal));
    }

    [Fact]
    public void Validate_NegativeWarningStalledLeaseCount_Fails()
    {
        var options = new HealthChecksOptions
        {
            ConsumerInbox = new ConsumerInboxHealthCheckOptions { WarningStalledLeaseCount = -1 },
        };

        ValidateOptionsResult result = _validator.Validate(Options.DefaultName, options);

        result.Failed.Should().BeTrue();
        result.Failures.Should().Contain(failure => failure.Contains("WarningStalledLeaseCount", StringComparison.Ordinal));
    }

    [Fact]
    public void Validate_CriticalStalledLeaseCountLessOrEqualToWarning_Fails()
    {
        var options = new HealthChecksOptions
        {
            ConsumerInbox = new ConsumerInboxHealthCheckOptions
            {
                WarningStalledLeaseCount = 20,
                CriticalStalledLeaseCount = 20,
            },
        };

        ValidateOptionsResult result = _validator.Validate(Options.DefaultName, options);

        result.Failed.Should().BeTrue();
        result.Failures.Should().Contain(failure => failure.Contains("CriticalStalledLeaseCount", StringComparison.Ordinal));
    }

    [Fact]
    public void Validate_NonPositiveWarningOldestStalledAge_Fails()
    {
        var options = new HealthChecksOptions
        {
            ConsumerInbox = new ConsumerInboxHealthCheckOptions { WarningOldestStalledAge = TimeSpan.Zero },
        };

        ValidateOptionsResult result = _validator.Validate(Options.DefaultName, options);

        result.Failed.Should().BeTrue();
        result.Failures.Should().Contain(failure => failure.Contains("WarningOldestStalledAge", StringComparison.Ordinal));
    }

    [Fact]
    public void Validate_CriticalOldestStalledAgeLessOrEqualToWarning_Fails()
    {
        var options = new HealthChecksOptions
        {
            ConsumerInbox = new ConsumerInboxHealthCheckOptions
            {
                WarningOldestStalledAge = TimeSpan.FromMinutes(10),
                CriticalOldestStalledAge = TimeSpan.FromMinutes(10),
            },
        };

        ValidateOptionsResult result = _validator.Validate(Options.DefaultName, options);

        result.Failed.Should().BeTrue();
        result.Failures.Should().Contain(failure => failure.Contains("CriticalOldestStalledAge", StringComparison.Ordinal));
    }

    [Fact]
    public void DependencyInjection_ValidateOnStart_ThrowsOptionsValidationException_WhenHealthChecksConfigurationIsInvalid()
    {
        var invalidConfig = new Dictionary<string, string?>
        {
            ["HealthChecks:Outbox:WarningPendingMessageCount"] = "100",
            ["HealthChecks:Outbox:CriticalPendingMessageCount"] = "50",
        };

        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(invalidConfig)
            .Build();

        var services = new ServiceCollection();
        services.AddOptions<HealthChecksOptions>()
            .Bind(configuration.GetSection(HealthChecksOptions.SectionName))
            .ValidateOnStart();
        services.AddSingleton<IValidateOptions<HealthChecksOptions>, HealthChecksOptionsValidator>();

        using ServiceProvider serviceProvider = services.BuildServiceProvider();

        Action resolve = () => _ = serviceProvider.GetRequiredService<IOptions<HealthChecksOptions>>().Value;

        resolve.Should().Throw<OptionsValidationException>()
            .Which.Failures.Should().Contain(f => f.Contains("CriticalPendingMessageCount"));
    }
}
