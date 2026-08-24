using FluentValidation;
using LastHour.Modules.Catalog.Domain.Categories;

namespace LastHour.Modules.Catalog.Application.Commands;

/// <summary>
/// Validates the structural shape of a category update request.
/// </summary>
public sealed class UpdateCategoryCommandValidator : AbstractValidator<UpdateCategoryCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateCategoryCommandValidator"/> class.
    /// </summary>
    public UpdateCategoryCommandValidator()
    {
        RuleFor(command => command.Id)
            .NotEmpty()
            .WithErrorCode("CategoryIdRequired")
            .WithMessage("A category identifier is required.");

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
