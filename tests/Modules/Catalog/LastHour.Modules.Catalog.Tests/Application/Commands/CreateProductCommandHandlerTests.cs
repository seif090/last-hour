using FluentAssertions;
using LastHour.BuildingBlocks.SharedKernel.Results;
using LastHour.Modules.Catalog.Application.Commands;
using LastHour.Modules.Catalog.Domain.Categories;
using LastHour.Modules.Catalog.Domain.Products;
using LastHour.Modules.Catalog.Tests.Application.TestSupport;
using Microsoft.Extensions.Logging.Abstractions;

namespace LastHour.Modules.Catalog.Tests.Application.Commands;

public class CreateProductCommandHandlerTests
{
    private static CreateProductCommandHandler CreateHandler(
        FakeProductRepository products,
        FakeCategoryRepository categories)
        => new CreateProductCommandHandler(
            products,
            categories,
            NullLogger<CreateProductCommandHandler>.Instance);

    [Fact]
    public async Task Handle_ValidCommand_CreatesProductSuccessfully()
    {
        var products = new FakeProductRepository();
        var categories = new FakeCategoryRepository();
        Category category = categories.AddCategory("Electronics");
        CreateProductCommandHandler handler = CreateHandler(products, categories);

        var command = new CreateProductCommand(
            "PHONE-001",
            "Smartphone X",
            799.99m,
            "USD",
            category.Id.Value,
            "Flagship phone");

        Result<ProductId> result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        Product created = products.All.Should().ContainSingle().Subject;
        created.Id.Should().Be(result.Value);
        created.Sku.Value.Should().Be("PHONE-001");
        created.Sku.NormalizedValue.Should().Be("PHONE-001");
        created.Name.Should().Be("Smartphone X");
        created.BasePrice.Amount.Should().Be(799.99m);
        created.BasePrice.Currency.Should().Be("USD");
        created.CategoryId.Should().Be(category.Id);
        created.Description.Should().Be("Flagship phone");
    }

    [Fact]
    public async Task Handle_CategoryDoesNotExist_ReturnsNotFoundError()
    {
        var products = new FakeProductRepository();
        var categories = new FakeCategoryRepository();
        CreateProductCommandHandler handler = CreateHandler(products, categories);

        var command = new CreateProductCommand(
            "PHONE-001",
            "Smartphone X",
            799.99m,
            "USD",
            Guid.NewGuid());

        Result<ProductId> result = await handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.FirstError!.Type.Should().Be(ErrorType.NotFound);
        result.FirstError!.Code.Should().Be("Categories.NotFound");
        products.All.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_DuplicateNormalizedSku_ReturnsConflictError()
    {
        var products = new FakeProductRepository();
        var categories = new FakeCategoryRepository();
        Category category = categories.AddCategory("Electronics");
        products.AddProduct("PHONE-001", "Existing Phone", 500m, "USD", category.Id);
        CreateProductCommandHandler handler = CreateHandler(products, categories);

        var command = new CreateProductCommand(
            "phone-001", // Different casing, same normalized SKU
            "New Phone",
            600m,
            "USD",
            category.Id.Value);

        Result<ProductId> result = await handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.FirstError!.Type.Should().Be(ErrorType.Conflict);
        result.FirstError!.Code.Should().Be("Products.SkuAlreadyExists");
        products.All.Should().ContainSingle();
    }

    [Fact]
    public async Task Handle_InvalidSku_ReturnsValidationFailure()
    {
        var products = new FakeProductRepository();
        var categories = new FakeCategoryRepository();
        Category category = categories.AddCategory("Electronics");
        CreateProductCommandHandler handler = CreateHandler(products, categories);

        var command = new CreateProductCommand(
            "INVALID SKU WITH SPACES",
            "Smartphone X",
            799.99m,
            "USD",
            category.Id.Value);

        Result<ProductId> result = await handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.FirstError!.Type.Should().Be(ErrorType.Validation);
        result.FirstError!.Code.Should().Be("InvalidSku");
    }

    [Fact]
    public async Task Handle_NegativePrice_ReturnsValidationFailure()
    {
        var products = new FakeProductRepository();
        var categories = new FakeCategoryRepository();
        Category category = categories.AddCategory("Electronics");
        CreateProductCommandHandler handler = CreateHandler(products, categories);

        var command = new CreateProductCommand(
            "PHONE-001",
            "Smartphone X",
            -10m,
            "USD",
            category.Id.Value);

        Result<ProductId> result = await handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.FirstError!.Type.Should().Be(ErrorType.Validation);
        result.FirstError!.Code.Should().Be("InvalidPrice");
    }

    [Fact]
    public async Task Handle_InvalidCurrency_ReturnsValidationFailure()
    {
        var products = new FakeProductRepository();
        var categories = new FakeCategoryRepository();
        Category category = categories.AddCategory("Electronics");
        CreateProductCommandHandler handler = CreateHandler(products, categories);

        var command = new CreateProductCommand(
            "PHONE-001",
            "Smartphone X",
            100m,
            "INVALID",
            category.Id.Value);

        Result<ProductId> result = await handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.FirstError!.Type.Should().Be(ErrorType.Validation);
        result.FirstError!.Code.Should().Be("InvalidPrice");
    }

    [Fact]
    public async Task Handle_OverlongProductName_ReturnsValidationFailure()
    {
        var products = new FakeProductRepository();
        var categories = new FakeCategoryRepository();
        Category category = categories.AddCategory("Electronics");
        CreateProductCommandHandler handler = CreateHandler(products, categories);

        var command = new CreateProductCommand(
            "PHONE-001",
            new string('a', Product.MaxNameLength + 1),
            100m,
            "USD",
            category.Id.Value);

        Result<ProductId> result = await handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.FirstError!.Type.Should().Be(ErrorType.Validation);
        result.FirstError!.Code.Should().Be("InvalidProductName");
    }
}
