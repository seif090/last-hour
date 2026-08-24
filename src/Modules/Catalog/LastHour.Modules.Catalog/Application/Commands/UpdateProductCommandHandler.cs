using LastHour.BuildingBlocks.Application.Cqrs;
using LastHour.BuildingBlocks.SharedKernel.Results;
using LastHour.Modules.Catalog.Domain.Products;
using Microsoft.Extensions.Logging;

namespace LastHour.Modules.Catalog.Application.Commands;

/// <summary>
/// Handles updating the descriptive details of an existing product.
/// </summary>
public sealed class UpdateProductCommandHandler : ICommandHandler<UpdateProductCommand>
{
    private static readonly Action<ILogger, Guid, Exception?> ProductUpdated =
        LoggerMessage.Define<Guid>(
            LogLevel.Information,
            new EventId(4005, nameof(UpdateProductCommandHandler)),
            "Product {ProductId} updated.");

    private readonly IProductRepository _products;
    private readonly ILogger<UpdateProductCommandHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateProductCommandHandler"/> class.
    /// </summary>
    /// <param name="products">The product repository.</param>
    /// <param name="logger">The logger instance.</param>
    /// <exception cref="ArgumentNullException">Any argument is <see langword="null"/>.</exception>
    public UpdateProductCommandHandler(
        IProductRepository products,
        ILogger<UpdateProductCommandHandler> logger)
    {
        ArgumentNullException.ThrowIfNull(products);
        ArgumentNullException.ThrowIfNull(logger);
        _products = products;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<Result> Handle(UpdateProductCommand command, CancellationToken cancellationToken)
    {
        ProductId productId = ProductId.Create(command.Id);
        Product? product = await _products.GetByIdTrackedAsync(productId, cancellationToken).ConfigureAwait(false);

        if (product is null)
        {
            return Result.Failure(Error.NotFound(
                "Products.NotFound",
                "The product could not be found."));
        }

        try
        {
            product.UpdateDetails(command.Name, command.Description, product.CategoryId);
        }
        catch (InvalidProductNameException exception)
        {
            return Result.Failure(
                Error.Validation("InvalidProductName", exception.Message));
        }

        ProductUpdated(_logger, product.Id.Value, null);
        return Result.Success();
    }
}
