using Microsoft.Extensions.Options;

namespace LastHour.BuildingBlocks.Infrastructure.Security;

/// <summary>
/// Validates the <see cref="JwtOptions"/> at startup so misconfigured token issuance or
/// validation fails fast: the issuer and audience must be declared, the signing key must be
/// present and long enough for HS256, the access-token lifetime must be positive and bounded,
/// and the clock skew must be non-negative and small. An empty key is a configuration error,
/// never a reason to fall back to an ephemeral or unsigned token.
/// </summary>
public sealed class JwtOptionsValidator : IValidateOptions<JwtOptions>
{
    /// <inheritdoc/>
    public ValidateOptionsResult Validate(string? name, JwtOptions options)
    {
        var failures = new List<string>();

        if (string.IsNullOrWhiteSpace(options.Issuer))
        {
            failures.Add("Jwt:Issuer is required.");
        }

        if (string.IsNullOrWhiteSpace(options.Audience))
        {
            failures.Add("Jwt:Audience is required.");
        }

        if (string.IsNullOrWhiteSpace(options.SigningKey))
        {
            failures.Add("Jwt:SigningKey is required. Provide it through the secrets surface (for example the LASTHOUR_SECRET_JWT_SIGNINGKEY environment variable), never in source control.");
        }
        else if (options.SigningKey.Length < JwtOptions.MinimumSigningKeyLength)
        {
            failures.Add($"Jwt:SigningKey must be at least {JwtOptions.MinimumSigningKeyLength} characters for HS256.");
        }

        if (options.AccessTokenLifetime <= TimeSpan.Zero)
        {
            failures.Add("Jwt:AccessTokenLifetime must be positive.");
        }
        else if (options.AccessTokenLifetime > JwtOptions.MaximumAccessTokenLifetime)
        {
            failures.Add($"Jwt:AccessTokenLifetime cannot exceed {JwtOptions.MaximumAccessTokenLifetime}.");
        }

        if (options.ClockSkew < TimeSpan.Zero)
        {
            failures.Add("Jwt:ClockSkew cannot be negative.");
        }
        else if (options.ClockSkew > JwtOptions.MaximumClockSkew)
        {
            failures.Add($"Jwt:ClockSkew cannot exceed {JwtOptions.MaximumClockSkew}.");
        }

        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }
}
