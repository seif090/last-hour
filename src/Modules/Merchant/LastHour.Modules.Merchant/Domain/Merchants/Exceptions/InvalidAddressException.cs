using LastHour.BuildingBlocks.SharedKernel.Domain.Exceptions;

namespace LastHour.Modules.Merchant.Domain.Merchants.Exceptions;

/// <summary>
/// Thrown when an address violates domain invariants.
/// </summary>
public sealed class InvalidAddressException : DomainException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InvalidAddressException"/> class.
    /// </summary>
    /// <param name="message">The reason the address is invalid.</param>
    public InvalidAddressException(string message)
        : base(message)
    {
    }
}
