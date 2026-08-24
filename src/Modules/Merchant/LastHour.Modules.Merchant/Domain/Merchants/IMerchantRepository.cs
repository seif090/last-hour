using LastHour.BuildingBlocks.Application.Contracts;
using LastHour.Modules.Merchant.Application.Models;

namespace LastHour.Modules.Merchant.Domain.Merchants;

/// <summary>
/// The persistence contract of the <see cref="Merchant"/> aggregate.
/// </summary>
public interface IMerchantRepository : IRepository<Merchant>
{
    /// <summary>
    /// Retrieves the merchant with the specified identifier, or <see langword="null"/> when no such merchant exists.
    /// </summary>
    /// <param name="id">The merchant identifier to look up.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>The matching merchant, or <see langword="null"/> when not found.</returns>
    Task<Merchant?> GetByIdAsync(MerchantId id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves the merchant with the specified identifier and places the aggregate in the change tracker,
    /// or <see langword="null"/> when no such merchant exists.
    /// </summary>
    /// <param name="id">The merchant identifier to look up.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>The matching, tracked merchant, or <see langword="null"/> when not found.</returns>
    Task<Merchant?> GetByIdTrackedAsync(MerchantId id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Determines whether a merchant with the specified identifier exists.
    /// </summary>
    /// <param name="id">The merchant identifier to check.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns><see langword="true"/> when the merchant exists; otherwise <see langword="false"/>.</returns>
    Task<bool> ExistsByIdAsync(MerchantId id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Determines whether a merchant with the specified name exists.
    /// </summary>
    /// <param name="name">The merchant name to check.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns><see langword="true"/> when a merchant with the name exists; otherwise <see langword="false"/>.</returns>
    Task<bool> ExistsByNameAsync(MerchantName name, CancellationToken cancellationToken = default);

    /// <summary>
    /// Counts the merchants matching the search criteria. An empty or whitespace-only search term
    /// acts as no filter.
    /// </summary>
    /// <param name="searchTerm">An optional term matched against the merchant name and city.</param>
    /// <param name="status">An optional status filter.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>The number of matching merchants.</returns>
    Task<int> CountAsync(
        string? searchTerm,
        MerchantStatus? status,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches merchants using the given criteria, returning a single ordered page. An empty or
    /// whitespace-only search term acts as no filter.
    /// </summary>
    /// <param name="searchTerm">An optional term matched against the merchant name and city.</param>
    /// <param name="status">An optional status filter.</param>
    /// <param name="sortBy">The merchant attribute to order by.</param>
    /// <param name="sortDirection">The ordering direction.</param>
    /// <param name="pageNumber">The one-based page number.</param>
    /// <param name="pageSize">The number of items per page.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>The matching, ordered page of merchants.</returns>
    Task<IReadOnlyList<Merchant>> SearchAsync(
        string? searchTerm,
        MerchantStatus? status,
        MerchantSortField sortBy,
        MerchantSortDirection sortDirection,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);
}
