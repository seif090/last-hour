using LastHour.BuildingBlocks.Application.Cqrs;
using LastHour.BuildingBlocks.SharedKernel.Results;
using LastHour.Modules.Merchant.Application.Models;
using LastHour.Modules.Merchant.Domain.Merchants;
using MerchantAggregate = LastHour.Modules.Merchant.Domain.Merchants.Merchant;

namespace LastHour.Modules.Merchant.Application.Queries;

/// <summary>
/// Resolves a merchant and projects it onto a safe <see cref="MerchantDto"/>.
/// </summary>
public sealed class GetMerchantByIdQueryHandler : IQueryHandler<GetMerchantByIdQuery, MerchantDto>
{
    private readonly IMerchantRepository _merchants;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetMerchantByIdQueryHandler"/> class.
    /// </summary>
    /// <param name="merchants">The merchant repository used to resolve the merchant.</param>
    /// <exception cref="ArgumentNullException"><paramref name="merchants"/> is <see langword="null"/>.</exception>
    public GetMerchantByIdQueryHandler(IMerchantRepository merchants)
    {
        ArgumentNullException.ThrowIfNull(merchants);
        _merchants = merchants;
    }

    /// <inheritdoc/>
    public async Task<Result<MerchantDto>> Handle(
        GetMerchantByIdQuery query,
        CancellationToken cancellationToken)
    {
        MerchantId merchantId = MerchantId.Create(query.MerchantId);
        MerchantAggregate? merchant = await _merchants
            .GetByIdAsync(merchantId, cancellationToken)
            .ConfigureAwait(false);

        if (merchant is null)
        {
            return Result<MerchantDto>.Failure(Error.NotFound(
                "Merchants.NotFound",
                "The merchant could not be found."));
        }

        return Result<MerchantDto>.Success(MapToDto(merchant));
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
