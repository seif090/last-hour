using FluentValidation;
using LastHour.Modules.Merchant.Application.Commands;
using LastHour.Modules.Merchant.Application.Persistence;
using LastHour.Modules.Merchant.Domain.Merchants;
using Microsoft.Extensions.DependencyInjection;

namespace LastHour.Modules.Merchant.DependencyInjection;

/// <summary>
/// Registers Merchant module services with the dependency injection container.
/// </summary>
public static class MerchantServiceCollectionExtensions
{
    /// <summary>
    /// Registers the Merchant module repositories and validators.
    /// </summary>
    /// <param name="services">The service collection to register into.</param>
    /// <returns>The same service collection, so calls can be chained.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="services"/> is <see langword="null"/>.</exception>
    public static IServiceCollection AddLastHourMerchant(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddScoped<IMerchantRepository, MerchantRepository>();

        services.AddValidatorsFromAssembly(typeof(RegisterMerchantCommand).Assembly);

        return services;
    }
}
