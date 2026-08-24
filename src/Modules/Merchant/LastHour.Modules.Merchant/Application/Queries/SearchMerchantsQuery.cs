using LastHour.BuildingBlocks.Application.Cqrs;
using LastHour.Modules.Merchant.Application.Models;
using LastHour.Modules.Merchant.Domain.Merchants;

namespace LastHour.Modules.Merchant.Application.Queries;

/// <summary>
/// Searches merchants with optional free-text matching and status filter, returning
/// a single ordered page of results together with the total result count.
/// </summary>
public sealed record SearchMerchantsQuery : IQuery<PagedResult<MerchantDto>>
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
    public const MerchantSortField DefaultSortBy = MerchantSortField.Name;

    /// <summary>
    /// The default sort direction.
    /// </summary>
    public const MerchantSortDirection DefaultSortDirection = MerchantSortDirection.Asc;

    /// <summary>
    /// Initializes a new instance of the <see cref="SearchMerchantsQuery"/> class.
    /// </summary>
    /// <param name="searchTerm">An optional search term matched against name and city.</param>
    /// <param name="status">An optional status filter.</param>
    /// <param name="sortBy">The merchant attribute to order by.</param>
    /// <param name="sortDirection">The sort direction.</param>
    /// <param name="pageNumber">The one-based page number.</param>
    /// <param name="pageSize">The number of items per page.</param>
    public SearchMerchantsQuery(
        string? searchTerm = null,
        MerchantStatus? status = null,
        MerchantSortField sortBy = DefaultSortBy,
        MerchantSortDirection sortDirection = DefaultSortDirection,
        int pageNumber = 1,
        int pageSize = DefaultPageSize)
    {
        SearchTerm = searchTerm;
        Status = status;
        SortBy = sortBy;
        SortDirection = sortDirection;
        PageNumber = pageNumber;
        PageSize = pageSize;
    }

    /// <summary>
    /// Gets the optional search term matched against name and city.
    /// </summary>
    public string? SearchTerm { get; }

    /// <summary>
    /// Gets the optional status filter.
    /// </summary>
    public MerchantStatus? Status { get; }

    /// <summary>
    /// Gets the merchant attribute to order by.
    /// </summary>
    public MerchantSortField SortBy { get; }

    /// <summary>
    /// Gets the sort direction.
    /// </summary>
    public MerchantSortDirection SortDirection { get; }

    /// <summary>
    /// Gets the one-based page number.
    /// </summary>
    public int PageNumber { get; }

    /// <summary>
    /// Gets the number of items per page.
    /// </summary>
    public int PageSize { get; }
}
