using LastHour.BuildingBlocks.Application.Cqrs;
using LastHour.BuildingBlocks.SharedKernel.Results;
using LastHour.Modules.Catalog.Application.Models;
using LastHour.Modules.Catalog.Domain.Categories;

namespace LastHour.Modules.Catalog.Application.Queries.Categories;

/// <summary>
/// Resolves every category and projects each onto a safe <see cref="CategoryDto"/>, ordered
/// deterministically by name in ascending direction.
/// </summary>
public sealed class GetCategoriesQueryHandler : IQueryHandler<GetCategoriesQuery, IReadOnlyList<CategoryDto>>
{
    private readonly ICategoryRepository _categories;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetCategoriesQueryHandler"/> class.
    /// </summary>
    /// <param name="categories">The category repository used to resolve the categories.</param>
    /// <exception cref="ArgumentNullException"><paramref name="categories"/> is <see langword="null"/>.</exception>
    public GetCategoriesQueryHandler(ICategoryRepository categories)
    {
        ArgumentNullException.ThrowIfNull(categories);
        _categories = categories;
    }

    /// <inheritdoc/>
    public async Task<Result<IReadOnlyList<CategoryDto>>> Handle(
        GetCategoriesQuery query,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<Category> categories = await _categories
            .GetAllAsync(cancellationToken)
            .ConfigureAwait(false);

        CategoryDto[] result = categories
            .OrderBy(category => category.Name, StringComparer.OrdinalIgnoreCase)
            .Select(category => new CategoryDto(
                category.Id.Value,
                category.Name,
                category.Description,
                category.ParentCategoryId?.Value))
            .ToArray();

        return Result<IReadOnlyList<CategoryDto>>.Success(result);
    }
}
