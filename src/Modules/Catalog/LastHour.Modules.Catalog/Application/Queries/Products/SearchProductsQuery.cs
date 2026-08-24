using LastHour.BuildingBlocks.Application.Cqrs;
using LastHour.Modules.Catalog.Application.Models;

namespace LastHour.Modules.Catalog.Application.Queries.Products;

/// <summary>
/// Searches the product catalog with optional free-text matching and a category filter, returning
/// a single ordered page of results together with the total result count.
/// </summary>
public sealed record SearchProductsQuery : IQuery<PagedResult<ProductDto>>
{
    /// <summary>
    /// The default page size.
    /// </summary>
    public const int DefaultPageSize = 20;

    /// <summary>
    /// The maximum allowed page size.
    /// </summary>
    public const int MaxPageSize = 100;

    /// <summary>
    /// The default sort field.
    /// </summary>
    public const string DefaultSortBy = "Name";

    /// <summary>
    /// The default sort direction.
    /// </summary>
    public const string DefaultSortDirection = "Asc";

    /// <summary>
    /// Gets the explicitly supported sort fields.
    /// </summary>
    public static IReadOnlyList<string> SupportedSortFields { get; } = new[] { "Name", "Price", "Sku" };

    /// <summary>
    /// Gets the explicitly supported sort directions.
    /// </summary>
    public static IReadOnlyList<string> SupportedSortDirections { get; } = new[] { "Asc", "Desc" };

    /// <summary>
    /// Initializes a new instance of the <see cref="SearchProductsQuery"/> class.
    /// </summary>
    /// <param name="pageNumber">The one-based page number.</param>
    /// <param name="pageSize">The number of items per page.</param>
    /// <param name="search">An optional term matched against the product name and SKU.</param>
    /// <param name="categoryId">An optional category identifier filter.</param>
    /// <param name="sortBy">The sort field; one of <see cref="SupportedSortFields"/>.</param>
    /// <param name="sortDirection">The sort direction; one of <see cref="SupportedSortDirections"/>.</param>
    public SearchProductsQuery(
        int pageNumber = 1,
        int pageSize = DefaultPageSize,
        string? search = null,
        Guid? categoryId = null,
        string sortBy = DefaultSortBy,
        string sortDirection = DefaultSortDirection)
    {
        PageNumber = pageNumber;
        PageSize = pageSize;
        Search = search;
        CategoryId = categoryId;
        SortBy = sortBy;
        SortDirection = sortDirection;
    }

    /// <summary>
    /// Gets the one-based page number.
    /// </summary>
    public int PageNumber { get; }

    /// <summary>
    /// Gets the number of items per page.
    /// </summary>
    public int PageSize { get; }

    /// <summary>
    /// Gets the optional search term matched against the product name and SKU.
    /// </summary>
    public string? Search { get; }

    /// <summary>
    /// Gets the optional category identifier filter.
    /// </summary>
    public Guid? CategoryId { get; }

    /// <summary>
    /// Gets the sort field.
    /// </summary>
    public string SortBy { get; }

    /// <summary>
    /// Gets the sort direction.
    /// </summary>
    public string SortDirection { get; }
}
