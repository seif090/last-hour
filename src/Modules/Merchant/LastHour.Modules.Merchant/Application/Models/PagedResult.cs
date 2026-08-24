namespace LastHour.Modules.Merchant.Application.Models;

/// <summary>
/// A page of results together with the metadata a frontend needs to render paged navigation:
/// the requested page coordinate and the total result count, with the derived total page count.
/// </summary>
/// <typeparam name="T">The type of the paged items.</typeparam>
public sealed record PagedResult<T>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PagedResult{T}"/> class.
    /// </summary>
    /// <param name="items">The items on the current page.</param>
    /// <param name="pageNumber">The one-based page number.</param>
    /// <param name="pageSize">The number of items per page.</param>
    /// <param name="totalCount">The total number of matching items across all pages.</param>
    public PagedResult(IReadOnlyList<T> items, int pageNumber, int pageSize, int totalCount)
    {
        Items = items;
        PageNumber = pageNumber;
        PageSize = pageSize;
        TotalCount = totalCount;
    }

    /// <summary>
    /// Gets the items on the current page.
    /// </summary>
    public IReadOnlyList<T> Items { get; }

    /// <summary>
    /// Gets the one-based page number.
    /// </summary>
    public int PageNumber { get; }

    /// <summary>
    /// Gets the number of items per page.
    /// </summary>
    public int PageSize { get; }

    /// <summary>
    /// Gets the total number of matching items across all pages.
    /// </summary>
    public int TotalCount { get; }

    /// <summary>
    /// Gets the total number of pages.
    /// </summary>
    public int TotalPages => TotalCount == 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
}
