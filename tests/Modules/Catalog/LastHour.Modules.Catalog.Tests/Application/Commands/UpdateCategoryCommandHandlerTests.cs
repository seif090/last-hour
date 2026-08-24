using FluentAssertions;
using LastHour.BuildingBlocks.SharedKernel.Results;
using LastHour.Modules.Catalog.Application.Commands;
using LastHour.Modules.Catalog.Domain.Categories;
using LastHour.Modules.Catalog.Tests.Application.TestSupport;
using Microsoft.Extensions.Logging.Abstractions;

namespace LastHour.Modules.Catalog.Tests.Application.Commands;

public class UpdateCategoryCommandHandlerTests
{
    private static UpdateCategoryCommandHandler CreateHandler(FakeCategoryRepository categories)
        => new UpdateCategoryCommandHandler(categories, NullLogger<UpdateCategoryCommandHandler>.Instance);

    [Fact]
    public async Task Handle_ValidCommand_UpdatesNameAndDescription()
    {
        var categories = new FakeCategoryRepository();
        Category category = categories.AddCategory("Computers", "Old description");
        UpdateCategoryCommandHandler handler = CreateHandler(categories);

        Result result = await handler.Handle(
            new UpdateCategoryCommand(category.Id.Value, "Laptops", "Updated description"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        category.Name.Should().Be("Laptops");
        category.Description.Should().Be("Updated description");
        category.ParentCategoryId.Should().BeNull();
    }

    [Fact]
    public async Task Handle_ValidCommandWithParent_UpdatesParentCategory()
    {
        var categories = new FakeCategoryRepository();
        Category parent = categories.AddCategory("Electronics", "Parent category");
        Category child = categories.AddCategory("Computers", "Child category");
        UpdateCategoryCommandHandler handler = CreateHandler(categories);

        Result result = await handler.Handle(
            new UpdateCategoryCommand(child.Id.Value, "Laptops", "Updated description", parent.Id.Value),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        child.Name.Should().Be("Laptops");
        child.ParentCategoryId.Should().Be(parent.Id);
    }

    [Fact]
    public async Task Handle_CategoryNotFound_ReturnsNotFoundError()
    {
        var categories = new FakeCategoryRepository();
        UpdateCategoryCommandHandler handler = CreateHandler(categories);

        Result result = await handler.Handle(
            new UpdateCategoryCommand(Guid.NewGuid(), "Laptops", "Description"),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.FirstError!.Type.Should().Be(ErrorType.NotFound);
        result.FirstError!.Code.Should().Be("Categories.NotFound");
    }

    [Fact]
    public async Task Handle_ParentNotFound_ReturnsNotFoundError()
    {
        var categories = new FakeCategoryRepository();
        Category category = categories.AddCategory("Computers", "Description");
        UpdateCategoryCommandHandler handler = CreateHandler(categories);

        Result result = await handler.Handle(
            new UpdateCategoryCommand(category.Id.Value, "Computers", "Description", Guid.NewGuid()),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.FirstError!.Type.Should().Be(ErrorType.NotFound);
        result.FirstError!.Code.Should().Be("Categories.ParentNotFound");
    }

    [Fact]
    public async Task Handle_SelfParenting_ReturnsValidationFailure()
    {
        var categories = new FakeCategoryRepository();
        Category category = categories.AddCategory("Computers", "Description");
        UpdateCategoryCommandHandler handler = CreateHandler(categories);

        Result result = await handler.Handle(
            new UpdateCategoryCommand(category.Id.Value, "Computers", "Description", category.Id.Value),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.FirstError!.Type.Should().Be(ErrorType.Validation);
        result.FirstError!.Code.Should().Be("Categories.SelfParentingNotAllowed");
    }

    [Fact]
    public async Task Handle_EmptyName_ReturnsValidationFailure()
    {
        var categories = new FakeCategoryRepository();
        Category category = categories.AddCategory("Computers", "Description");
        UpdateCategoryCommandHandler handler = CreateHandler(categories);

        Result result = await handler.Handle(
            new UpdateCategoryCommand(category.Id.Value, "   ", "Description"),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.FirstError!.Type.Should().Be(ErrorType.Validation);
        result.FirstError!.Code.Should().Be("InvalidCategoryName");
    }

    [Fact]
    public async Task Handle_OverlongName_ReturnsValidationFailure()
    {
        var categories = new FakeCategoryRepository();
        Category category = categories.AddCategory("Computers", "Description");
        UpdateCategoryCommandHandler handler = CreateHandler(categories);

        Result result = await handler.Handle(
            new UpdateCategoryCommand(category.Id.Value, new string('a', Category.MaxNameLength + 1), "Description"),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.FirstError!.Type.Should().Be(ErrorType.Validation);
        result.FirstError!.Code.Should().Be("InvalidCategoryName");
    }

    [Fact]
    public async Task Handle_OverlongDescription_ReturnsValidationFailure()
    {
        var categories = new FakeCategoryRepository();
        Category category = categories.AddCategory("Computers", "Description");
        UpdateCategoryCommandHandler handler = CreateHandler(categories);

        Result result = await handler.Handle(
            new UpdateCategoryCommand(category.Id.Value, "Computers", new string('a', Category.MaxDescriptionLength + 1)),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.FirstError!.Type.Should().Be(ErrorType.Validation);
        result.FirstError!.Code.Should().Be("InvalidCategoryName");
    }
}
