using FluentValidation;
using LastHour.Modules.Catalog.Domain.Categories;

namespace LastHour.Modules.Catalog.Application.Commands;

/// <summary>
/// Validates the structural shape of a category creation request.
/// </summary>
public sealed class CreateCategoryCommandValidator : AbstractValidator<CreateCategoryCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CreateCategoryCommandValidator"/> class.
    /// </summary>
    public CreateCategoryCommandValidator()
    {
        RuleFor(command => command.Name)
            .NotEmpty()
            .WithErrorCode("CategoryNameRequired")
            .WithMessage("A category name is required.")
            .MaximumLength(Category.MaxNameLength)
            .WithErrorCode("CategoryNameTooLong")
            .WithMessage($"A category name cannot exceed {Category.MaxNameLength} characters.");

        RuleFor(command => command.Description)
            .MaximumLength(Category.MaxDescriptionLength)
            .WithErrorCode("CategoryDescriptionTooLong")
            .WithMessage($"A category description cannot exceed {Category.MaxDescriptionLength} characters.");
    }
}
