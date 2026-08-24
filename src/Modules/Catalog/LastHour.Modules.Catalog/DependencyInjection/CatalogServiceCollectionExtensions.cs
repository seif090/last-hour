using FluentValidation;
using LastHour.Modules.Catalog.Application.Commands;
using LastHour.Modules.Catalog.Application.Persistence;
using LastHour.Modules.Catalog.Domain.Categories;
using LastHour.Modules.Catalog.Domain.Products;
using Microsoft.Extensions.DependencyInjection;

namespace LastHour.Modules.Catalog.DependencyInjection;

/// <summary>
/// Registers Catalog module services with the dependency injection container.
/// </summary>
public static class CatalogServiceCollectionExtensions
{
    /// <summary>
    /// Registers the Catalog module repositories and validators.
    /// </summary>
    /// <param name="services">The service collection to register into.</param>
    /// <returns>The same service collection, so calls can be chained.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="services"/> is <see langword="null"/>.</exception>
    public static IServiceCollection AddLastHourCatalog(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddScoped<ICategoryRepository, CategoryRepository>();
        services.AddScoped<IProductRepository, ProductRepository>();

        services.AddValidatorsFromAssembly(typeof(CreateCategoryCommand).Assembly);

        return services;
    }
}
