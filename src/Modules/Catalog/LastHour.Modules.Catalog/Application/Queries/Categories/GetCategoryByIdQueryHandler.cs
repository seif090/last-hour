using LastHour.BuildingBlocks.Application.Cqrs;
using LastHour.BuildingBlocks.SharedKernel.Results;
using LastHour.Modules.Catalog.Application.Models;
using LastHour.Modules.Catalog.Domain.Categories;

namespace LastHour.Modules.Catalog.Application.Queries.Categories;

/// <summary>
/// Resolves a category and projects it onto a safe <see cref="CategoryDto"/>. An unknown
/// category yields a not-found result.
/// </summary>
public sealed class GetCategoryByIdQueryHandler : IQueryHandler<GetCategoryByIdQuery, CategoryDto>
{
    private readonly ICategoryRepository _categories;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetCategoryByIdQueryHandler"/> class.
    /// </summary>
    /// <param name="categories">The category repository used to resolve the category.</param>
    /// <exception cref="ArgumentNullException"><paramref name="categories"/> is <see langword="null"/>.</exception>
    public GetCategoryByIdQueryHandler(ICategoryRepository categories)
    {
        ArgumentNullException.ThrowIfNull(categories);
        _categories = categories;
    }

    /// <inheritdoc/>
    public async Task<Result<CategoryDto>> Handle(
        GetCategoryByIdQuery query,
        CancellationToken cancellationToken)
    {
        CategoryId categoryId = CategoryId.Create(query.CategoryId);
        Category? category = await _categories
            .GetByIdAsync(categoryId, cancellationToken)
            .ConfigureAwait(false);

        if (category is null)
        {
            return Result<CategoryDto>.Failure(Error.NotFound(
                "Categories.NotFound",
                "The category could not be found."));
        }

        return Result<CategoryDto>.Success(new CategoryDto(
            category.Id.Value,
            category.Name,
            category.Description,
            category.ParentCategoryId?.Value));
    }
}
