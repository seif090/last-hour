using LastHour.BuildingBlocks.Application.Cqrs;
using LastHour.Modules.Catalog.Application.Models;

namespace LastHour.Modules.Catalog.Application.Queries.Categories;

/// <summary>
/// Retrieves a single category by its identifier.
/// </summary>
public sealed record GetCategoryByIdQuery : IQuery<CategoryDto>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GetCategoryByIdQuery"/> class.
    /// </summary>
    /// <param name="categoryId">The category identifier to look up.</param>
    public GetCategoryByIdQuery(Guid categoryId)
    {
        CategoryId = categoryId;
    }

    /// <summary>
    /// Gets the category identifier to look up.
    /// </summary>
    public Guid CategoryId { get; }
}
