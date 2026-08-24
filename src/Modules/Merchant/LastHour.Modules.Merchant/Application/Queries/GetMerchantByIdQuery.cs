using LastHour.BuildingBlocks.Application.Cqrs;
using LastHour.Modules.Merchant.Application.Models;

namespace LastHour.Modules.Merchant.Application.Queries;

/// <summary>
/// Retrieves a single merchant by its identifier.
/// </summary>
public sealed record GetMerchantByIdQuery : IQuery<MerchantDto>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GetMerchantByIdQuery"/> class.
    /// </summary>
    /// <param name="merchantId">The merchant identifier to look up.</param>
    public GetMerchantByIdQuery(Guid merchantId)
    {
        MerchantId = merchantId;
    }

    /// <summary>
    /// Gets the merchant identifier to look up.
    /// </summary>
    public Guid MerchantId { get; }
}
