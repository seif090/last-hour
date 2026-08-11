using System.Globalization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;

namespace LastHour.Api.RateLimiting;

/// <summary>
/// Registers the rate limiting surface: named policies bound from the
/// <see cref="RateLimitSettings.SectionName"/> configuration section, one of which can be
/// selected as the global limiter applied to every request.
/// </summary>
public static class RateLimitingServiceCollectionExtensions
{
    /// <summary>
    /// Registers rate limiting and binds the named policies from configuration. Every policy is
    /// available through <c>RequireRateLimiting(policyName)</c> on an endpoint; the policy named
    /// by <see cref="RateLimitSettings.GlobalPolicyName"/> is additionally applied to every
    /// request before the endpoint-specific limiter. Rejected requests receive a
    /// <c>application/problem+json</c> body with a <c>Retry-After</c> hint when the limiter
    /// provides one.
    /// </summary>
    /// <param name="services">The service collection to register into.</param>
    /// <param name="configuration">The configuration to bind the settings from.</param>
    /// <returns>The same service collection, so calls can be chained.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="services"/> or <paramref name="configuration"/> is <see langword="null"/>.</exception>
    public static IServiceCollection AddLastHourRateLimiting(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        RateLimitSettings settings =
            configuration.GetSection(RateLimitSettings.SectionName).Get<RateLimitSettings>() ?? new RateLimitSettings();

        services.AddOptions<RateLimitSettings>()
            .Bind(configuration.GetSection(RateLimitSettings.SectionName))
            .ValidateOnStart();
        services.AddSingleton<IValidateOptions<RateLimitSettings>, RateLimitSettingsValidator>();

        if (settings.Enabled)
        {
            services.AddRateLimiter(options =>
            {
                options.RejectionStatusCode = settings.RejectionStatusCode;
                options.OnRejected = OnRejectedAsync;

                foreach (RateLimitPolicySettings policy in settings.Policies)
                {
                    if (string.IsNullOrWhiteSpace(policy.Name))
                    {
                        continue;
                    }

                    options.AddPolicy<string>(policy.Name, context => CreatePartition(policy, context));
                }

                if (!string.IsNullOrWhiteSpace(settings.GlobalPolicyName))
                {
                    RateLimitPolicySettings? globalPolicy = settings.Policies.FirstOrDefault(policy => policy.Name == settings.GlobalPolicyName);
                    if (globalPolicy is not null)
                    {
                        options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context => CreatePartition(globalPolicy, context));
                    }
                }
            });
        }

        return services;
    }

    private static RateLimitPartition<string> CreatePartition(RateLimitPolicySettings policy, HttpContext context)
    {
        string partitionKey = ResolvePartitionKey(policy, context);

        return policy.Algorithm switch
        {
            RateLimitingAlgorithm.FixedWindow => RateLimitPartition.GetFixedWindowLimiter(partitionKey, _ => CreateFixedWindowOptions(policy)),
            RateLimitingAlgorithm.SlidingWindow => RateLimitPartition.GetSlidingWindowLimiter(partitionKey, _ => CreateSlidingWindowOptions(policy)),
            RateLimitingAlgorithm.TokenBucket => RateLimitPartition.GetTokenBucketLimiter(partitionKey, _ => CreateTokenBucketOptions(policy)),
            RateLimitingAlgorithm.Concurrency => RateLimitPartition.GetConcurrencyLimiter(partitionKey, _ => CreateConcurrencyOptions(policy)),
            RateLimitingAlgorithm.NoLimit => RateLimitPartition.GetNoLimiter(partitionKey),
            _ => throw new ArgumentOutOfRangeException(nameof(policy), policy, "Unsupported rate limiting algorithm."),
        };
    }

    private static string ResolvePartitionKey(RateLimitPolicySettings policy, HttpContext context)
    {
        return policy.PartitionBy switch
        {
            RateLimitingPartitioning.Global => "global",
            RateLimitingPartitioning.IpAddress => context.Connection.RemoteIpAddress?.ToString() ?? "unknown-ip",
            RateLimitingPartitioning.Header => ResolveHeaderPartitionKey(policy.PartitionHeaderName, context.Request.Headers),
            RateLimitingPartitioning.Path => context.Request.Path.ToString(),
            RateLimitingPartitioning.Host => context.Request.Host.ToString(),
            _ => "global",
        };
    }

    private static string ResolveHeaderPartitionKey(string? headerName, IHeaderDictionary headers)
    {
        if (string.IsNullOrWhiteSpace(headerName))
        {
            return "unknown-header";
        }

        string value = headers[headerName].ToString();
        return string.IsNullOrEmpty(value) ? "unknown-header" : value;
    }

    private static FixedWindowRateLimiterOptions CreateFixedWindowOptions(RateLimitPolicySettings policy)
    {
        return new FixedWindowRateLimiterOptions
        {
            PermitLimit = policy.PermitLimit,
            Window = TimeSpan.FromSeconds(policy.WindowSeconds),
            QueueLimit = policy.QueueLimit,
            AutoReplenishment = policy.AutoReplenishment,
        };
    }

    private static SlidingWindowRateLimiterOptions CreateSlidingWindowOptions(RateLimitPolicySettings policy)
    {
        return new SlidingWindowRateLimiterOptions
        {
            PermitLimit = policy.PermitLimit,
            Window = TimeSpan.FromSeconds(policy.WindowSeconds),
            SegmentsPerWindow = policy.SegmentsPerWindow,
            QueueLimit = policy.QueueLimit,
            AutoReplenishment = policy.AutoReplenishment,
        };
    }

    private static TokenBucketRateLimiterOptions CreateTokenBucketOptions(RateLimitPolicySettings policy)
    {
        return new TokenBucketRateLimiterOptions
        {
            TokenLimit = policy.TokenLimit,
            TokensPerPeriod = policy.TokensPerPeriod,
            ReplenishmentPeriod = TimeSpan.FromSeconds(policy.ReplenishmentPeriodSeconds),
            QueueLimit = policy.QueueLimit,
            AutoReplenishment = policy.AutoReplenishment,
        };
    }

    private static ConcurrencyLimiterOptions CreateConcurrencyOptions(RateLimitPolicySettings policy)
    {
        return new ConcurrencyLimiterOptions
        {
            PermitLimit = policy.PermitLimit,
            QueueLimit = policy.QueueLimit,
        };
    }

    private static async ValueTask OnRejectedAsync(OnRejectedContext context, CancellationToken cancellationToken)
    {
        HttpResponse response = context.HttpContext.Response;
        response.ContentType = "application/problem+json";

        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out TimeSpan retryAfter))
        {
            response.Headers.RetryAfter = retryAfter.TotalSeconds.ToString("0", CultureInfo.InvariantCulture);
        }

        await response.WriteAsJsonAsync(
            value: new
            {
                type = "https://tools.ietf.org/html/rfc6585#section-4",
                title = "Too Many Requests",
                status = response.StatusCode,
            },
            options: null,
            contentType: "application/problem+json",
            cancellationToken: cancellationToken);
    }
}
