using System.Text;
using LastHour.Api.Security.Authorization;
using LastHour.BuildingBlocks.Application.Contracts;
using LastHour.BuildingBlocks.Infrastructure.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
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
///
/// Authorization is wired on top of the validated identity: the permission policy provider
/// turns <c>Permission.&lt;code&gt;</c> policies into <see cref="PermissionRequirement"/>s, the
/// permission handler resolves the caller's live permission set through the identity module's
/// repository, and the result handler renders consistent 403 problem details while preserving
/// the framework's 401 challenge. The HTTP-aware <see cref="HttpContextCurrentUser"/> also
/// overrides the unauthenticated infrastructure default inside the API host.
/// </summary>
public static class AuthenticationServiceCollectionExtensions
{
    /// <summary>
    /// Registers JWT bearer authentication (as the default scheme), the permission-based
    /// authorization surface, and the HTTP-aware current-user identity.
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

        // Permission policies: Permission.<code> -> PermissionRequirement. Registered after
        // AddAuthorization so it replaces the default policy provider.
        services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();

        // The handler resolves the caller's effective permissions through the scoped role
        // repository, so it must be scoped with the request's unit of work.
        services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();

        // Consistent 403 problem details while the framework challenge keeps its 401 shape.
        services.AddSingleton<IAuthorizationMiddlewareResultHandler, AuthorizationProblemDetailsResultHandler>();

        // HTTP-aware current user: the API host overrides the unauthenticated infrastructure
        // default so application-layer code sees the real caller during a request.
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, HttpContextCurrentUser>();

        return services;
    }
}
