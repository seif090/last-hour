using LastHour.BuildingBlocks.Application.Cqrs;

namespace LastHour.Modules.Catalog.Application.Commands;

/// <summary>
/// Changes the SKU of an existing product.
/// </summary>
public sealed record ChangeProductSkuCommand : ICommand
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ChangeProductSkuCommand"/> class.
    /// </summary>
    /// <param name="productId">The product identifier.</param>
    /// <param name="sku">The new SKU.</param>
    public ChangeProductSkuCommand(Guid productId, string sku)
    {
        ProductId = productId;
        Sku = sku;
    }

    /// <summary>
    /// Gets the product identifier.
    /// </summary>
    public Guid ProductId { get; }

    /// <summary>
    /// Gets the new SKU.
    /// </summary>
    public string Sku { get; }
}
