using LastHour.Api.RateLimiting;
using Microsoft.Extensions.Options;

namespace LastHour.Api.Tests.Validators;

public class RateLimitSettingsValidatorTests
{
    private readonly RateLimitSettingsValidator _validator = new();

    [Fact]
    public void Validate_DisabledSettings_Succeeds()
    {
        var settings = new RateLimitSettings { Enabled = false, RejectionStatusCode = 200 };

        ValidateOptionsResult result = _validator.Validate(null, settings);

        Assert.True(result.Succeeded);
    }

    [Fact]
    public void Validate_EmptyPolicies_Succeeds()
    {
        var settings = new RateLimitSettings { Enabled = true };

        ValidateOptionsResult result = _validator.Validate(null, settings);

        Assert.True(result.Succeeded);
    }

    [Fact]
    public void Validate_ValidDefaultPolicies_Succeeds()
    {
        var settings = new RateLimitSettings
        {
            Enabled = true,
            RejectionStatusCode = 429,
            GlobalPolicyName = "Global",
            Policies =
            {
                new RateLimitPolicySettings
                {
                    Name = "Global",
                    Algorithm = RateLimitingAlgorithm.FixedWindow,
                    PartitionBy = RateLimitingPartitioning.Global,
                    PermitLimit = 120,
                    WindowSeconds = 60,
                },
                new RateLimitPolicySettings
                {
                    Name = "SystemStatus",
                    Algorithm = RateLimitingAlgorithm.SlidingWindow,
                    PartitionBy = RateLimitingPartitioning.IpAddress,
                    PermitLimit = 30,
                    WindowSeconds = 60,
                    SegmentsPerWindow = 2,
                },
            },
        };

        ValidateOptionsResult result = _validator.Validate(null, settings);

        Assert.True(result.Succeeded);
    }

    [Fact]
    public void Validate_RejectionStatusCodeOutsideErrorRange_Fails()
    {
        var settings = new RateLimitSettings { Enabled = true, RejectionStatusCode = 200 };

        ValidateOptionsResult result = _validator.Validate(null, settings);

        Assert.True(result.Failed);
        Assert.Contains("RejectionStatusCode", result.FailureMessage);
    }

    [Fact]
    public void Validate_GlobalPolicyNameNotConfigured_Fails()
    {
        var settings = new RateLimitSettings
        {
            Enabled = true,
            GlobalPolicyName = "Missing",
            Policies =
            {
                new RateLimitPolicySettings { Name = "Global" },
            },
        };

        ValidateOptionsResult result = _validator.Validate(null, settings);

        Assert.True(result.Failed);
        Assert.Contains("Missing", result.FailureMessage);
    }

    [Fact]
    public void Validate_DuplicatePolicyName_Fails()
    {
        var settings = new RateLimitSettings
        {
            Enabled = true,
            Policies =
            {
                new RateLimitPolicySettings { Name = "Global" },
                new RateLimitPolicySettings { Name = "Global" },
            },
        };

        ValidateOptionsResult result = _validator.Validate(null, settings);

        Assert.True(result.Failed);
        Assert.Contains("duplicated", result.FailureMessage);
    }

    [Fact]
    public void Validate_EmptyPolicyName_Fails()
    {
        var settings = new RateLimitSettings
        {
            Enabled = true,
            Policies =
            {
                new RateLimitPolicySettings { Name = string.Empty },
            },
        };

        ValidateOptionsResult result = _validator.Validate(null, settings);

        Assert.True(result.Failed);
        Assert.Contains("must not be empty", result.FailureMessage);
    }

    [Fact]
    public void Validate_ZeroPermitLimitForFixedWindow_Fails()
    {
        var settings = new RateLimitSettings
        {
            Enabled = true,
            Policies =
            {
                new RateLimitPolicySettings
                {
                    Name = "Global",
                    Algorithm = RateLimitingAlgorithm.FixedWindow,
                    PermitLimit = 0,
                },
            },
        };

        ValidateOptionsResult result = _validator.Validate(null, settings);

        Assert.True(result.Failed);
        Assert.Contains("PermitLimit", result.FailureMessage);
    }

    [Fact]
    public void Validate_ZeroWindowSecondsForTokenBucket_Fails()
    {
        var settings = new RateLimitSettings
        {
            Enabled = true,
            Policies =
            {
                new RateLimitPolicySettings
                {
                    Name = "Burst",
                    Algorithm = RateLimitingAlgorithm.TokenBucket,
                    TokenLimit = 10,
                    TokensPerPeriod = 2,
                    ReplenishmentPeriodSeconds = 60,
                    WindowSeconds = 0,
                },
            },
        };

        ValidateOptionsResult result = _validator.Validate(null, settings);

        Assert.True(result.Failed);
        Assert.Contains("WindowSeconds", result.FailureMessage);
    }

    [Fact]
    public void Validate_SlidingWindowWithoutSegments_Fails()
    {
        var settings = new RateLimitSettings
        {
            Enabled = true,
            Policies =
            {
                new RateLimitPolicySettings
                {
                    Name = "SystemStatus",
                    Algorithm = RateLimitingAlgorithm.SlidingWindow,
                    SegmentsPerWindow = 0,
                },
            },
        };

        ValidateOptionsResult result = _validator.Validate(null, settings);

        Assert.True(result.Failed);
        Assert.Contains("SegmentsPerWindow", result.FailureMessage);
    }

    [Fact]
    public void Validate_HeaderPartitioningWithoutHeaderName_Fails()
    {
        var settings = new RateLimitSettings
        {
            Enabled = true,
            Policies =
            {
                new RateLimitPolicySettings
                {
                    Name = "ByHeader",
                    PartitionBy = RateLimitingPartitioning.Header,
                },
            },
        };

        ValidateOptionsResult result = _validator.Validate(null, settings);

        Assert.True(result.Failed);
        Assert.Contains("PartitionHeaderName", result.FailureMessage);
    }

    [Fact]
    public void Validate_NoLimitPolicy_RequiresNoLimits()
    {
        var settings = new RateLimitSettings
        {
            Enabled = true,
            Policies =
            {
                new RateLimitPolicySettings
                {
                    Name = "Open",
                    Algorithm = RateLimitingAlgorithm.NoLimit,
                    PermitLimit = 0,
                    WindowSeconds = 0,
                },
            },
        };

        ValidateOptionsResult result = _validator.Validate(null, settings);

        Assert.True(result.Succeeded);
    }

    [Fact]
    public void Validate_UnknownAlgorithm_Fails()
    {
        var settings = new RateLimitSettings
        {
            Enabled = true,
            Policies =
            {
                new RateLimitPolicySettings
                {
                    Name = "Bogus",
                    Algorithm = (RateLimitingAlgorithm)999,
                },
            },
        };

        ValidateOptionsResult result = _validator.Validate(null, settings);

        Assert.True(result.Failed);
        Assert.Contains("Algorithm", result.FailureMessage);
    }
}
