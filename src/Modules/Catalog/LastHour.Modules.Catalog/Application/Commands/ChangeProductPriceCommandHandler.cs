using LastHour.BuildingBlocks.Application.Cqrs;
using LastHour.BuildingBlocks.SharedKernel.Results;
using LastHour.Modules.Catalog.Domain.Products;
using Microsoft.Extensions.Logging;

namespace LastHour.Modules.Catalog.Application.Commands;

/// <summary>
/// Handles changing the base price of an existing product.
/// </summary>
public sealed class ChangeProductPriceCommandHandler : ICommandHandler<ChangeProductPriceCommand>
{
    private static readonly Action<ILogger, Guid, decimal, string, Exception?> ProductPriceChanged =
        LoggerMessage.Define<Guid, decimal, string>(
            LogLevel.Information,
            new EventId(4006, nameof(ChangeProductPriceCommandHandler)),
            "Price changed for product {ProductId} to {Amount} {Currency}.");

    private readonly IProductRepository _products;
    private readonly ILogger<ChangeProductPriceCommandHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ChangeProductPriceCommandHandler"/> class.
    /// </summary>
    /// <param name="products">The product repository.</param>
    /// <param name="logger">The logger instance.</param>
    /// <exception cref="ArgumentNullException">Any argument is <see langword="null"/>.</exception>
    public ChangeProductPriceCommandHandler(
        IProductRepository products,
        ILogger<ChangeProductPriceCommandHandler> logger)
    {
        ArgumentNullException.ThrowIfNull(products);
        ArgumentNullException.ThrowIfNull(logger);
        _products = products;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<Result> Handle(ChangeProductPriceCommand command, CancellationToken cancellationToken)
    {
        ProductId productId = ProductId.Create(command.ProductId);
        Product? product = await _products.GetByIdTrackedAsync(productId, cancellationToken).ConfigureAwait(false);

        if (product is null)
        {
            return Result.Failure(Error.NotFound(
                "Products.NotFound",
                "The product could not be found."));
        }

        Money newPrice;
        try
        {
            newPrice = Money.Create(command.Amount, command.Currency);
        }
        catch (InvalidPriceException exception)
        {
            return Result.Failure(
                Error.Validation("InvalidPrice", exception.Message));
        }

        product.ChangePrice(newPrice);

        ProductPriceChanged(_logger, product.Id.Value, newPrice.Amount, newPrice.Currency, null);
        return Result.Success();
    }
}
