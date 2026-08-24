using FluentAssertions;
using LastHour.BuildingBlocks.SharedKernel.Results;
using LastHour.Modules.Catalog.Application.Models;
using LastHour.Modules.Catalog.Application.Queries.Products;
using LastHour.Modules.Catalog.Domain.Categories;
using LastHour.Modules.Catalog.Domain.Products;
using LastHour.Modules.Catalog.Tests.Application.TestSupport;

namespace LastHour.Modules.Catalog.Tests.Application.Queries;

public class ProductQueryHandlerTests
{
    private static GetProductByIdQueryHandler CreateByIdHandler(FakeProductRepository products)
        => new GetProductByIdQueryHandler(products);

    private static SearchProductsQueryHandler CreateSearchHandler(FakeProductRepository products)
        => new SearchProductsQueryHandler(products);

    [Fact]
    public async Task GetProductById_ExistingProduct_ReturnsProductDto()
    {
        var products = new FakeProductRepository();
        CategoryId categoryId = CategoryId.New();
        Product product = products.AddProduct("KEY-01", "Mechanical Keyboard", 149.99m, "EUR", categoryId, "RGB Gaming");
        GetProductByIdQueryHandler handler = CreateByIdHandler(products);

        Result<ProductDto> result = await handler.Handle(
            new GetProductByIdQuery(product.Id.Value),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be(product.Id.Value);
        result.Value.Sku.Should().Be("KEY-01");
        result.Value.Name.Should().Be("Mechanical Keyboard");
        result.Value.Description.Should().Be("RGB Gaming");
        result.Value.PriceAmount.Should().Be(149.99m);
        result.Value.PriceCurrency.Should().Be("EUR");
        result.Value.CategoryId.Should().Be(categoryId.Value);
    }

    [Fact]
    public async Task GetProductById_MissingProduct_ReturnsNotFound()
    {
        GetProductByIdQueryHandler handler = CreateByIdHandler(new FakeProductRepository());

        Result<ProductDto> result = await handler.Handle(
            new GetProductByIdQuery(Guid.NewGuid()),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.FirstError!.Type.Should().Be(ErrorType.NotFound);
        result.FirstError!.Code.Should().Be("Products.NotFound");
    }

    [Fact]
    public async Task GetProductById_SoftDeletedProduct_ReturnsNotFound()
    {
        var products = new FakeProductRepository();
        Product product = products.AddProduct("KEY-01", "Mechanical Keyboard", 149.99m, "EUR", CategoryId.New());
        product.IsDeleted = true;
        GetProductByIdQueryHandler handler = CreateByIdHandler(products);

        Result<ProductDto> result = await handler.Handle(
            new GetProductByIdQuery(product.Id.Value),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.FirstError!.Type.Should().Be(ErrorType.NotFound);
        result.FirstError!.Code.Should().Be("Products.NotFound");
    }

    [Fact]
    public async Task SearchProducts_ReturnsPaginatedResultWithMetadata()
    {
        var products = new FakeProductRepository();
        CategoryId categoryId = CategoryId.New();
        for (int index = 1; index <= 3; index++)
        {
            products.AddProduct($"SKU-{index:00}", $"Product {index}", 10m * index, "EUR", categoryId);
        }

        SearchProductsQueryHandler handler = CreateSearchHandler(products);

        Result<PagedResult<ProductDto>> result = await handler.Handle(
            new SearchProductsQuery(pageSize: 2),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(2);
        result.Value.PageNumber.Should().Be(1);
        result.Value.PageSize.Should().Be(2);
        result.Value.TotalCount.Should().Be(3);
        result.Value.TotalPages.Should().Be(2);
    }

    [Fact]
    public async Task SearchProducts_FiltersByName()
    {
        var products = new FakeProductRepository();
        CategoryId categoryId = CategoryId.New();
        products.AddProduct("KEY-01", "Mechanical Keyboard", 149.99m, "EUR", categoryId);
        products.AddProduct("MOUSE-01", "Gaming Mouse", 79.99m, "EUR", categoryId);
        SearchProductsQueryHandler handler = CreateSearchHandler(products);

        Result<PagedResult<ProductDto>> result = await handler.Handle(
            new SearchProductsQuery(search: "mouse"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalCount.Should().Be(1);
        result.Value.Items.Should().ContainSingle(dto => dto.Name == "Gaming Mouse");
    }

    [Fact]
    public async Task SearchProducts_FiltersBySku()
    {
        var products = new FakeProductRepository();
        CategoryId categoryId = CategoryId.New();
        products.AddProduct("KEY-01", "Mechanical Keyboard", 149.99m, "EUR", categoryId);
        products.AddProduct("MOUSE-01", "Gaming Mouse", 79.99m, "EUR", categoryId);
        SearchProductsQueryHandler handler = CreateSearchHandler(products);

        Result<PagedResult<ProductDto>> result = await handler.Handle(
            new SearchProductsQuery(search: "MOUSE-01"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalCount.Should().Be(1);
        result.Value.Items.Should().ContainSingle(dto => dto.Sku == "MOUSE-01");
    }

    [Fact]
    public async Task SearchProducts_WhitespaceSearch_BehavesAsNoFilter()
    {
        var products = new FakeProductRepository();
        CategoryId categoryId = CategoryId.New();
        products.AddProduct("KEY-01", "Mechanical Keyboard", 149.99m, "EUR", categoryId);
        products.AddProduct("MOUSE-01", "Gaming Mouse", 79.99m, "EUR", categoryId);
        SearchProductsQueryHandler handler = CreateSearchHandler(products);

        Result<PagedResult<ProductDto>> result = await handler.Handle(
            new SearchProductsQuery(search: "   "),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalCount.Should().Be(2);
    }

    [Fact]
    public async Task SearchProducts_FiltersByCategoryId()
    {
        var products = new FakeProductRepository();
        CategoryId electronics = CategoryId.New();
        CategoryId books = CategoryId.New();
        products.AddProduct("KEY-01", "Mechanical Keyboard", 149.99m, "EUR", electronics);
        products.AddProduct("BOOK-01", "C# In Depth", 39.99m, "EUR", books);
        SearchProductsQueryHandler handler = CreateSearchHandler(products);

        Result<PagedResult<ProductDto>> result = await handler.Handle(
            new SearchProductsQuery(categoryId: electronics.Value),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalCount.Should().Be(1);
        result.Value.Items.Should().ContainSingle(dto => dto.Sku == "KEY-01");
    }

    [Fact]
    public async Task SearchProducts_CategoryWithNoProducts_ReturnsEmptyResult()
    {
        var products = new FakeProductRepository();
        CategoryId electronics = CategoryId.New();
        CategoryId books = CategoryId.New();
        products.AddProduct("KEY-01", "Mechanical Keyboard", 149.99m, "EUR", electronics);
        SearchProductsQueryHandler handler = CreateSearchHandler(products);

        Result<PagedResult<ProductDto>> result = await handler.Handle(
            new SearchProductsQuery(categoryId: books.Value),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalCount.Should().Be(0);
        result.Value.Items.Should().BeEmpty();
        result.Value.TotalPages.Should().Be(0);
    }

    [Fact]
    public async Task SearchProducts_SortsByNameAscending()
    {
        var products = new FakeProductRepository();
        CategoryId categoryId = CategoryId.New();
        products.AddProduct("KEY-01", "Zebra Keyboard", 10m, "EUR", categoryId);
        products.AddProduct("MOUSE-01", "Alpha Mouse", 20m, "EUR", categoryId);
        products.AddProduct("PAD-01", "Mango Pad", 30m, "EUR", categoryId);
        SearchProductsQueryHandler handler = CreateSearchHandler(products);

        Result<PagedResult<ProductDto>> result = await handler.Handle(
            new SearchProductsQuery(sortBy: "Name", sortDirection: "Asc"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Select(dto => dto.Name).Should().Equal("Alpha Mouse", "Mango Pad", "Zebra Keyboard");
    }

    [Fact]
    public async Task SearchProducts_SortsByNameDescending()
    {
        var products = new FakeProductRepository();
        CategoryId categoryId = CategoryId.New();
        products.AddProduct("KEY-01", "Zebra Keyboard", 10m, "EUR", categoryId);
        products.AddProduct("MOUSE-01", "Alpha Mouse", 20m, "EUR", categoryId);
        products.AddProduct("PAD-01", "Mango Pad", 30m, "EUR", categoryId);
        SearchProductsQueryHandler handler = CreateSearchHandler(products);

        Result<PagedResult<ProductDto>> result = await handler.Handle(
            new SearchProductsQuery(sortBy: "Name", sortDirection: "Desc"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Select(dto => dto.Name).Should().Equal("Zebra Keyboard", "Mango Pad", "Alpha Mouse");
    }

    [Fact]
    public async Task SearchProducts_SortsByPriceAscending()
    {
        var products = new FakeProductRepository();
        CategoryId categoryId = CategoryId.New();
        products.AddProduct("KEY-01", "Zebra Keyboard", 30m, "EUR", categoryId);
        products.AddProduct("MOUSE-01", "Alpha Mouse", 10m, "EUR", categoryId);
        products.AddProduct("PAD-01", "Mango Pad", 20m, "EUR", categoryId);
        SearchProductsQueryHandler handler = CreateSearchHandler(products);

        Result<PagedResult<ProductDto>> result = await handler.Handle(
            new SearchProductsQuery(sortBy: "Price", sortDirection: "Asc"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Select(dto => dto.PriceAmount).Should().Equal(10m, 20m, 30m);
    }

    [Fact]
    public async Task SearchProducts_SortsByPriceDescending()
    {
        var products = new FakeProductRepository();
        CategoryId categoryId = CategoryId.New();
        products.AddProduct("KEY-01", "Zebra Keyboard", 30m, "EUR", categoryId);
        products.AddProduct("MOUSE-01", "Alpha Mouse", 10m, "EUR", categoryId);
        products.AddProduct("PAD-01", "Mango Pad", 20m, "EUR", categoryId);
        SearchProductsQueryHandler handler = CreateSearchHandler(products);

        Result<PagedResult<ProductDto>> result = await handler.Handle(
            new SearchProductsQuery(sortBy: "Price", sortDirection: "Desc"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Select(dto => dto.PriceAmount).Should().Equal(30m, 20m, 10m);
    }

    [Fact]
    public async Task SearchProducts_SortsBySkuAscending()
    {
        var products = new FakeProductRepository();
        CategoryId categoryId = CategoryId.New();
        products.AddProduct("PAD-01", "Mango Pad", 30m, "EUR", categoryId);
        products.AddProduct("KEY-01", "Zebra Keyboard", 10m, "EUR", categoryId);
        products.AddProduct("MOUSE-01", "Alpha Mouse", 20m, "EUR", categoryId);
        SearchProductsQueryHandler handler = CreateSearchHandler(products);

        Result<PagedResult<ProductDto>> result = await handler.Handle(
            new SearchProductsQuery(sortBy: "Sku", sortDirection: "Asc"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Select(dto => dto.Sku).Should().Equal("KEY-01", "MOUSE-01", "PAD-01");
    }

    [Fact]
    public async Task SearchProducts_SupportsPaginationAcrossSortedResults()
    {
        var products = new FakeProductRepository();
        CategoryId categoryId = CategoryId.New();
        products.AddProduct("KEY-01", "Echo Keyboard", 10m, "EUR", categoryId);
        products.AddProduct("MOUSE-01", "Delta Mouse", 20m, "EUR", categoryId);
        products.AddProduct("PAD-01", "Charlie Pad", 30m, "EUR", categoryId);
        products.AddProduct("CABLE-01", "Bravo Cable", 40m, "EUR", categoryId);
        products.AddProduct("MON-01", "Alpha Monitor", 50m, "EUR", categoryId);
        SearchProductsQueryHandler handler = CreateSearchHandler(products);

        Result<PagedResult<ProductDto>> first = await handler.Handle(
            new SearchProductsQuery(pageNumber: 1, pageSize: 2, sortBy: "Name"),
            CancellationToken.None);

        Result<PagedResult<ProductDto>> second = await handler.Handle(
            new SearchProductsQuery(pageNumber: 2, pageSize: 2, sortBy: "Name"),
            CancellationToken.None);

        first.IsSuccess.Should().BeTrue();
        first.Value.Items.Select(dto => dto.Name).Should().Equal("Alpha Monitor", "Bravo Cable");
        first.Value.TotalCount.Should().Be(5);
        first.Value.TotalPages.Should().Be(3);

        second.IsSuccess.Should().BeTrue();
        second.Value.Items.Select(dto => dto.Name).Should().Equal("Charlie Pad", "Delta Mouse");
        second.Value.TotalCount.Should().Be(5);
    }

    [Fact]
    public async Task SearchProducts_ExcludesSoftDeletedProducts()
    {
        var products = new FakeProductRepository();
        CategoryId categoryId = CategoryId.New();
        Product live = products.AddProduct("KEY-01", "Mechanical Keyboard", 149.99m, "EUR", categoryId);
        Product deleted = products.AddProduct("MOUSE-01", "Gaming Mouse", 79.99m, "EUR", categoryId);
        deleted.IsDeleted = true;
        SearchProductsQueryHandler handler = CreateSearchHandler(products);

        Result<PagedResult<ProductDto>> result = await handler.Handle(
            new SearchProductsQuery(),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalCount.Should().Be(1);
        result.Value.Items.Should().ContainSingle(dto => dto.Id == live.Id.Value);
        result.Value.Items.Should().NotContain(dto => dto.Id == deleted.Id.Value);
    }
}
