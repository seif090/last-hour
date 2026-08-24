using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace LastHour.Api.Security.Authorization;

/// <summary>
/// Maps the <c>Permission.&lt;code&gt;</c> policy convention onto a
/// <see cref="PermissionRequirement"/>. Every catalog code is automatically a policy with zero
/// per-permission registration, so the policy surface can never drift from the centralized
/// permission catalog; an unknown code still resolves to a requirement that simply never
/// matches a user's permission set, so a mistyped attribute fails closed. Any policy name
/// outside the convention is delegated to the framework's default provider untouched.
/// </summary>
public sealed class PermissionPolicyProvider : IAuthorizationPolicyProvider
{
    private readonly DefaultAuthorizationPolicyProvider _defaultProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="PermissionPolicyProvider"/> class.
    /// </summary>
    /// <param name="options">The authorization options the default provider is built from.</param>
    /// <exception cref="ArgumentNullException"><paramref name="options"/> is <see langword="null"/>.</exception>
    public PermissionPolicyProvider(IOptions<AuthorizationOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);
        _defaultProvider = new DefaultAuthorizationPolicyProvider(options);
    }

    /// <inheritdoc/>
    public Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        if (policyName.StartsWith(RequirePermissionAttribute.PolicyPrefix, StringComparison.Ordinal))
        {
            string permission = policyName[RequirePermissionAttribute.PolicyPrefix.Length..];

            var policy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .AddRequirements(new PermissionRequirement(permission))
                .Build();

            return Task.FromResult<AuthorizationPolicy?>(policy);
        }

        return _defaultProvider.GetPolicyAsync(policyName);
    }

    /// <inheritdoc/>
    public Task<AuthorizationPolicy> GetDefaultPolicyAsync()
        => _defaultProvider.GetDefaultPolicyAsync();

    /// <inheritdoc/>
    public Task<AuthorizationPolicy?> GetFallbackPolicyAsync()
        => _defaultProvider.GetFallbackPolicyAsync();
}
