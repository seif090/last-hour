namespace LastHour.Modules.Catalog.Application.Models;

/// <summary>
/// The safe projection of a category returned to read callers. It carries the stable identifier
/// and the descriptive taxonomy fields only — never internal persistence or audit state.
/// </summary>
public sealed record CategoryDto
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CategoryDto"/> class.
    /// </summary>
    /// <param name="id">The category identifier.</param>
    /// <param name="name">The category name.</param>
    /// <param name="description">An optional description.</param>
    /// <param name="parentCategoryId">An optional parent category identifier.</param>
    public CategoryDto(Guid id, string name, string? description, Guid? parentCategoryId)
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
