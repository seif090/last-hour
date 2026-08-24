using FluentAssertions;
using FluentValidation.Results;
using LastHour.Modules.Catalog.Application.Queries.Products;

namespace LastHour.Modules.Catalog.Tests.Application.Validation;

public class SearchProductsQueryValidatorTests
{
    private readonly SearchProductsQueryValidator _validator = new SearchProductsQueryValidator();

    [Fact]
    public void ValidQuery_PassesValidation()
    {
        var query = new SearchProductsQuery(pageNumber: 2, pageSize: 25, search: "keyboard", sortBy: "price", sortDirection: "desc");

        ValidationResult result = _validator.Validate(query);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void DefaultQuery_PassesValidation()
    {
        var query = new SearchProductsQuery();

        ValidationResult result = _validator.Validate(query);

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void PageNumberBelowOne_FailsValidation(int pageNumber)
    {
        var query = new SearchProductsQuery(pageNumber: pageNumber);

        ValidationResult result = _validator.Validate(query);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorCode == "PageNumberInvalid");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void PageSizeBelowOne_FailsValidation(int pageSize)
    {
        var query = new SearchProductsQuery(pageSize: pageSize);

        ValidationResult result = _validator.Validate(query);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorCode == "PageSizeInvalid");
    }

    [Fact]
    public void PageSizeAboveMaximum_FailsValidation()
    {
        var query = new SearchProductsQuery(pageSize: SearchProductsQuery.MaxPageSize + 1);

        ValidationResult result = _validator.Validate(query);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorCode == "PageSizeTooLarge");
    }

    [Theory]
    [InlineData("Unknown")]
    [InlineData("Id")]
    [InlineData("")]
    public void UnsupportedSortField_FailsValidation(string sortBy)
    {
        var query = new SearchProductsQuery(sortBy: sortBy);

        ValidationResult result = _validator.Validate(query);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorCode == "SortByInvalid");
    }

    [Theory]
    [InlineData("Forward")]
    [InlineData("Back")]
    [InlineData("")]
    public void UnsupportedSortDirection_FailsValidation(string sortDirection)
    {
        var query = new SearchProductsQuery(sortDirection: sortDirection);

        ValidationResult result = _validator.Validate(query);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorCode == "SortDirectionInvalid");
    }

    [Fact]
    public void EmptyCategoryId_FailsValidation()
    {
        var query = new SearchProductsQuery(categoryId: Guid.Empty);

        ValidationResult result = _validator.Validate(query);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorCode == "CategoryIdInvalid");
    }
}
