using LastHour.BuildingBlocks.SharedKernel.Domain.Exceptions;

namespace LastHour.Modules.Merchant.Domain.Merchants.Exceptions;

/// <summary>
/// Thrown when a merchant name violates domain invariants.
/// </summary>
public sealed class MerchantNameInvalidException : DomainException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MerchantNameInvalidException"/> class.
    /// </summary>
    /// <param name="message">The reason the merchant name is invalid.</param>
    public MerchantNameInvalidException(string message)
        : base(message)
    {
    }
}
