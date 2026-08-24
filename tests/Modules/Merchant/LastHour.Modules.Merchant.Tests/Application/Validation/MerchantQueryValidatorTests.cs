using FluentAssertions;
using FluentValidation.Results;
using LastHour.Modules.Merchant.Application.Models;
using LastHour.Modules.Merchant.Application.Queries;
using LastHour.Modules.Merchant.Domain.Merchants;

namespace LastHour.Modules.Merchant.Tests.Application.Validation;

public class MerchantQueryValidatorTests
{
    private readonly GetMerchantByIdQueryValidator _getByIdValidator = new();
    private readonly SearchMerchantsQueryValidator _searchValidator = new();

    [Fact]
    public void GetById_EmptyMerchantId_FailsValidation()
    {
        GetMerchantByIdQuery query = new(Guid.Empty);

        ValidationResult result = _getByIdValidator.Validate(query);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(error =>
            error.PropertyName == nameof(GetMerchantByIdQuery.MerchantId) &&
            error.ErrorCode == "MerchantIdRequired");
    }

    [Fact]
    public void GetById_ValidMerchantId_PassesValidation()
    {
        GetMerchantByIdQuery query = new(Guid.NewGuid());

        ValidationResult result = _getByIdValidator.Validate(query);

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-10)]
    public void Search_InvalidPageNumber_FailsValidation(int pageNumber)
    {
        SearchMerchantsQuery query = new(pageNumber: pageNumber);

        ValidationResult result = _searchValidator.Validate(query);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(error =>
            error.PropertyName == nameof(SearchMerchantsQuery.PageNumber) &&
            error.ErrorCode == "PageNumberOutOfRange");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(101)]
    [InlineData(500)]
    public void Search_InvalidPageSize_FailsValidation(int pageSize)
    {
        SearchMerchantsQuery query = new(pageSize: pageSize);

        ValidationResult result = _searchValidator.Validate(query);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(error =>
            error.PropertyName == nameof(SearchMerchantsQuery.PageSize) &&
            error.ErrorCode == "PageSizeOutOfRange");
    }

    [Theory]
    [InlineData(1, 1)]
    [InlineData(1, 20)]
    [InlineData(5, 100)]
    public void Search_ValidPagingBounds_PassesValidation(int pageNumber, int pageSize)
    {
        SearchMerchantsQuery query = new(pageNumber: pageNumber, pageSize: pageSize);

        ValidationResult result = _searchValidator.Validate(query);

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(MerchantSortField.Name)]
    [InlineData(MerchantSortField.Status)]
    [InlineData(MerchantSortField.CreatedAt)]
    public void Search_SupportedSortFields_PassValidation(MerchantSortField sortBy)
    {
        SearchMerchantsQuery query = new(sortBy: sortBy);

        ValidationResult result = _searchValidator.Validate(query);

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(MerchantSortDirection.Asc)]
    [InlineData(MerchantSortDirection.Desc)]
    public void Search_SupportedSortDirections_PassValidation(MerchantSortDirection sortDirection)
    {
        SearchMerchantsQuery query = new(sortDirection: sortDirection);

        ValidationResult result = _searchValidator.Validate(query);

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(MerchantStatus.Pending)]
    [InlineData(MerchantStatus.Active)]
    [InlineData(MerchantStatus.Suspended)]
    [InlineData(MerchantStatus.Closed)]
    public void Search_SupportedStatus_PassValidation(MerchantStatus status)
    {
        SearchMerchantsQuery query = new(status: status);

        ValidationResult result = _searchValidator.Validate(query);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Search_NullStatus_PassesValidation()
    {
        SearchMerchantsQuery query = new(status: null);

        ValidationResult result = _searchValidator.Validate(query);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Search_InvalidEnumStatus_FailsValidation()
    {
        SearchMerchantsQuery query = new(status: (MerchantStatus)999);

        ValidationResult result = _searchValidator.Validate(query);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(error =>
            error.PropertyName == nameof(SearchMerchantsQuery.Status) &&
            error.ErrorCode == "MerchantStatusInvalid");
    }

    [Fact]
    public void Search_InvalidEnumSortField_FailsValidation()
    {
        SearchMerchantsQuery query = new(sortBy: (MerchantSortField)999);

        ValidationResult result = _searchValidator.Validate(query);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(error =>
            error.PropertyName == nameof(SearchMerchantsQuery.SortBy) &&
            error.ErrorCode == "SortByInvalid");
    }

    [Fact]
    public void Search_InvalidEnumSortDirection_FailsValidation()
    {
        SearchMerchantsQuery query = new(sortDirection: (MerchantSortDirection)999);

        ValidationResult result = _searchValidator.Validate(query);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(error =>
            error.PropertyName == nameof(SearchMerchantsQuery.SortDirection) &&
            error.ErrorCode == "SortDirectionInvalid");
    }
}
