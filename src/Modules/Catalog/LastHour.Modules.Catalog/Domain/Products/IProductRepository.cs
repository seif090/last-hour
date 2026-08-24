using LastHour.BuildingBlocks.Application.Contracts;
using LastHour.Modules.Catalog.Domain.Categories;

namespace LastHour.Modules.Catalog.Domain.Products;

/// <summary>
/// The persistence contract of the <see cref="Product"/> aggregate.
/// </summary>
public interface IProductRepository : IRepository<Product>
{
    /// <summary>
    /// Retrieves the product with the specified identifier, or <see langword="null"/> when no such product exists.
    /// </summary>
    /// <param name="id">The product identifier to look up.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>The matching product, or <see langword="null"/> when not found.</returns>
    Task<Product?> GetByIdAsync(ProductId id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves the product with the specified identifier and places the aggregate in the change tracker,
    /// or <see langword="null"/> when no such product exists.
    /// </summary>
    /// <param name="id">The product identifier to look up.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>The matching, tracked product, or <see langword="null"/> when not found.</returns>
    Task<Product?> GetByIdTrackedAsync(ProductId id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Determines whether a product with the specified identifier exists.
    /// </summary>
    /// <param name="id">The product identifier to check.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns><see langword="true"/> when the product exists; otherwise <see langword="false"/>.</returns>
    Task<bool> ExistsByIdAsync(ProductId id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Determines whether a product with the specified normalized SKU exists.
    /// </summary>
    /// <param name="normalizedSku">The normalized SKU to check.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns><see langword="true"/> when a product with the normalized SKU exists; otherwise <see langword="false"/>.</returns>
    Task<bool> ExistsByNormalizedSkuAsync(string normalizedSku, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves the product with the specified normalized SKU, or <see langword="null"/> when no such product exists.
    /// </summary>
    /// <param name="normalizedSku">The normalized SKU to look up.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>The matching product, or <see langword="null"/> when not found.</returns>
    Task<Product?> GetByNormalizedSkuAsync(string normalizedSku, CancellationToken cancellationToken = default);

    /// <summary>
    /// Counts the products matching the search criteria. An empty or whitespace-only search term
    /// acts as no filter. Soft-deleted products are always excluded.
    /// </summary>
    /// <param name="searchTerm">An optional term matched against the product name and normalized SKU.</param>
    /// <param name="categoryId">An optional category identifier that restricts the results.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>The number of matching products.</returns>
    Task<int> CountAsync(
        string? searchTerm,
        CategoryId? categoryId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches products using the given criteria, returning a single ordered page. An empty or
    /// whitespace-only search term acts as no filter. Soft-deleted products are always excluded.
    /// </summary>
    /// <param name="searchTerm">An optional term matched against the product name and normalized SKU.</param>
    /// <param name="categoryId">An optional category identifier that restricts the results.</param>
    /// <param name="sortBy">The product attribute to order by.</param>
    /// <param name="sortDirection">The ordering direction.</param>
    /// <param name="pageNumber">The one-based page number.</param>
    /// <param name="pageSize">The number of items per page.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>The matching, ordered page of products.</returns>
    Task<IReadOnlyList<Product>> SearchAsync(
        string? searchTerm,
        CategoryId? categoryId,
        ProductSortField sortBy,
        ProductSortDirection sortDirection,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);
}
