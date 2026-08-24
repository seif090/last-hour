using LastHour.BuildingBlocks.Application.Cqrs;
using LastHour.BuildingBlocks.SharedKernel.Results;
using LastHour.Modules.Catalog.Application.Models;
using LastHour.Modules.Catalog.Domain.Products;

namespace LastHour.Modules.Catalog.Application.Queries.Products;

/// <summary>
/// Resolves a product and projects it onto a safe <see cref="ProductDto"/>. Reads run
/// no-tracking and respect the global soft-delete query filter, so a missing or soft-deleted
/// product yields a not-found result.
/// </summary>
public sealed class GetProductByIdQueryHandler : IQueryHandler<GetProductByIdQuery, ProductDto>
{
    private readonly IProductRepository _products;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetProductByIdQueryHandler"/> class.
    /// </summary>
    /// <param name="products">The product repository used to resolve the product.</param>
    /// <exception cref="ArgumentNullException"><paramref name="products"/> is <see langword="null"/>.</exception>
    public GetProductByIdQueryHandler(IProductRepository products)
    {
        ArgumentNullException.ThrowIfNull(products);
        _products = products;
    }

    /// <inheritdoc/>
    public async Task<Result<ProductDto>> Handle(
        GetProductByIdQuery query,
        CancellationToken cancellationToken)
    {
        ProductId productId = ProductId.Create(query.ProductId);
        Product? product = await _products
            .GetByIdAsync(productId, cancellationToken)
            .ConfigureAwait(false);

        if (product is null)
        {
            return Result<ProductDto>.Failure(Error.NotFound(
                "Products.NotFound",
                "The product could not be found."));
        }

        return Result<ProductDto>.Success(new ProductDto(
            product.Id.Value,
            product.Sku.Value,
            product.Name,
            product.Description,
            product.BasePrice.Amount,
            product.BasePrice.Currency,
            product.CategoryId.Value));
    }
}
