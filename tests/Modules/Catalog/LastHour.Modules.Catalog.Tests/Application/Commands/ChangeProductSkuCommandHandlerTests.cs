using FluentAssertions;
using LastHour.BuildingBlocks.SharedKernel.Results;
using LastHour.Modules.Catalog.Application.Commands;
using LastHour.Modules.Catalog.Domain.Categories;
using LastHour.Modules.Catalog.Domain.Products;
using LastHour.Modules.Catalog.Tests.Application.TestSupport;
using Microsoft.Extensions.Logging.Abstractions;

namespace LastHour.Modules.Catalog.Tests.Application.Commands;

public class ChangeProductSkuCommandHandlerTests
{
    private static ChangeProductSkuCommandHandler CreateHandler(FakeProductRepository products)
        => new ChangeProductSkuCommandHandler(products, NullLogger<ChangeProductSkuCommandHandler>.Instance);

    [Fact]
    public async Task Handle_ValidNewSku_ChangesSkuSuccessfully()
    {
        var products = new FakeProductRepository();
        var categoryId = CategoryId.New();
        Product product = products.AddProduct(
            "OLD-SKU-001",
            "Smartphone",
            499.99m,
            "USD",
            categoryId,
            "Description");
        ChangeProductSkuCommandHandler handler = CreateHandler(products);

        Result result = await handler.Handle(
            new ChangeProductSkuCommand(product.Id.Value, "NEW-SKU-002"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        product.Sku.Value.Should().Be("NEW-SKU-002");
        product.Sku.NormalizedValue.Should().Be("NEW-SKU-002");
    }

    [Fact]
    public async Task Handle_ValidNewSku_NormalizesSkuProperly()
    {
        var products = new FakeProductRepository();
        Product product = products.AddProduct(
            "OLD-SKU-001",
            "Smartphone",
            499.99m,
            "USD",
            CategoryId.New());
        ChangeProductSkuCommandHandler handler = CreateHandler(products);

        Result result = await handler.Handle(
            new ChangeProductSkuCommand(product.Id.Value, "  new-sku-002  "),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        product.Sku.Value.Should().Be("new-sku-002");
        product.Sku.NormalizedValue.Should().Be("NEW-SKU-002");
    }

    [Fact]
    public async Task Handle_SameNormalizedSku_IsSuccessfulNoOpWithoutDuplicateLookup()
    {
        var products = new FakeProductRepository();
        Product product = products.AddProduct(
            "SKU-001",
            "Smartphone",
            499.99m,
            "USD",
            CategoryId.New());
        ChangeProductSkuCommandHandler handler = CreateHandler(products);

        Result result = await handler.Handle(
            new ChangeProductSkuCommand(product.Id.Value, "sku-001"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        products.ExistsByNormalizedSkuCallsCount.Should().Be(0);
    }

    [Fact]
    public async Task Handle_ProductDoesNotExist_ReturnsNotFoundError()
    {
        var products = new FakeProductRepository();
        ChangeProductSkuCommandHandler handler = CreateHandler(products);

        Result result = await handler.Handle(
            new ChangeProductSkuCommand(Guid.NewGuid(), "NEW-SKU"),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.FirstError!.Type.Should().Be(ErrorType.NotFound);
        result.FirstError!.Code.Should().Be("Products.NotFound");
    }

    [Fact]
    public async Task Handle_NormalizedSkuAlreadyExistsOnAnotherProduct_ReturnsConflictError()
    {
        var products = new FakeProductRepository();
        var categoryId = CategoryId.New();
        Product product1 = products.AddProduct("SKU-001", "Phone 1", 100m, "USD", categoryId);
        products.AddProduct("SKU-002", "Phone 2", 200m, "USD", categoryId);
        ChangeProductSkuCommandHandler handler = CreateHandler(products);

        Result result = await handler.Handle(
            new ChangeProductSkuCommand(product1.Id.Value, "sku-002"),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.FirstError!.Type.Should().Be(ErrorType.Conflict);
        result.FirstError!.Code.Should().Be("Products.SkuAlreadyExists");
        product1.Sku.Value.Should().Be("SKU-001");
    }

    [Fact]
    public async Task Handle_InvalidSku_ReturnsValidationFailure()
    {
        var products = new FakeProductRepository();
        Product product = products.AddProduct(
            "SKU-001",
            "Smartphone",
            499.99m,
            "USD",
            CategoryId.New());
        ChangeProductSkuCommandHandler handler = CreateHandler(products);

        Result result = await handler.Handle(
            new ChangeProductSkuCommand(product.Id.Value, "INVALID SKU WITH SPACES"),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.FirstError!.Type.Should().Be(ErrorType.Validation);
        result.FirstError!.Code.Should().Be("InvalidSku");
    }

    [Fact]
    public async Task Handle_ValidCommand_PreservesOtherProductProperties()
    {
        var products = new FakeProductRepository();
        var categoryId = CategoryId.New();
        Product product = products.AddProduct(
            "OLD-SKU-001",
            "Smartphone",
            499.99m,
            "USD",
            categoryId,
            "Original Description");
        ChangeProductSkuCommandHandler handler = CreateHandler(products);

        Result result = await handler.Handle(
            new ChangeProductSkuCommand(product.Id.Value, "NEW-SKU-002"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        product.Name.Should().Be("Smartphone");
        product.Description.Should().Be("Original Description");
        product.BasePrice.Amount.Should().Be(499.99m);
        product.BasePrice.Currency.Should().Be("USD");
        product.CategoryId.Should().Be(categoryId);
    }
}
