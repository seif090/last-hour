using FluentAssertions;
using LastHour.BuildingBlocks.SharedKernel.Results;
using LastHour.Modules.Catalog.Application.Commands;
using LastHour.Modules.Catalog.Domain.Categories;
using LastHour.Modules.Catalog.Domain.Products;
using LastHour.Modules.Catalog.Tests.Application.TestSupport;
using Microsoft.Extensions.Logging.Abstractions;

namespace LastHour.Modules.Catalog.Tests.Application.Commands;

public class DeleteProductCommandHandlerTests
{
    private static DeleteProductCommandHandler CreateHandler(FakeProductRepository products)
        => new DeleteProductCommandHandler(products, NullLogger<DeleteProductCommandHandler>.Instance);

    [Fact]
    public async Task Handle_ExistingProduct_RemovesProductFromRepository()
    {
        var products = new FakeProductRepository();
        Product product = products.AddProduct(
            "PHONE-001",
            "Smartphone",
            499.99m,
            "USD",
            CategoryId.New());
        DeleteProductCommandHandler handler = CreateHandler(products);

        Result result = await handler.Handle(
            new DeleteProductCommand(product.Id.Value),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        products.RemoveCallsCount.Should().Be(1);
        products.All.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_ProductDoesNotExist_ReturnsNotFoundError()
    {
        var products = new FakeProductRepository();
        DeleteProductCommandHandler handler = CreateHandler(products);

        Result result = await handler.Handle(
            new DeleteProductCommand(Guid.NewGuid()),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.FirstError!.Type.Should().Be(ErrorType.NotFound);
        result.FirstError!.Code.Should().Be("Products.NotFound");
        products.RemoveCallsCount.Should().Be(0);
    }

    [Fact]
    public async Task Handle_ExistingProduct_DelegatesToRepositoryWithoutMutatingSoftDeleteDirectly()
    {
        var products = new FakeProductRepository();
        Product product = products.AddProduct(
            "PHONE-001",
            "Smartphone",
            499.99m,
            "USD",
            CategoryId.New());
        DeleteProductCommandHandler handler = CreateHandler(products);

        Result result = await handler.Handle(
            new DeleteProductCommand(product.Id.Value),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        // The handler delegates entity deletion to repository Remove; SoftDeleteInterceptor sets IsDeleted in EF Core pipeline
        product.IsDeleted.Should().BeFalse();
    }
}
