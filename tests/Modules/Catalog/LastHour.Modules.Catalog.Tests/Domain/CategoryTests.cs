using FluentAssertions;
using LastHour.BuildingBlocks.SharedKernel.Domain.Exceptions;
using LastHour.Modules.Catalog.Domain.Categories;

namespace LastHour.Modules.Catalog.Tests.Domain;

public class CategoryTests
{
    [Fact]
    public void Create_ValidCategory_SetsProperties()
    {
        Category category = Category.Create("Electronics", "Gadgets and devices");

        category.Id.Value.Should().NotBe(Guid.Empty);
        category.Name.Should().Be("Electronics");
        category.Description.Should().Be("Gadgets and devices");
        category.ParentCategoryId.Should().BeNull();
    }

    [Fact]
    public void Create_WithParentCategory_SetsParentCategoryId()
    {
        CategoryId parentId = CategoryId.New();
        Category category = Category.Create("Smartphones", "Mobile phones", parentId);

        category.ParentCategoryId.Should().Be(parentId);
    }

    [Fact]
    public void Create_WhitespacePaddedName_IsTrimmed()
    {
        Category category = Category.Create("  Books  ", "  Reading material  ");

        category.Name.Should().Be("Books");
        category.Description.Should().Be("Reading material");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_NullOrWhiteSpaceName_ThrowsInvalidCategoryNameException(string? name)
    {
        Action act = () => Category.Create(name!);

        act.Should().Throw<InvalidCategoryNameException>()
            .WithMessage("A category name is required.");
    }

    [Fact]
    public void Create_OverlongName_ThrowsInvalidCategoryNameException()
    {
        string longName = new string('a', Category.MaxNameLength + 1);

        Action act = () => Category.Create(longName);

        act.Should().Throw<InvalidCategoryNameException>()
            .WithMessage($"A category name cannot exceed {Category.MaxNameLength} characters.");
    }

    [Fact]
    public void Create_OverlongDescription_ThrowsInvalidCategoryNameException()
    {
        string longDescription = new string('a', Category.MaxDescriptionLength + 1);

        Action act = () => Category.Create("Electronics", longDescription);

        act.Should().Throw<InvalidCategoryNameException>()
            .WithMessage($"A category description cannot exceed {Category.MaxDescriptionLength} characters.");
    }

    [Fact]
    public void Update_ValidData_UpdatesProperties()
    {
        Category category = Category.Create("Old Name", "Old Description");
        CategoryId newParentId = CategoryId.New();

        category.Update("New Name", "New Description", newParentId);

        category.Name.Should().Be("New Name");
        category.Description.Should().Be("New Description");
        category.ParentCategoryId.Should().Be(newParentId);
    }

    [Fact]
    public void Update_SelfParent_ThrowsBusinessRuleValidationException()
    {
        Category category = Category.Create("Electronics");

        Action act = () => category.Update("Electronics", null, category.Id);

        act.Should().Throw<BusinessRuleValidationException>()
            .WithMessage("A category cannot reference itself as its parent category.");
    }

    [Fact]
    public void Update_NullOrWhiteSpaceName_ThrowsInvalidCategoryNameException()
    {
        Category category = Category.Create("Electronics");

        Action act = () => category.Update("   ");

        act.Should().Throw<InvalidCategoryNameException>();
    }
}
