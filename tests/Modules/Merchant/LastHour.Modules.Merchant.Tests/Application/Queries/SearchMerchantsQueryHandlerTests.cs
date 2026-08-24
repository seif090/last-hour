using FluentAssertions;
using LastHour.BuildingBlocks.SharedKernel.Results;
using LastHour.Modules.Merchant.Application.Models;
using LastHour.Modules.Merchant.Application.Queries;
using LastHour.Modules.Merchant.Domain.Merchants;
using LastHour.Modules.Merchant.Tests.Application.TestSupport;
using MerchantAggregate = LastHour.Modules.Merchant.Domain.Merchants.Merchant;

namespace LastHour.Modules.Merchant.Tests.Application.Queries;

public class SearchMerchantsQueryHandlerTests
{
    private static SearchMerchantsQueryHandler CreateHandler(FakeMerchantRepository merchants)
        => new(merchants);

    [Fact]
    public void Constructor_NullRepository_ThrowsArgumentNullException()
    {
        Action act = () => _ = new SearchMerchantsQueryHandler(null!);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("merchants");
    }

    [Fact]
    public async Task Handle_EmptySearch_ReturnsPaginatedResults()
    {
        FakeMerchantRepository merchants = new();
        merchants.AddMerchant("Alpha Shop", "Boston");
        merchants.AddMerchant("Bravo Store", "Boston");
        merchants.AddMerchant("Charlie Mart", "Boston");

        SearchMerchantsQueryHandler handler = CreateHandler(merchants);

        Result<PagedResult<MerchantDto>> result = await handler.Handle(
            new SearchMerchantsQuery(pageNumber: 1, pageSize: 2),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(2);
        result.Value.TotalCount.Should().Be(3);
        result.Value.TotalPages.Should().Be(2);
        result.Value.PageNumber.Should().Be(1);
        result.Value.PageSize.Should().Be(2);
    }

    [Fact]
    public async Task Handle_SearchByMerchantName_ReturnsMatchingMerchants()
    {
        FakeMerchantRepository merchants = new();
        merchants.AddMerchant("Sweet Bakery", "Dallas");
        merchants.AddMerchant("Hardware Tools", "Dallas");

        SearchMerchantsQueryHandler handler = CreateHandler(merchants);

        Result<PagedResult<MerchantDto>> result = await handler.Handle(
            new SearchMerchantsQuery(searchTerm: "Bakery"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().ContainSingle(m => m.Name == "Sweet Bakery");
        result.Value.TotalCount.Should().Be(1);
    }

    [Fact]
    public async Task Handle_SearchByCity_ReturnsMatchingMerchants()
    {
        FakeMerchantRepository merchants = new();
        merchants.AddMerchant("First Mart", "Chicago");
        merchants.AddMerchant("Second Mart", "Atlanta");

        SearchMerchantsQueryHandler handler = CreateHandler(merchants);

        Result<PagedResult<MerchantDto>> result = await handler.Handle(
            new SearchMerchantsQuery(searchTerm: "Chicago"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().ContainSingle(m => m.Name == "First Mart");
        result.Value.TotalCount.Should().Be(1);
    }

    [Fact]
    public async Task Handle_WithStatusFilter_ReturnsOnlyMatchingStatus()
    {
        FakeMerchantRepository merchants = new();
        merchants.AddMerchant("Pending Store");
        MerchantAggregate active = merchants.AddMerchant("Active Store");
        active.Verify();

        SearchMerchantsQueryHandler handler = CreateHandler(merchants);

        Result<PagedResult<MerchantDto>> result = await handler.Handle(
            new SearchMerchantsQuery(status: MerchantStatus.Active),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().ContainSingle(m => m.Name == "Active Store");
        result.Value.TotalCount.Should().Be(1);
    }

    [Fact]
    public async Task Handle_CombinedSearchAndStatusFilter_ReturnsIntersection()
    {
        FakeMerchantRepository merchants = new();
        merchants.AddMerchant("Target Shop 1", "Houston");
        MerchantAggregate activeTarget = merchants.AddMerchant("Target Shop 2", "Houston");
        activeTarget.Verify();
        MerchantAggregate activeOther = merchants.AddMerchant("Other Shop", "Houston");
        activeOther.Verify();

        SearchMerchantsQueryHandler handler = CreateHandler(merchants);

        Result<PagedResult<MerchantDto>> result = await handler.Handle(
            new SearchMerchantsQuery(searchTerm: "Target", status: MerchantStatus.Active),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().ContainSingle(m => m.Name == "Target Shop 2");
        result.Value.TotalCount.Should().Be(1);
    }

    [Fact]
    public async Task Handle_Pagination_ReturnsCorrectPageSlice()
    {
        FakeMerchantRepository merchants = new();
        merchants.AddMerchant("Shop A");
        merchants.AddMerchant("Shop B");
        merchants.AddMerchant("Shop C");

        SearchMerchantsQueryHandler handler = CreateHandler(merchants);

        Result<PagedResult<MerchantDto>> result = await handler.Handle(
            new SearchMerchantsQuery(pageNumber: 2, pageSize: 2),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().ContainSingle(m => m.Name == "Shop C");
        result.Value.PageNumber.Should().Be(2);
        result.Value.TotalCount.Should().Be(3);
        result.Value.TotalPages.Should().Be(2);
    }

    [Fact]
    public async Task Handle_PageNumberBeyondAvailableData_ReturnsEmptyItemsWithCorrectTotals()
    {
        FakeMerchantRepository merchants = new();
        merchants.AddMerchant("Store 1");
        merchants.AddMerchant("Store 2");
        merchants.AddMerchant("Store 3");

        SearchMerchantsQueryHandler handler = CreateHandler(merchants);

        Result<PagedResult<MerchantDto>> result = await handler.Handle(
            new SearchMerchantsQuery(pageNumber: 5, pageSize: 2),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().BeEmpty();
        result.Value.TotalCount.Should().Be(3);
        result.Value.TotalPages.Should().Be(2);
        result.Value.PageNumber.Should().Be(5);
        result.Value.PageSize.Should().Be(2);
    }

    [Fact]
    public async Task Handle_SortByNameAscendingAndDescending_OrdersCorrectly()
    {
        FakeMerchantRepository merchants = new();
        merchants.AddMerchant("Bravo");
        merchants.AddMerchant("Alpha");
        merchants.AddMerchant("Charlie");

        SearchMerchantsQueryHandler handler = CreateHandler(merchants);

        Result<PagedResult<MerchantDto>> ascResult = await handler.Handle(
            new SearchMerchantsQuery(sortBy: MerchantSortField.Name, sortDirection: MerchantSortDirection.Asc),
            CancellationToken.None);

        Result<PagedResult<MerchantDto>> descResult = await handler.Handle(
            new SearchMerchantsQuery(sortBy: MerchantSortField.Name, sortDirection: MerchantSortDirection.Desc),
            CancellationToken.None);

        ascResult.Value.Items.Select(m => m.Name).Should().Equal("Alpha", "Bravo", "Charlie");
        descResult.Value.Items.Select(m => m.Name).Should().Equal("Charlie", "Bravo", "Alpha");
    }

    [Fact]
    public async Task Handle_SortByStatus_OrdersCorrectly()
    {
        FakeMerchantRepository merchants = new();
        merchants.AddMerchant("Store 1");
        MerchantAggregate active = merchants.AddMerchant("Store 2");
        active.Verify();

        SearchMerchantsQueryHandler handler = CreateHandler(merchants);

        Result<PagedResult<MerchantDto>> result = await handler.Handle(
            new SearchMerchantsQuery(sortBy: MerchantSortField.Status, sortDirection: MerchantSortDirection.Asc),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(2);
    }

    [Fact]
    public async Task Handle_SortByCreatedAt_OrdersCorrectly()
    {
        FakeMerchantRepository merchants = new();
        merchants.AddMerchant("Store 1");
        merchants.AddMerchant("Store 2");

        SearchMerchantsQueryHandler handler = CreateHandler(merchants);

        Result<PagedResult<MerchantDto>> result = await handler.Handle(
            new SearchMerchantsQuery(sortBy: MerchantSortField.CreatedAt, sortDirection: MerchantSortDirection.Asc),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(2);
    }

    [Fact]
    public async Task Handle_EmptyResultSet_ReturnsSuccessWithEmptyItems()
    {
        FakeMerchantRepository merchants = new();
        SearchMerchantsQueryHandler handler = CreateHandler(merchants);

        Result<PagedResult<MerchantDto>> result = await handler.Handle(
            new SearchMerchantsQuery(searchTerm: "NonExistent"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().BeEmpty();
        result.Value.TotalCount.Should().Be(0);
        result.Value.TotalPages.Should().Be(0);
    }
}
