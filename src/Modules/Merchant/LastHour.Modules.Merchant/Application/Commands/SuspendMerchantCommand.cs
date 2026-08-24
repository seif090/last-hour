using LastHour.BuildingBlocks.Application.Cqrs;

namespace LastHour.Modules.Merchant.Application.Commands;

/// <summary>
/// Suspends an active merchant.
/// </summary>
public sealed record SuspendMerchantCommand : ICommand
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SuspendMerchantCommand"/> class.
    /// </summary>
    /// <param name="merchantId">The merchant identifier to suspend.</param>
    public SuspendMerchantCommand(Guid merchantId)
    {
        MerchantId = merchantId;
    }

    /// <summary>
    /// Gets the merchant identifier to suspend.
    /// </summary>
    public Guid MerchantId { get; }
}
