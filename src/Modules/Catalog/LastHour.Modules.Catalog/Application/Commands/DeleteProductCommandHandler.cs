using LastHour.BuildingBlocks.Application.Cqrs;
using LastHour.BuildingBlocks.SharedKernel.Results;
using LastHour.Modules.Catalog.Domain.Products;
using Microsoft.Extensions.Logging;

namespace LastHour.Modules.Catalog.Application.Commands;

/// <summary>
/// Handles deleting a product from the catalog.
/// </summary>
public sealed class DeleteProductCommandHandler : ICommandHandler<DeleteProductCommand>
{
    private static readonly Action<ILogger, Guid, Exception?> ProductDeleted =
        LoggerMessage.Define<Guid>(
            LogLevel.Information,
            new EventId(4010, nameof(DeleteProductCommandHandler)),
            "Product {ProductId} marked for deletion.");

    private readonly IProductRepository _products;
    private readonly ILogger<DeleteProductCommandHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="DeleteProductCommandHandler"/> class.
    /// </summary>
    /// <param name="products">The product repository.</param>
    /// <param name="logger">The logger instance.</param>
    /// <exception cref="ArgumentNullException">Any argument is <see langword="null"/>.</exception>
    public DeleteProductCommandHandler(
        IProductRepository products,
        ILogger<DeleteProductCommandHandler> logger)
    {
        ArgumentNullException.ThrowIfNull(products);
        ArgumentNullException.ThrowIfNull(logger);
        _products = products;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<Result> Handle(DeleteProductCommand command, CancellationToken cancellationToken)
    {
        ProductId productId = ProductId.Create(command.ProductId);
        Product? product = await _products.GetByIdTrackedAsync(productId, cancellationToken).ConfigureAwait(false);

        if (product is null)
        {
            return Result.Failure(Error.NotFound(
                "Products.NotFound",
                "The product could not be found."));
        }

        _products.Remove(product);

        ProductDeleted(_logger, product.Id.Value, null);
        return Result.Success();
    }
}
