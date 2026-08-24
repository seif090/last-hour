using FluentAssertions;
using LastHour.BuildingBlocks.SharedKernel.Results;
using LastHour.Modules.Catalog.Application.Models;
using LastHour.Modules.Catalog.Application.Queries.Categories;
using LastHour.Modules.Catalog.Domain.Categories;
using LastHour.Modules.Catalog.Tests.Application.TestSupport;

namespace LastHour.Modules.Catalog.Tests.Application.Queries;

public class CategoryQueryHandlerTests
{
    private static GetCategoryByIdQueryHandler CreateByIdHandler(FakeCategoryRepository categories)
        => new GetCategoryByIdQueryHandler(categories);

    private static GetCategoriesQueryHandler CreateListHandler(FakeCategoryRepository categories)
        => new GetCategoriesQueryHandler(categories);

    [Fact]
    public async Task GetCategoryById_ExistingCategory_ReturnsCategoryDto()
    {
        var categories = new FakeCategoryRepository();
        Category parent = categories.AddCategory("Electronics", "Electronic gadgets");
        Category child = categories.AddCategory("Smartphones", "Mobile phones", parent.Id);
        GetCategoryByIdQueryHandler handler = CreateByIdHandler(categories);

        Result<CategoryDto> result = await handler.Handle(
            new GetCategoryByIdQuery(child.Id.Value),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be(child.Id.Value);
        result.Value.Name.Should().Be("Smartphones");
        result.Value.Description.Should().Be("Mobile phones");
        result.Value.ParentCategoryId.Should().Be(parent.Id.Value);
    }

    [Fact]
    public async Task GetCategoryById_MissingCategory_ReturnsNotFound()
    {
        var categories = new FakeCategoryRepository();
        categories.AddCategory("Electronics");
        GetCategoryByIdQueryHandler handler = CreateByIdHandler(categories);

        Result<CategoryDto> result = await handler.Handle(
            new GetCategoryByIdQuery(Guid.NewGuid()),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.FirstError!.Type.Should().Be(ErrorType.NotFound);
        result.FirstError!.Code.Should().Be("Categories.NotFound");
    }

    [Fact]
    public async Task GetCategories_ReturnsAllCategoriesAsDtos()
    {
        var categories = new FakeCategoryRepository();
        categories.AddCategory("Books");
        categories.AddCategory("Games");
        GetCategoriesQueryHandler handler = CreateListHandler(categories);

        Result<IReadOnlyList<CategoryDto>> result = await handler.Handle(
            new GetCategoriesQuery(),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        result.Value.Should().Contain(dto => dto.Name == "Books" && dto.ParentCategoryId == null);
        result.Value.Should().Contain(dto => dto.Name == "Games" && dto.ParentCategoryId == null);
    }

    [Fact]
    public async Task GetCategories_OrdersByNameAscendingRegardlessOfInsertionOrder()
    {
        var categories = new FakeCategoryRepository();
        Category zeta = categories.AddCategory("Zeta");
        categories.AddCategory("Alpha", parentCategoryId: zeta.Id);
        categories.AddCategory("Gamma");
        categories.AddCategory("Beta");
        GetCategoriesQueryHandler handler = CreateListHandler(categories);

        Result<IReadOnlyList<CategoryDto>> result = await handler.Handle(
            new GetCategoriesQuery(),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Select(dto => dto.Name).Should().Equal("Alpha", "Beta", "Gamma", "Zeta");
    }

    [Fact]
    public async Task GetCategories_NoCategories_ReturnsEmptyCollection()
    {
        GetCategoriesQueryHandler handler = CreateListHandler(new FakeCategoryRepository());

        Result<IReadOnlyList<CategoryDto>> result = await handler.Handle(
            new GetCategoriesQuery(),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }
}
