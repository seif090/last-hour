using FluentValidation;

namespace LastHour.Modules.Catalog.Application.Queries.Products;

/// <summary>
/// Validates the structural shape of a product search request: sane paging bounds, an optional
/// non-empty category filter, and a sort field and direction drawn from the explicitly
/// supported sets.
/// </summary>
public sealed class SearchProductsQueryValidator : AbstractValidator<SearchProductsQuery>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SearchProductsQueryValidator"/> class.
    /// </summary>
    public SearchProductsQueryValidator()
    {
        RuleFor(query => query.PageNumber)
            .GreaterThanOrEqualTo(1)
            .WithErrorCode("PageNumberInvalid")
            .WithMessage("Page number must be at least 1.");

        RuleFor(query => query.PageSize)
            .GreaterThanOrEqualTo(1)
            .WithErrorCode("PageSizeInvalid")
            .WithMessage("Page size must be at least 1.")
            .LessThanOrEqualTo(SearchProductsQuery.MaxPageSize)
            .WithErrorCode("PageSizeTooLarge")
            .WithMessage($"Page size cannot exceed {SearchProductsQuery.MaxPageSize}.");

        RuleFor(query => query.CategoryId)
            .Must(categoryId => !categoryId.HasValue || categoryId.Value != Guid.Empty)
            .WithErrorCode("CategoryIdInvalid")
            .WithMessage("A category identifier cannot be empty when provided.");

        RuleFor(query => query.SortBy)
            .Must(sortBy => SearchProductsQuery.SupportedSortFields.Contains(sortBy, StringComparer.OrdinalIgnoreCase))
            .WithErrorCode("SortByInvalid")
            .WithMessage("Sort field must be one of: Name, Price, Sku.");

        RuleFor(query => query.SortDirection)
            .Must(sortDirection => SearchProductsQuery.SupportedSortDirections.Contains(sortDirection, StringComparer.OrdinalIgnoreCase))
            .WithErrorCode("SortDirectionInvalid")
            .WithMessage("Sort direction must be one of: Asc, Desc.");
    }
}
