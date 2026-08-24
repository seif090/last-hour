using LastHour.BuildingBlocks.SharedKernel.Domain.Exceptions;

namespace LastHour.Modules.Catalog.Domain.Products;

/// <summary>
/// Thrown when a price or monetary amount violates domain price invariants.
/// </summary>
public sealed class InvalidPriceException : DomainException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InvalidPriceException"/> class.
    /// </summary>
    /// <param name="message">The reason the price is invalid.</param>
    public InvalidPriceException(string message)
        : base(message)
    {
    }
}
