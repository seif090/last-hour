using FluentAssertions;
using LastHour.BuildingBlocks.SharedKernel.Results;
using LastHour.Modules.Catalog.Application.Commands;
using LastHour.Modules.Catalog.Domain.Categories;
using LastHour.Modules.Catalog.Domain.Products;
using LastHour.Modules.Catalog.Tests.Application.TestSupport;
using Microsoft.Extensions.Logging.Abstractions;

namespace LastHour.Modules.Catalog.Tests.Application.Commands;

public class UpdateProductCommandHandlerTests
{
    private static UpdateProductCommandHandler CreateHandler(FakeProductRepository products)
        => new UpdateProductCommandHandler(products, NullLogger<UpdateProductCommandHandler>.Instance);

    [Fact]
    public async Task Handle_ValidCommand_UpdatesNameAndDescription()
    {
        var products = new FakeProductRepository();
        var categoryId = CategoryId.New();
        Product product = products.AddProduct(
            "PHONE-001",
            "Old Name",
            499.99m,
            "USD",
            categoryId,
            "Old Description");
        UpdateProductCommandHandler handler = CreateHandler(products);

        Result result = await handler.Handle(
            new UpdateProductCommand(product.Id.Value, "New Name", "New Description"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        product.Name.Should().Be("New Name");
        product.Description.Should().Be("New Description");
    }

    [Fact]
    public async Task Handle_ValidCommand_DoesNotModifySkuPriceOrCategory()
    {
        var products = new FakeProductRepository();
        var categoryId = CategoryId.New();
        Product product = products.AddProduct(
            "PHONE-001",
            "Old Name",
            499.99m,
            "USD",
            categoryId,
            "Old Description");
        UpdateProductCommandHandler handler = CreateHandler(products);

        Result result = await handler.Handle(
            new UpdateProductCommand(product.Id.Value, "New Name", "New Description"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        product.Sku.Value.Should().Be("PHONE-001");
        product.BasePrice.Amount.Should().Be(499.99m);
        product.BasePrice.Currency.Should().Be("USD");
        product.CategoryId.Should().Be(categoryId);
    }

    [Fact]
    public async Task Handle_ProductDoesNotExist_ReturnsNotFoundError()
    {
        var products = new FakeProductRepository();
        UpdateProductCommandHandler handler = CreateHandler(products);

        Result result = await handler.Handle(
            new UpdateProductCommand(Guid.NewGuid(), "New Name", "New Description"),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.FirstError!.Type.Should().Be(ErrorType.NotFound);
        result.FirstError!.Code.Should().Be("Products.NotFound");
    }

    [Fact]
    public async Task Handle_EmptyName_ReturnsValidationFailure()
    {
        var products = new FakeProductRepository();
        Product product = products.AddProduct("PHONE-001", "Old Name", 100m, "USD", CategoryId.New());
        UpdateProductCommandHandler handler = CreateHandler(products);

        Result result = await handler.Handle(
            new UpdateProductCommand(product.Id.Value, "   ", "Description"),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.FirstError!.Type.Should().Be(ErrorType.Validation);
        result.FirstError!.Code.Should().Be("InvalidProductName");
    }

    [Fact]
    public async Task Handle_OverlongName_ReturnsValidationFailure()
    {
        var products = new FakeProductRepository();
        Product product = products.AddProduct("PHONE-001", "Old Name", 100m, "USD", CategoryId.New());
        UpdateProductCommandHandler handler = CreateHandler(products);

        Result result = await handler.Handle(
            new UpdateProductCommand(product.Id.Value, new string('a', Product.MaxNameLength + 1), "Description"),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.FirstError!.Type.Should().Be(ErrorType.Validation);
        result.FirstError!.Code.Should().Be("InvalidProductName");
    }

    [Fact]
    public async Task Handle_OverlongDescription_ReturnsValidationFailure()
    {
        var products = new FakeProductRepository();
        Product product = products.AddProduct("PHONE-001", "Old Name", 100m, "USD", CategoryId.New());
        UpdateProductCommandHandler handler = CreateHandler(products);

        Result result = await handler.Handle(
            new UpdateProductCommand(product.Id.Value, "Name", new string('a', Product.MaxDescriptionLength + 1)),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.FirstError!.Type.Should().Be(ErrorType.Validation);
        result.FirstError!.Code.Should().Be("InvalidProductName");
    }
}
