using LastHour.BuildingBlocks.SharedKernel.Domain.Exceptions;

namespace LastHour.Modules.Catalog.Domain.Categories;

/// <summary>
/// Thrown when a category name or description violates domain invariants.
/// </summary>
public sealed class InvalidCategoryNameException : DomainException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InvalidCategoryNameException"/> class.
    /// </summary>
    /// <param name="message">The reason the category name or description is invalid.</param>
    public InvalidCategoryNameException(string message)
        : base(message)
    {
    }
}
