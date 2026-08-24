using FluentAssertions;
using FluentValidation.Results;
using LastHour.Modules.Catalog.Application.Commands;
using LastHour.Modules.Catalog.Domain.Categories;

namespace LastHour.Modules.Catalog.Tests.Application.Validation;

public class CategoryCommandValidatorTests
{
    [Fact]
    public void CreateCategoryValidator_ValidCommand_PassesValidation()
    {
        var validator = new CreateCategoryCommandValidator();
        var command = new CreateCategoryCommand("Books", "Printed and digital books", Guid.NewGuid());

        ValidationResult result = validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void CreateCategoryValidator_EmptyName_FailsValidation(string? name)
    {
        var validator = new CreateCategoryCommandValidator();
        var command = new CreateCategoryCommand(name!, "Description");

        ValidationResult result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorCode == "CategoryNameRequired");
    }

    [Fact]
    public void CreateCategoryValidator_OverlongName_FailsValidation()
    {
        var validator = new CreateCategoryCommandValidator();
        var command = new CreateCategoryCommand(new string('a', Category.MaxNameLength + 1), "Description");

        ValidationResult result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorCode == "CategoryNameTooLong");
    }

    [Fact]
    public void CreateCategoryValidator_OverlongDescription_FailsValidation()
    {
        var validator = new CreateCategoryCommandValidator();
        var command = new CreateCategoryCommand("Books", new string('a', Category.MaxDescriptionLength + 1));

        ValidationResult result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorCode == "CategoryDescriptionTooLong");
    }

    [Fact]
    public void UpdateCategoryValidator_ValidCommand_PassesValidation()
    {
        var validator = new UpdateCategoryCommandValidator();
        var command = new UpdateCategoryCommand(Guid.NewGuid(), "Books", "Updated description", Guid.NewGuid());

        ValidationResult result = validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void UpdateCategoryValidator_EmptyId_FailsValidation()
    {
        var validator = new UpdateCategoryCommandValidator();
        var command = new UpdateCategoryCommand(Guid.Empty, "Books", "Description");

        ValidationResult result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorCode == "CategoryIdRequired");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void UpdateCategoryValidator_EmptyName_FailsValidation(string? name)
    {
        var validator = new UpdateCategoryCommandValidator();
        var command = new UpdateCategoryCommand(Guid.NewGuid(), name!, "Description");

        ValidationResult result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorCode == "CategoryNameRequired");
    }

    [Fact]
    public void UpdateCategoryValidator_OverlongName_FailsValidation()
    {
        var validator = new UpdateCategoryCommandValidator();
        var command = new UpdateCategoryCommand(Guid.NewGuid(), new string('a', Category.MaxNameLength + 1), "Description");

        ValidationResult result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorCode == "CategoryNameTooLong");
    }

    [Fact]
    public void UpdateCategoryValidator_OverlongDescription_FailsValidation()
    {
        var validator = new UpdateCategoryCommandValidator();
        var command = new UpdateCategoryCommand(Guid.NewGuid(), "Books", new string('a', Category.MaxDescriptionLength + 1));

        ValidationResult result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorCode == "CategoryDescriptionTooLong");
    }
}
