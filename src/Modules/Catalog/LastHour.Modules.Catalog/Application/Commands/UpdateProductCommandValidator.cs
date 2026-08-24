using FluentValidation;
using LastHour.Modules.Catalog.Domain.Products;

namespace LastHour.Modules.Catalog.Application.Commands;

/// <summary>
/// Validates the structural shape of a product update request.
/// </summary>
public sealed class UpdateProductCommandValidator : AbstractValidator<UpdateProductCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateProductCommandValidator"/> class.
    /// </summary>
    public UpdateProductCommandValidator()
    {
        RuleFor(command => command.Id)
            .NotEmpty()
            .WithErrorCode("ProductIdRequired")
            .WithMessage("A product identifier is required.");

        RuleFor(command => command.Name)
            .NotEmpty()
            .WithErrorCode("ProductNameRequired")
            .WithMessage("A product name is required.")
            .MaximumLength(Product.MaxNameLength)
            .WithErrorCode("ProductNameTooLong")
            .WithMessage($"A product name cannot exceed {Product.MaxNameLength} characters.");

        RuleFor(command => command.Description)
            .MaximumLength(Product.MaxDescriptionLength)
            .WithErrorCode("ProductDescriptionTooLong")
            .WithMessage($"A product description cannot exceed {Product.MaxDescriptionLength} characters.");
    }
}
