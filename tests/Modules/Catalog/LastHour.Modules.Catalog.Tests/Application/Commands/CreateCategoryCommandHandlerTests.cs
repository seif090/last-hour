using FluentAssertions;
using LastHour.BuildingBlocks.SharedKernel.Results;
using LastHour.Modules.Catalog.Application.Commands;
using LastHour.Modules.Catalog.Domain.Categories;
using LastHour.Modules.Catalog.Tests.Application.TestSupport;
using Microsoft.Extensions.Logging.Abstractions;

namespace LastHour.Modules.Catalog.Tests.Application.Commands;

public class CreateCategoryCommandHandlerTests
{
    private static CreateCategoryCommandHandler CreateHandler(FakeCategoryRepository categories)
        => new CreateCategoryCommandHandler(categories, NullLogger<CreateCategoryCommandHandler>.Instance);

    [Fact]
    public async Task Handle_ValidRootCategory_CreatesCategorySuccessfully()
    {
        var categories = new FakeCategoryRepository();
        CreateCategoryCommandHandler handler = CreateHandler(categories);

        Result<CategoryId> result = await handler.Handle(
            new CreateCategoryCommand("Electronics", "Electronic gadgets and devices"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        Category created = categories.All.Should().ContainSingle().Subject;
        created.Id.Should().Be(result.Value);
        created.Name.Should().Be("Electronics");
        created.Description.Should().Be("Electronic gadgets and devices");
        created.ParentCategoryId.Should().BeNull();
    }

    [Fact]
    public async Task Handle_ValidCategoryWithParent_CreatesCategoryWithParent()
    {
        var categories = new FakeCategoryRepository();
        Category parent = categories.AddCategory("Electronics", "Parent category");
        CreateCategoryCommandHandler handler = CreateHandler(categories);

        Result<CategoryId> result = await handler.Handle(
            new CreateCategoryCommand("Smartphones", "Mobile phones", parent.Id.Value),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        categories.All.Should().HaveCount(2);
        Category created = categories.All.Single(c => c.Id == result.Value);
        created.Name.Should().Be("Smartphones");
        created.ParentCategoryId.Should().Be(parent.Id);
    }

    [Fact]
    public async Task Handle_ParentDoesNotExist_ReturnsNotFoundError()
    {
        var categories = new FakeCategoryRepository();
        CreateCategoryCommandHandler handler = CreateHandler(categories);
        var nonExistentParentId = Guid.NewGuid();

        Result<CategoryId> result = await handler.Handle(
            new CreateCategoryCommand("Smartphones", "Mobile phones", nonExistentParentId),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.FirstError!.Type.Should().Be(ErrorType.NotFound);
        result.FirstError!.Code.Should().Be("Categories.ParentNotFound");
        categories.All.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_EmptyName_ReturnsValidationFailure()
    {
        CreateCategoryCommandHandler handler = CreateHandler(new FakeCategoryRepository());

        Result<CategoryId> result = await handler.Handle(
            new CreateCategoryCommand("   ", "Description"),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.FirstError!.Type.Should().Be(ErrorType.Validation);
        result.FirstError!.Code.Should().Be("InvalidCategoryName");
    }

    [Fact]
    public async Task Handle_OverlongName_ReturnsValidationFailure()
    {
        CreateCategoryCommandHandler handler = CreateHandler(new FakeCategoryRepository());

        Result<CategoryId> result = await handler.Handle(
            new CreateCategoryCommand(new string('a', Category.MaxNameLength + 1), "Description"),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.FirstError!.Type.Should().Be(ErrorType.Validation);
        result.FirstError!.Code.Should().Be("InvalidCategoryName");
    }

    [Fact]
    public async Task Handle_OverlongDescription_ReturnsValidationFailure()
    {
        CreateCategoryCommandHandler handler = CreateHandler(new FakeCategoryRepository());

        Result<CategoryId> result = await handler.Handle(
            new CreateCategoryCommand("Books", new string('a', Category.MaxDescriptionLength + 1)),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.FirstError!.Type.Should().Be(ErrorType.Validation);
        result.FirstError!.Code.Should().Be("InvalidCategoryName");
    }
}
