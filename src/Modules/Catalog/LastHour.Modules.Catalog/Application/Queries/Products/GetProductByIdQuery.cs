using LastHour.BuildingBlocks.Application.Cqrs;
using LastHour.Modules.Catalog.Application.Models;

namespace LastHour.Modules.Catalog.Application.Queries.Products;

/// <summary>
/// Retrieves a single product by its identifier.
/// </summary>
public sealed record GetProductByIdQuery : IQuery<ProductDto>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GetProductByIdQuery"/> class.
    /// </summary>
    /// <param name="productId">The product identifier to look up.</param>
    public GetProductByIdQuery(Guid productId)
    {
        ProductId = productId;
    }

    /// <summary>
    /// Gets the product identifier to look up.
    /// </summary>
    public Guid ProductId { get; }
}
