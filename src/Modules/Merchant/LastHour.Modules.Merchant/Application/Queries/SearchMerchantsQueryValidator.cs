using FluentValidation;

namespace LastHour.Modules.Merchant.Application.Queries;

/// <summary>
/// Validates the structural shape of a merchant search request.
/// </summary>
public sealed class SearchMerchantsQueryValidator : AbstractValidator<SearchMerchantsQuery>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SearchMerchantsQueryValidator"/> class.
    /// </summary>
    public SearchMerchantsQueryValidator()
    {
        RuleFor(query => query.PageNumber)
            .GreaterThanOrEqualTo(1)
            .WithErrorCode("PageNumberOutOfRange")
            .WithMessage("Page number must be at least 1.");

        RuleFor(query => query.PageSize)
            .InclusiveBetween(1, SearchMerchantsQuery.MaxPageSize)
            .WithErrorCode("PageSizeOutOfRange")
            .WithMessage($"Page size must be between 1 and {SearchMerchantsQuery.MaxPageSize}.");

        RuleFor(query => query.Status)
            .IsInEnum()
            .When(query => query.Status.HasValue)
            .WithErrorCode("MerchantStatusInvalid")
            .WithMessage("The specified merchant status is invalid.");

        RuleFor(query => query.SortBy)
            .IsInEnum()
            .WithErrorCode("SortByInvalid")
            .WithMessage("The specified sort field is invalid.");

        RuleFor(query => query.SortDirection)
            .IsInEnum()
            .WithErrorCode("SortDirectionInvalid")
            .WithMessage("The specified sort direction is invalid.");
    }
}
