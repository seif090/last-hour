using LastHour.BuildingBlocks.Application.Cqrs;

namespace LastHour.Modules.Catalog.Application.Commands;

/// <summary>
/// Deletes a product from the catalog.
/// </summary>
public sealed record DeleteProductCommand : ICommand
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DeleteProductCommand"/> class.
    /// </summary>
    /// <param name="productId">The product identifier.</param>
    public DeleteProductCommand(Guid productId)
    {
        ProductId = productId;
    }

    /// <summary>
    /// Gets the product identifier.
    /// </summary>
    public Guid ProductId { get; }
}
