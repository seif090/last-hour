using System.Text;
using LastHour.BuildingBlocks.Infrastructure.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace LastHour.Api.Security.Authentication;

/// <summary>
/// Registers JWT bearer authentication and the authorization surface. Token validation is
/// delegated entirely to the framework's JWT bearer handler with deliberately strict settings —
/// issuer, audience, lifetime and signature are all validated, the clock skew stays small, and
/// inbound claim types are left unmapped so the raw <c>sub</c> claim is what endpoints read.
/// The validation parameters are derived from the same validated <see cref="JwtOptions"/> that
/// the token generator uses, so issuance and validation can never drift apart.
/// </summary>
public static class AuthenticationServiceCollectionExtensions
{
    /// <summary>
    /// Registers JWT bearer authentication (as the default scheme) and authorization.
    /// </summary>
    /// <param name="services">The service collection to register into.</param>
    /// <returns>The same service collection, so calls can be chained.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="services"/> is <see langword="null"/>.</exception>
    public static IServiceCollection AddLastHourAuthentication(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer();

        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<JwtOptions>>((bearerOptions, jwtOptions) =>
            {
                JwtOptions options = jwtOptions.Value;

                bearerOptions.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = options.Issuer,
                    ValidAudience = options.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.SigningKey)),
                    ClockSkew = options.ClockSkew,
                };

                // Keep the raw claim names ("sub", "jti") instead of mapping them to legacy
                // ClaimTypes constants; endpoints read the subject via the standard "sub" name.
                bearerOptions.MapInboundClaims = false;
            });

        services.AddAuthorization();

        return services;
    }
}
