using FluentAssertions;
using LastHour.BuildingBlocks.SharedKernel.Results;
using LastHour.Modules.Catalog.Application.Commands;
using LastHour.Modules.Catalog.Domain.Categories;
using LastHour.Modules.Catalog.Domain.Products;
using LastHour.Modules.Catalog.Tests.Application.TestSupport;
using Microsoft.Extensions.Logging.Abstractions;

namespace LastHour.Modules.Catalog.Tests.Application.Commands;

public class ChangeProductPriceCommandHandlerTests
{
    private static ChangeProductPriceCommandHandler CreateHandler(FakeProductRepository products)
        => new ChangeProductPriceCommandHandler(products, NullLogger<ChangeProductPriceCommandHandler>.Instance);

    [Fact]
    public async Task Handle_ValidCommand_ChangesPriceAmountAndCurrency()
    {
        var products = new FakeProductRepository();
        var categoryId = CategoryId.New();
        Product product = products.AddProduct(
            "PHONE-001",
            "Smartphone",
            499.99m,
            "USD",
            categoryId,
            "Description");
        ChangeProductPriceCommandHandler handler = CreateHandler(products);

        Result result = await handler.Handle(
            new ChangeProductPriceCommand(product.Id.Value, 599.99m, "EUR"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        product.BasePrice.Amount.Should().Be(599.99m);
        product.BasePrice.Currency.Should().Be("EUR");
    }

    [Fact]
    public async Task Handle_ValidZeroAmount_ChangesPriceToZero()
    {
        var products = new FakeProductRepository();
        Product product = products.AddProduct(
            "PHONE-001",
            "Smartphone",
            499.99m,
            "USD",
            CategoryId.New());
        ChangeProductPriceCommandHandler handler = CreateHandler(products);

        Result result = await handler.Handle(
            new ChangeProductPriceCommand(product.Id.Value, 0m, "USD"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        product.BasePrice.Amount.Should().Be(0m);
        product.BasePrice.Currency.Should().Be("USD");
    }

    [Fact]
    public async Task Handle_ValidCommand_DoesNotModifyOtherProperties()
    {
        var products = new FakeProductRepository();
        var categoryId = CategoryId.New();
        Product product = products.AddProduct(
            "PHONE-001",
            "Smartphone",
            499.99m,
            "USD",
            categoryId,
            "Original Description");
        ChangeProductPriceCommandHandler handler = CreateHandler(products);

        Result result = await handler.Handle(
            new ChangeProductPriceCommand(product.Id.Value, 599.99m, "EUR"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        product.Sku.Value.Should().Be("PHONE-001");
        product.Name.Should().Be("Smartphone");
        product.Description.Should().Be("Original Description");
        product.CategoryId.Should().Be(categoryId);
    }

    [Fact]
    public async Task Handle_ProductDoesNotExist_ReturnsNotFoundError()
    {
        var products = new FakeProductRepository();
        ChangeProductPriceCommandHandler handler = CreateHandler(products);

        Result result = await handler.Handle(
            new ChangeProductPriceCommand(Guid.NewGuid(), 100m, "USD"),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.FirstError!.Type.Should().Be(ErrorType.NotFound);
        result.FirstError!.Code.Should().Be("Products.NotFound");
    }

    [Fact]
    public async Task Handle_NegativeAmount_ReturnsValidationFailure()
    {
        var products = new FakeProductRepository();
        Product product = products.AddProduct(
            "PHONE-001",
            "Smartphone",
            499.99m,
            "USD",
            CategoryId.New());
        ChangeProductPriceCommandHandler handler = CreateHandler(products);

        Result result = await handler.Handle(
            new ChangeProductPriceCommand(product.Id.Value, -50m, "USD"),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.FirstError!.Type.Should().Be(ErrorType.Validation);
        result.FirstError!.Code.Should().Be("InvalidPrice");
    }

    [Fact]
    public async Task Handle_InvalidCurrencyCode_ReturnsValidationFailure()
    {
        var products = new FakeProductRepository();
        Product product = products.AddProduct(
            "PHONE-001",
            "Smartphone",
            499.99m,
            "USD",
            CategoryId.New());
        ChangeProductPriceCommandHandler handler = CreateHandler(products);

        Result result = await handler.Handle(
            new ChangeProductPriceCommand(product.Id.Value, 100m, "US"),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.FirstError!.Type.Should().Be(ErrorType.Validation);
        result.FirstError!.Code.Should().Be("InvalidPrice");
    }
}
