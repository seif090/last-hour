using LastHour.BuildingBlocks.Application.Cqrs;
using LastHour.Modules.Catalog.Domain.Categories;

namespace LastHour.Modules.Catalog.Application.Commands;

/// <summary>
/// Creates a new category in the catalog taxonomy.
/// </summary>
public sealed record CreateCategoryCommand : ICommand<CategoryId>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CreateCategoryCommand"/> class.
    /// </summary>
    /// <param name="name">The category name.</param>
    /// <param name="description">An optional description.</param>
    /// <param name="parentCategoryId">An optional parent category identifier.</param>
    public CreateCategoryCommand(string name, string? description = null, Guid? parentCategoryId = null)
    {
        Name = name;
        Description = description;
        ParentCategoryId = parentCategoryId;
    }

    /// <summary>
    /// Gets the category name.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the optional description.
    /// </summary>
    public string? Description { get; }

    /// <summary>
    /// Gets the optional parent category identifier.
    /// </summary>
    public Guid? ParentCategoryId { get; }
}
