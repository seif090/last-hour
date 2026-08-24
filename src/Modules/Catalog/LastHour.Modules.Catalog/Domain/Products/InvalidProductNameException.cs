using LastHour.BuildingBlocks.SharedKernel.Domain.Exceptions;

namespace LastHour.Modules.Catalog.Domain.Products;

/// <summary>
/// Thrown when a product name or description violates domain invariants.
/// </summary>
public sealed class InvalidProductNameException : DomainException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InvalidProductNameException"/> class.
    /// </summary>
    /// <param name="message">The reason the product name or description is invalid.</param>
    public InvalidProductNameException(string message)
        : base(message)
    {
    }
}
