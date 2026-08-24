using LastHour.BuildingBlocks.SharedKernel.Domain.Exceptions;

namespace LastHour.Modules.Catalog.Domain.Products;

/// <summary>
/// Thrown when a product SKU violates the domain SKU invariant.
/// </summary>
public sealed class InvalidSkuException : DomainException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InvalidSkuException"/> class.
    /// </summary>
    /// <param name="message">The reason the SKU is invalid.</param>
    public InvalidSkuException(string message)
        : base(message)
    {
    }
}
