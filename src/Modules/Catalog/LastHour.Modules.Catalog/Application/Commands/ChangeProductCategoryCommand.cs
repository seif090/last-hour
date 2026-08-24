using LastHour.BuildingBlocks.Application.Cqrs;

namespace LastHour.Modules.Catalog.Application.Commands;

/// <summary>
/// Changes the category of an existing product.
/// </summary>
public sealed record ChangeProductCategoryCommand : ICommand
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ChangeProductCategoryCommand"/> class.
    /// </summary>
    /// <param name="productId">The product identifier.</param>
    /// <param name="categoryId">The new category identifier.</param>
    public ChangeProductCategoryCommand(Guid productId, Guid categoryId)
    {
        ProductId = productId;
        CategoryId = categoryId;
    }

    /// <summary>
    /// Gets the product identifier.
    /// </summary>
    public Guid ProductId { get; }

    /// <summary>
    /// Gets the category identifier.
    /// </summary>
    public Guid CategoryId { get; }
}
