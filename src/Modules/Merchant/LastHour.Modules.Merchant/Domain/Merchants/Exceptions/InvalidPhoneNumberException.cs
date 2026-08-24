using LastHour.BuildingBlocks.SharedKernel.Domain.Exceptions;

namespace LastHour.Modules.Merchant.Domain.Merchants.Exceptions;

/// <summary>
/// Thrown when a phone number violates domain invariants.
/// </summary>
public sealed class InvalidPhoneNumberException : DomainException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InvalidPhoneNumberException"/> class.
    /// </summary>
    /// <param name="message">The reason the phone number is invalid.</param>
    public InvalidPhoneNumberException(string message)
        : base(message)
    {
    }
}
