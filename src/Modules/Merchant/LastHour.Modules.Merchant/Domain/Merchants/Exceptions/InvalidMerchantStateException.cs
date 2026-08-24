using LastHour.BuildingBlocks.SharedKernel.Domain.Exceptions;

namespace LastHour.Modules.Merchant.Domain.Merchants.Exceptions;

/// <summary>
/// Thrown when an invalid merchant state or state transition is encountered.
/// </summary>
public sealed class InvalidMerchantStateException : DomainException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InvalidMerchantStateException"/> class.
    /// </summary>
    /// <param name="message">The reason the merchant state is invalid.</param>
    public InvalidMerchantStateException(string message)
        : base(message)
    {
    }
}
