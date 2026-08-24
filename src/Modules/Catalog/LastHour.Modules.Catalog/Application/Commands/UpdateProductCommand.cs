using LastHour.BuildingBlocks.Application.Cqrs;

namespace LastHour.Modules.Catalog.Application.Commands;

/// <summary>
/// Updates the descriptive details of an existing product.
/// </summary>
public sealed record UpdateProductCommand : ICommand
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateProductCommand"/> class.
    /// </summary>
    /// <param name="id">The product identifier.</param>
    /// <param name="name">The updated product name.</param>
    /// <param name="description">The updated optional description.</param>
    public UpdateProductCommand(Guid id, string name, string? description = null)
    {
        Id = id;
        Name = name;
        Description = description;
    }

    /// <summary>
    /// Gets the product identifier.
    /// </summary>
    public Guid Id { get; }

    /// <summary>
    /// Gets the updated product name.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the updated optional description.
    /// </summary>
    public string? Description { get; }
}
