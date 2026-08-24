using LastHour.BuildingBlocks.Application.Cqrs;
using LastHour.BuildingBlocks.SharedKernel.Results;
using LastHour.Modules.Merchant.Application.Models;
using LastHour.Modules.Merchant.Domain.Merchants;
using MerchantAggregate = LastHour.Modules.Merchant.Domain.Merchants.Merchant;

namespace LastHour.Modules.Merchant.Application.Queries;

/// <summary>
/// Searches merchants and projects the matching page onto safe <see cref="MerchantDto"/>
/// values. An empty or whitespace-only search term acts as no filter.
/// </summary>
public sealed class SearchMerchantsQueryHandler : IQueryHandler<SearchMerchantsQuery, PagedResult<MerchantDto>>
{
    private readonly IMerchantRepository _merchants;

    /// <summary>
    /// Initializes a new instance of the <see cref="SearchMerchantsQueryHandler"/> class.
    /// </summary>
    /// <param name="merchants">The merchant repository used to search and count merchants.</param>
    /// <exception cref="ArgumentNullException"><paramref name="merchants"/> is <see langword="null"/>.</exception>
    public SearchMerchantsQueryHandler(IMerchantRepository merchants)
    {
        ArgumentNullException.ThrowIfNull(merchants);
        _merchants = merchants;
    }

    /// <inheritdoc/>
    public async Task<Result<PagedResult<MerchantDto>>> Handle(
        SearchMerchantsQuery query,
        CancellationToken cancellationToken)
    {
        int totalCount = await _merchants
            .CountAsync(query.SearchTerm, query.Status, cancellationToken)
            .ConfigureAwait(false);

        IReadOnlyList<MerchantAggregate> merchants = await _merchants
            .SearchAsync(
                query.SearchTerm,
                query.Status,
                query.SortBy,
                query.SortDirection,
                query.PageNumber,
                query.PageSize,
                cancellationToken)
            .ConfigureAwait(false);

        MerchantDto[] items = merchants
            .Select(MapToDto)
            .ToArray();

        PagedResult<MerchantDto> page = new PagedResult<MerchantDto>(
            items,
            query.PageNumber,
            query.PageSize,
            totalCount);

        return Result<PagedResult<MerchantDto>>.Success(page);
    }

    private static MerchantDto MapToDto(MerchantAggregate merchant) => new MerchantDto(
        merchant.Id.Value,
        merchant.OwnerId,
        merchant.Name.Value,
        merchant.Address.Street,
        merchant.Address.City,
        merchant.Address.StateOrProvince,
        merchant.Address.PostalCode,
        merchant.Address.Country,
        merchant.Phone.Value,
        merchant.Status.ToString());
}
