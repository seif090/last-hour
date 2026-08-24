using LastHour.BuildingBlocks.Application.Cqrs;
using LastHour.BuildingBlocks.SharedKernel.Results;
using LastHour.Modules.Catalog.Application.Models;
using LastHour.Modules.Catalog.Domain.Categories;
using LastHour.Modules.Catalog.Domain.Products;

namespace LastHour.Modules.Catalog.Application.Queries.Products;

/// <summary>
/// Searches the product catalog and projects the matching page onto safe <see cref="ProductDto"/>
/// values. An empty or whitespace-only search term acts as no filter; an unknown category simply
/// yields an empty page. Soft-deleted products are excluded by the repository query filters.
/// </summary>
public sealed class SearchProductsQueryHandler : IQueryHandler<SearchProductsQuery, PagedResult<ProductDto>>
{
    private readonly IProductRepository _products;

    /// <summary>
    /// Initializes a new instance of the <see cref="SearchProductsQueryHandler"/> class.
    /// </summary>
    /// <param name="products">The product repository used to search and count products.</param>
    /// <exception cref="ArgumentNullException"><paramref name="products"/> is <see langword="null"/>.</exception>
    public SearchProductsQueryHandler(IProductRepository products)
    {
        ArgumentNullException.ThrowIfNull(products);
        _products = products;
    }

    /// <inheritdoc/>
    public async Task<Result<PagedResult<ProductDto>>> Handle(
        SearchProductsQuery query,
        CancellationToken cancellationToken)
    {
        string? searchTerm = string.IsNullOrWhiteSpace(query.Search) ? null : query.Search.Trim();
        CategoryId? categoryId = query.CategoryId.HasValue ? CategoryId.Create(query.CategoryId.Value) : null;
        ProductSortField sortBy = ResolveSortField(query.SortBy);
        ProductSortDirection sortDirection = ResolveSortDirection(query.SortDirection);

        int totalCount = await _products
            .CountAsync(searchTerm, categoryId, cancellationToken)
            .ConfigureAwait(false);

        IReadOnlyList<Product> products = await _products
            .SearchAsync(searchTerm, categoryId, sortBy, sortDirection, query.PageNumber, query.PageSize, cancellationToken)
            .ConfigureAwait(false);

        ProductDto[] items = products
            .Select(product => new ProductDto(
                product.Id.Value,
                product.Sku.Value,
                product.Name,
                product.Description,
                product.BasePrice.Amount,
                product.BasePrice.Currency,
                product.CategoryId.Value))
            .ToArray();

        PagedResult<ProductDto> page = new PagedResult<ProductDto>(
            items,
            query.PageNumber,
            query.PageSize,
            totalCount);

        return Result<PagedResult<ProductDto>>.Success(page);
    }

    private static ProductSortField ResolveSortField(string sortBy) => sortBy.ToUpperInvariant() switch
    {
        "NAME" => ProductSortField.Name,
        "PRICE" => ProductSortField.Price,
        "SKU" => ProductSortField.Sku,
        _ => throw new ArgumentOutOfRangeException(nameof(sortBy), sortBy, "Unsupported product sort field."),
    };

    private static ProductSortDirection ResolveSortDirection(string sortDirection) => sortDirection.ToUpperInvariant() switch
    {
        "ASC" => ProductSortDirection.Asc,
        "DESC" => ProductSortDirection.Desc,
        _ => throw new ArgumentOutOfRangeException(nameof(sortDirection), sortDirection, "Unsupported product sort direction."),
    };
}
