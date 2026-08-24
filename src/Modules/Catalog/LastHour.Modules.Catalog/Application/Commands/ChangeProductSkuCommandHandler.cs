using LastHour.BuildingBlocks.Application.Cqrs;
using LastHour.BuildingBlocks.SharedKernel.Results;
using LastHour.Modules.Catalog.Domain.Products;
using Microsoft.Extensions.Logging;

namespace LastHour.Modules.Catalog.Application.Commands;

/// <summary>
/// Handles changing the SKU of an existing product.
/// </summary>
public sealed class ChangeProductSkuCommandHandler : ICommandHandler<ChangeProductSkuCommand>
{
    private static readonly Action<ILogger, Guid, string, Exception?> ProductSkuChanged =
        LoggerMessage.Define<Guid, string>(
            LogLevel.Information,
            new EventId(4007, nameof(ChangeProductSkuCommandHandler)),
            "SKU changed for product {ProductId} to {Sku}.");

    private static readonly Action<ILogger, Exception?> DuplicateSkuRejected =
        LoggerMessage.Define(
            LogLevel.Warning,
            new EventId(4008, nameof(ChangeProductSkuCommandHandler)),
            "Product SKU change rejected because the normalized SKU already exists.");

    private readonly IProductRepository _products;
    private readonly ILogger<ChangeProductSkuCommandHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ChangeProductSkuCommandHandler"/> class.
    /// </summary>
    /// <param name="products">The product repository.</param>
    /// <param name="logger">The logger instance.</param>
    /// <exception cref="ArgumentNullException">Any argument is <see langword="null"/>.</exception>
    public ChangeProductSkuCommandHandler(
        IProductRepository products,
        ILogger<ChangeProductSkuCommandHandler> logger)
    {
        ArgumentNullException.ThrowIfNull(products);
        ArgumentNullException.ThrowIfNull(logger);
        _products = products;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<Result> Handle(ChangeProductSkuCommand command, CancellationToken cancellationToken)
    {
        ProductId productId = ProductId.Create(command.ProductId);
        Product? product = await _products.GetByIdTrackedAsync(productId, cancellationToken).ConfigureAwait(false);

        if (product is null)
        {
            return Result.Failure(Error.NotFound(
                "Products.NotFound",
                "The product could not be found."));
        }

        Sku newSku;
        try
        {
            newSku = Sku.From(command.Sku);
        }
        catch (InvalidSkuException exception)
        {
            return Result.Failure(
                Error.Validation("InvalidSku", exception.Message));
        }

        if (string.Equals(product.Sku.NormalizedValue, newSku.NormalizedValue, StringComparison.Ordinal))
        {
            return Result.Success();
        }

        if (await _products.ExistsByNormalizedSkuAsync(newSku.NormalizedValue, cancellationToken).ConfigureAwait(false))
        {
            DuplicateSkuRejected(_logger, null);
            return Result.Failure(Error.Conflict(
                "Products.SkuAlreadyExists",
                "A product with this SKU already exists."));
        }

        product.ChangeSku(newSku);

        ProductSkuChanged(_logger, product.Id.Value, newSku.NormalizedValue, null);
        return Result.Success();
    }
}
