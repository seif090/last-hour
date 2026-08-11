using Microsoft.Extensions.Options;

namespace LastHour.Api.RateLimiting;

/// <summary>
/// Validates the <see cref="RateLimitSettings"/> so misconfiguration fails fast at startup.
/// Policies must be uniquely named, use a supported algorithm and partitioning strategy, carry
/// algorithm-appropriate limits, and — when the policy partitions by header — name the header.
/// The global policy must reference an existing policy and the rejection status code must be a
/// real client/server error status, so a bad setting surfaces at boot instead of as a silent
/// misconfiguration or an obscure runtime failure.
/// </summary>
public sealed class RateLimitSettingsValidator : IValidateOptions<RateLimitSettings>
{
    /// <inheritdoc/>
    public ValidateOptionsResult Validate(string? name, RateLimitSettings options)
    {
        var failures = new List<string>();

        if (!options.Enabled)
        {
            return ValidateOptionsResult.Success;
        }

        if (options.RejectionStatusCode is < 400 or > 599)
        {
            failures.Add(
                $"{RateLimitSettings.SectionName}:RejectionStatusCode must be a client or server error status code (400-599).");
        }

        var policyNames = new HashSet<string>(StringComparer.Ordinal);

        foreach (RateLimitPolicySettings policy in options.Policies)
        {
            ValidatePolicy(policy, failures, policyNames);
        }

        if (!string.IsNullOrWhiteSpace(options.GlobalPolicyName)
            && !policyNames.Contains(options.GlobalPolicyName))
        {
            failures.Add(
                $"{RateLimitSettings.SectionName}:GlobalPolicyName '{options.GlobalPolicyName}' does not match any configured policy name.");
        }

        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }

    private static void ValidatePolicy(
        RateLimitPolicySettings policy,
        List<string> failures,
        HashSet<string> policyNames)
    {
        string scope = $"{RateLimitSettings.SectionName}:Policies";

        if (string.IsNullOrWhiteSpace(policy.Name))
        {
            failures.Add($"{scope}: a policy name must not be empty.");
        }
        else if (!policyNames.Add(policy.Name))
        {
            failures.Add($"{scope}: policy name '{policy.Name}' is duplicated.");
        }

        if (!Enum.IsDefined(policy.Algorithm))
        {
            failures.Add($"{scope}:'{policy.Name}':Algorithm '{policy.Algorithm}' is not a supported rate limiting algorithm.");
        }

        if (!Enum.IsDefined(policy.PartitionBy))
        {
            failures.Add($"{scope}:'{policy.Name}':PartitionBy '{policy.PartitionBy}' is not a supported partitioning strategy.");
        }

        if (policy.PartitionBy == RateLimitingPartitioning.Header
            && string.IsNullOrWhiteSpace(policy.PartitionHeaderName))
        {
            failures.Add($"{scope}:'{policy.Name}':PartitionHeaderName is required when PartitionBy is Header.");
        }

        if (policy.Algorithm == RateLimitingAlgorithm.NoLimit)
        {
            return;
        }

        if (policy.PermitLimit <= 0
            && policy.Algorithm is RateLimitingAlgorithm.FixedWindow
                or RateLimitingAlgorithm.SlidingWindow
                or RateLimitingAlgorithm.Concurrency)
        {
            failures.Add($"{scope}:'{policy.Name}':PermitLimit must be greater than zero for {policy.Algorithm}.");
        }

        if (policy.WindowSeconds <= 0
            && policy.Algorithm is RateLimitingAlgorithm.FixedWindow
                or RateLimitingAlgorithm.SlidingWindow
                or RateLimitingAlgorithm.TokenBucket)
        {
            failures.Add($"{scope}:'{policy.Name}':WindowSeconds must be greater than zero for {policy.Algorithm}.");
        }

        if (policy.Algorithm == RateLimitingAlgorithm.SlidingWindow && policy.SegmentsPerWindow < 1)
        {
            failures.Add($"{scope}:'{policy.Name}':SegmentsPerWindow must be at least 1 for SlidingWindow.");
        }

        if (policy.Algorithm == RateLimitingAlgorithm.TokenBucket)
        {
            if (policy.TokenLimit <= 0)
            {
                failures.Add($"{scope}:'{policy.Name}':TokenLimit must be greater than zero for TokenBucket.");
            }

            if (policy.TokensPerPeriod <= 0)
            {
                failures.Add($"{scope}:'{policy.Name}':TokensPerPeriod must be greater than zero for TokenBucket.");
            }

            if (policy.ReplenishmentPeriodSeconds <= 0)
            {
                failures.Add($"{scope}:'{policy.Name}':ReplenishmentPeriodSeconds must be greater than zero for TokenBucket.");
            }
        }
    }
}
