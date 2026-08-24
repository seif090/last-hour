using LastHour.BuildingBlocks.Application.Cqrs;

namespace LastHour.Modules.Merchant.Application.Commands;

/// <summary>
/// Activates a suspended merchant.
/// </summary>
public sealed record ActivateMerchantCommand : ICommand
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ActivateMerchantCommand"/> class.
    /// </summary>
    /// <param name="merchantId">The merchant identifier to activate.</param>
    public ActivateMerchantCommand(Guid merchantId)
    {
        MerchantId = merchantId;
    }

    /// <summary>
    /// Gets the merchant identifier to activate.
    /// </summary>
    public Guid MerchantId { get; }
}
