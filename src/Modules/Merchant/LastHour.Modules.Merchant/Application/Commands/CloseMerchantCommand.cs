using LastHour.BuildingBlocks.Application.Cqrs;

namespace LastHour.Modules.Merchant.Application.Commands;

/// <summary>
/// Closes a merchant permanently.
/// </summary>
public sealed record CloseMerchantCommand : ICommand
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CloseMerchantCommand"/> class.
    /// </summary>
    /// <param name="merchantId">The merchant identifier to close.</param>
    public CloseMerchantCommand(Guid merchantId)
    {
        MerchantId = merchantId;
    }

    /// <summary>
    /// Gets the merchant identifier to close.
    /// </summary>
    public Guid MerchantId { get; }
}
