using LastHour.BuildingBlocks.Application.Cqrs;

namespace LastHour.Modules.Catalog.Application.Commands;

/// <summary>
/// Changes the base price of an existing product.
/// </summary>
public sealed record ChangeProductPriceCommand : ICommand
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ChangeProductPriceCommand"/> class.
    /// </summary>
    /// <param name="productId">The product identifier.</param>
    /// <param name="amount">The new price amount.</param>
    /// <param name="currency">The ISO 4217 currency code.</param>
    public ChangeProductPriceCommand(Guid productId, decimal amount, string currency)
    {
        ProductId = productId;
        Amount = amount;
        Currency = currency;
    }

    /// <summary>
    /// Gets the product identifier.
    /// </summary>
    public Guid ProductId { get; }

    /// <summary>
    /// Gets the new price amount.
    /// </summary>
    public decimal Amount { get; }

    /// <summary>
    /// Gets the ISO 4217 currency code.
    /// </summary>
    public string Currency { get; }
}
