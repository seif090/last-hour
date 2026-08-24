using FluentValidation;
using LastHour.Modules.Identity.Application;
using LastHour.Modules.Identity.Application.Commands;
using LastHour.Modules.Identity.Application.Persistence;
using LastHour.Modules.Identity.Application.RefreshTokens;
using LastHour.Modules.Identity.Domain.RefreshTokens;
using LastHour.Modules.Identity.Domain.Roles;
using LastHour.Modules.Identity.Domain.Users;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace LastHour.Modules.Identity.DependencyInjection;

/// <summary>
/// Registers the Identity module services with the dependency injection container. The module
/// holds no infrastructure of its own: its entity configuration is discovered by the shared
/// DbContext, its domain abstractions are implemented by the composition root, and the
/// password hashing provider comes from the shared infrastructure layer.
/// </summary>
public static class IdentityServiceCollectionExtensions
{
    /// <summary>
    /// Registers the Identity module: the password policy with startup validation and the
    /// credential service.
    /// </summary>
    /// <param name="services">The service collection to register into.</param>
    /// <param name="configuration">The application configuration used to bind options.</param>
    /// <returns>The same service collection, so calls can be chained.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="services"/> or
    /// <paramref name="configuration"/> is <see langword="null"/>.</exception>
    public static IServiceCollection AddLastHourIdentity(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddOptions<PasswordPolicy>()
            .Bind(configuration.GetSection(PasswordPolicy.SectionName))
            .ValidateOnStart();
        services.AddSingleton<IValidateOptions<PasswordPolicy>, PasswordPolicyValidator>();

        services.AddOptions<RefreshTokenOptions>()
            .Bind(configuration.GetSection(RefreshTokenOptions.SectionName))
            .ValidateOnStart();
        services.AddSingleton<IValidateOptions<RefreshTokenOptions>, RefreshTokenOptionsValidator>();

        services.AddOptions<AccountSecurityOptions>()
            .Bind(configuration.GetSection(AccountSecurityOptions.SectionName))
            .ValidateOnStart();
        services.AddSingleton<IValidateOptions<AccountSecurityOptions>, AccountSecurityOptionsValidator>();

        services.AddScoped<IPasswordCredentialService, PasswordCredentialService>();
        services.AddScoped<IAuthorizationBootstrapService, AuthorizationBootstrapService>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRoleRepository, RoleRepository>();
        services.AddSingleton<IRefreshTokenService, RefreshTokenService>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();

        // FluentValidation validators are consumed by the shared validation pipeline behavior;
        // registering the module assembly here keeps validation rules with their commands.
        services.AddValidatorsFromAssembly(typeof(RegisterUserCommand).Assembly);

        return services;
    }
}
