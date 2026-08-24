using LastHour.BuildingBlocks.Application.Cqrs;
using LastHour.BuildingBlocks.SharedKernel.Results;
using LastHour.Modules.Catalog.Domain.Categories;
using LastHour.Modules.Catalog.Domain.Products;
using Microsoft.Extensions.Logging;

namespace LastHour.Modules.Catalog.Application.Commands;

/// <summary>
/// Handles changing the category of an existing product.
/// </summary>
public sealed class ChangeProductCategoryCommandHandler : ICommandHandler<ChangeProductCategoryCommand>
{
    private static readonly Action<ILogger, Guid, Guid, Exception?> ProductCategoryChanged =
        LoggerMessage.Define<Guid, Guid>(
            LogLevel.Information,
            new EventId(4009, nameof(ChangeProductCategoryCommandHandler)),
            "Category changed for product {ProductId} to category {CategoryId}.");

    private readonly IProductRepository _products;
    private readonly ICategoryRepository _categories;
    private readonly ILogger<ChangeProductCategoryCommandHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ChangeProductCategoryCommandHandler"/> class.
    /// </summary>
    /// <param name="products">The product repository.</param>
    /// <param name="categories">The category repository.</param>
    /// <param name="logger">The logger instance.</param>
    /// <exception cref="ArgumentNullException">Any argument is <see langword="null"/>.</exception>
    public ChangeProductCategoryCommandHandler(
        IProductRepository products,
        ICategoryRepository categories,
        ILogger<ChangeProductCategoryCommandHandler> logger)
    {
        ArgumentNullException.ThrowIfNull(products);
        ArgumentNullException.ThrowIfNull(categories);
        ArgumentNullException.ThrowIfNull(logger);
        _products = products;
        _categories = categories;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<Result> Handle(ChangeProductCategoryCommand command, CancellationToken cancellationToken)
    {
        ProductId productId = ProductId.Create(command.ProductId);
        Product? product = await _products.GetByIdTrackedAsync(productId, cancellationToken).ConfigureAwait(false);

        if (product is null)
        {
            return Result.Failure(Error.NotFound(
                "Products.NotFound",
                "The product could not be found."));
        }

        CategoryId newCategoryId = CategoryId.Create(command.CategoryId);

        if (product.CategoryId == newCategoryId)
        {
            return Result.Success();
        }

        if (!await _categories.ExistsByIdAsync(newCategoryId, cancellationToken).ConfigureAwait(false))
        {
            return Result.Failure(Error.NotFound(
                "Categories.NotFound",
                "The category could not be found."));
        }

        product.ChangeCategory(newCategoryId);

        ProductCategoryChanged(_logger, product.Id.Value, newCategoryId.Value, null);
        return Result.Success();
    }
}
