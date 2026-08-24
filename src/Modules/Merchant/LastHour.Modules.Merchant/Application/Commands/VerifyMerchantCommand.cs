using LastHour.BuildingBlocks.Application.Cqrs;

namespace LastHour.Modules.Merchant.Application.Commands;

/// <summary>
/// Verifies a pending merchant, activating their account.
/// </summary>
public sealed record VerifyMerchantCommand : ICommand
{
    /// <summary>
    /// Initializes a new instance of the <see cref="VerifyMerchantCommand"/> class.
    /// </summary>
    /// <param name="merchantId">The merchant identifier to verify.</param>
    public VerifyMerchantCommand(Guid merchantId)
    {
        MerchantId = merchantId;
    }

    /// <summary>
    /// Gets the merchant identifier to verify.
    /// </summary>
    public Guid MerchantId { get; }
}
