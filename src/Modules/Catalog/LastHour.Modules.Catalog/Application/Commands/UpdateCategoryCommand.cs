using LastHour.BuildingBlocks.Application.Cqrs;

namespace LastHour.Modules.Catalog.Application.Commands;

/// <summary>
/// Updates the details and optional parent hierarchy of an existing category.
/// </summary>
public sealed record UpdateCategoryCommand : ICommand
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateCategoryCommand"/> class.
    /// </summary>
    /// <param name="id">The category identifier.</param>
    /// <param name="name">The updated category name.</param>
    /// <param name="description">The updated optional description.</param>
    /// <param name="parentCategoryId">The updated optional parent category identifier.</param>
    public UpdateCategoryCommand(Guid id, string name, string? description = null, Guid? parentCategoryId = null)
    {
        Id = id;
        Name = name;
        Description = description;
        ParentCategoryId = parentCategoryId;
    }

    /// <summary>
    /// Gets the category identifier.
    /// </summary>
    public Guid Id { get; }

    /// <summary>
    /// Gets the updated category name.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the updated optional description.
    /// </summary>
    public string? Description { get; }

    /// <summary>
    /// Gets the updated optional parent category identifier.
    /// </summary>
    public Guid? ParentCategoryId { get; }
}
