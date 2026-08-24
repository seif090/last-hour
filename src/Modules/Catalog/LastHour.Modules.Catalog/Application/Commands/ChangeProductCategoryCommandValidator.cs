using FluentValidation;

namespace LastHour.Modules.Catalog.Application.Commands;

/// <summary>
/// Validates the structural shape of a product category change request.
/// </summary>
public sealed class ChangeProductCategoryCommandValidator : AbstractValidator<ChangeProductCategoryCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ChangeProductCategoryCommandValidator"/> class.
    /// </summary>
    public ChangeProductCategoryCommandValidator()
    {
        RuleFor(command => command.ProductId)
            .NotEmpty()
            .WithErrorCode("ProductIdRequired")
            .WithMessage("A product identifier is required.");

        RuleFor(command => command.CategoryId)
            .NotEmpty()
            .WithErrorCode("CategoryIdRequired")
            .WithMessage("A category identifier is required.");
    }
}
