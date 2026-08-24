using FluentAssertions;
using LastHour.BuildingBlocks.SharedKernel.Results;
using LastHour.Modules.Catalog.Application.Commands;
using LastHour.Modules.Catalog.Domain.Categories;
using LastHour.Modules.Catalog.Domain.Products;
using LastHour.Modules.Catalog.Tests.Application.TestSupport;
using Microsoft.Extensions.Logging.Abstractions;

namespace LastHour.Modules.Catalog.Tests.Application.Commands;

public class ChangeProductCategoryCommandHandlerTests
{
    private static ChangeProductCategoryCommandHandler CreateHandler(
        FakeProductRepository products,
        FakeCategoryRepository categories)
        => new ChangeProductCategoryCommandHandler(
            products,
            categories,
            NullLogger<ChangeProductCategoryCommandHandler>.Instance);

    [Fact]
    public async Task Handle_ValidTargetCategory_ChangesCategorySuccessfully()
    {
        var products = new FakeProductRepository();
        var categories = new FakeCategoryRepository();
        Category oldCategory = categories.AddCategory("Old Category");
        Category newCategory = categories.AddCategory("New Category");
        Product product = products.AddProduct(
            "PHONE-001",
            "Smartphone",
            499.99m,
            "USD",
            oldCategory.Id,
            "Description");
        ChangeProductCategoryCommandHandler handler = CreateHandler(products, categories);

        Result result = await handler.Handle(
            new ChangeProductCategoryCommand(product.Id.Value, newCategory.Id.Value),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        product.CategoryId.Should().Be(newCategory.Id);
    }

    [Fact]
    public async Task Handle_SameCategory_ReturnsSuccessAsNoOp()
    {
        var products = new FakeProductRepository();
        var categories = new FakeCategoryRepository();
        Category category = categories.AddCategory("Category 1");
        Product product = products.AddProduct(
            "PHONE-001",
            "Smartphone",
            499.99m,
            "USD",
            category.Id);
        ChangeProductCategoryCommandHandler handler = CreateHandler(products, categories);

        Result result = await handler.Handle(
            new ChangeProductCategoryCommand(product.Id.Value, category.Id.Value),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        product.CategoryId.Should().Be(category.Id);
    }

    [Fact]
    public async Task Handle_ProductDoesNotExist_ReturnsNotFoundError()
    {
        var products = new FakeProductRepository();
        var categories = new FakeCategoryRepository();
        Category category = categories.AddCategory("Category 1");
        ChangeProductCategoryCommandHandler handler = CreateHandler(products, categories);

        Result result = await handler.Handle(
            new ChangeProductCategoryCommand(Guid.NewGuid(), category.Id.Value),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.FirstError!.Type.Should().Be(ErrorType.NotFound);
        result.FirstError!.Code.Should().Be("Products.NotFound");
    }

    [Fact]
    public async Task Handle_TargetCategoryDoesNotExist_ReturnsNotFoundError()
    {
        var products = new FakeProductRepository();
        var categories = new FakeCategoryRepository();
        Category category = categories.AddCategory("Category 1");
        Product product = products.AddProduct(
            "PHONE-001",
            "Smartphone",
            499.99m,
            "USD",
            category.Id);
        ChangeProductCategoryCommandHandler handler = CreateHandler(products, categories);

        Result result = await handler.Handle(
            new ChangeProductCategoryCommand(product.Id.Value, Guid.NewGuid()),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.FirstError!.Type.Should().Be(ErrorType.NotFound);
        result.FirstError!.Code.Should().Be("Categories.NotFound");
        product.CategoryId.Should().Be(category.Id);
    }

    [Fact]
    public async Task Handle_ValidCommand_PreservesOtherProductProperties()
    {
        var products = new FakeProductRepository();
        var categories = new FakeCategoryRepository();
        Category oldCategory = categories.AddCategory("Old Category");
        Category newCategory = categories.AddCategory("New Category");
        Product product = products.AddProduct(
            "PHONE-001",
            "Smartphone",
            499.99m,
            "USD",
            oldCategory.Id,
            "Original Description");
        ChangeProductCategoryCommandHandler handler = CreateHandler(products, categories);

        Result result = await handler.Handle(
            new ChangeProductCategoryCommand(product.Id.Value, newCategory.Id.Value),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        product.Sku.Value.Should().Be("PHONE-001");
        product.Name.Should().Be("Smartphone");
        product.Description.Should().Be("Original Description");
        product.BasePrice.Amount.Should().Be(499.99m);
        product.BasePrice.Currency.Should().Be("USD");
    }
}
