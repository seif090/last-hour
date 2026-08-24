using LastHour.BuildingBlocks.Application.Cqrs;
using LastHour.Modules.Catalog.Application.Models;

namespace LastHour.Modules.Catalog.Application.Queries.Categories;

/// <summary>
/// Retrieves all categories in the catalog taxonomy.
/// </summary>
public sealed record GetCategoriesQuery : IQuery<IReadOnlyList<CategoryDto>>;
