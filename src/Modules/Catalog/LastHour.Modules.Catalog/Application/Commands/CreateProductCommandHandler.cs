using LastHour.BuildingBlocks.Application.Cqrs;
using LastHour.BuildingBlocks.SharedKernel.Results;
using LastHour.Modules.Catalog.Domain.Categories;
using LastHour.Modules.Catalog.Domain.Products;
using Microsoft.Extensions.Logging;

namespace LastHour.Modules.Catalog.Application.Commands;

/// <summary>
/// Handles the creation of a new product aggregate root.
/// </summary>
public sealed class CreateProductCommandHandler : ICommandHandler<CreateProductCommand, ProductId>
{
    private static readonly Action<ILogger, Guid, string, Exception?> ProductCreated =
        LoggerMessage.Define<Guid, string>(
            LogLevel.Information,
            new EventId(4003, nameof(CreateProductCommandHandler)),
            "Product {ProductId} created with SKU {Sku}.");

    private static readonly Action<ILogger, Exception?> DuplicateSkuRejected =
        LoggerMessage.Define(
            LogLevel.Warning,
            new EventId(4004, nameof(CreateProductCommandHandler)),
            "Product creation rejected because the normalized SKU already exists.");

    private readonly IProductRepository _products;
    private readonly ICategoryRepository _categories;
    private readonly ILogger<CreateProductCommandHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="CreateProductCommandHandler"/> class.
    /// </summary>
    /// <param name="products">The product repository.</param>
    /// <param name="categories">The category repository.</param>
    /// <param name="logger">The logger instance.</param>
    /// <exception cref="ArgumentNullException">Any argument is <see langword="null"/>.</exception>
    public CreateProductCommandHandler(
        IProductRepository products,
        ICategoryRepository categories,
        ILogger<CreateProductCommandHandler> logger)
    {
        ArgumentNullException.ThrowIfNull(products);
        ArgumentNullException.ThrowIfNull(categories);
        ArgumentNullException.ThrowIfNull(logger);
        _products = products;
        _categories = categories;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<Result<ProductId>> Handle(CreateProductCommand command, CancellationToken cancellationToken)
    {
        CategoryId categoryId = CategoryId.Create(command.CategoryId);
        if (!await _categories.ExistsByIdAsync(categoryId, cancellationToken).ConfigureAwait(false))
        {
            return Result<ProductId>.Failure(Error.NotFound(
                "Categories.NotFound",
                "The category could not be found."));
        }

        Sku sku;
        try
        {
            sku = Sku.From(command.Sku);
        }
        catch (InvalidSkuException exception)
        {
            return Result<ProductId>.Failure(
                Error.Validation("InvalidSku", exception.Message));
        }

        if (await _products.ExistsByNormalizedSkuAsync(sku.NormalizedValue, cancellationToken).ConfigureAwait(false))
        {
            DuplicateSkuRejected(_logger, null);
            return Result<ProductId>.Failure(Error.Conflict(
                "Products.SkuAlreadyExists",
                "A product with this SKU already exists."));
        }

        Money basePrice;
        try
        {
            basePrice = Money.Create(command.PriceAmount, command.PriceCurrency);
        }
        catch (InvalidPriceException exception)
        {
            return Result<ProductId>.Failure(
                Error.Validation("InvalidPrice", exception.Message));
        }

        Product product;
        try
        {
            product = Product.Create(sku, command.Name, basePrice, categoryId, command.Description);
        }
        catch (InvalidProductNameException exception)
        {
            return Result<ProductId>.Failure(
                Error.Validation("InvalidProductName", exception.Message));
        }

        _products.Add(product);

        ProductCreated(_logger, product.Id.Value, product.Sku.NormalizedValue, null);
        return Result<ProductId>.Success(product.Id);
    }
}
