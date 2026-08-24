using FluentValidation;
using LastHour.Modules.Catalog.Domain.Products;

namespace LastHour.Modules.Catalog.Application.Commands;

/// <summary>
/// Validates the structural shape of a product creation request.
/// </summary>
public sealed class CreateProductCommandValidator : AbstractValidator<CreateProductCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CreateProductCommandValidator"/> class.
    /// </summary>
    public CreateProductCommandValidator()
    {
        RuleFor(command => command.Sku)
            .NotEmpty()
            .WithErrorCode("SkuRequired")
            .WithMessage("A SKU is required.")
            .MaximumLength(Sku.MaxLength)
            .WithErrorCode("SkuTooLong")
            .WithMessage($"A SKU cannot exceed {Sku.MaxLength} characters.");

        RuleFor(command => command.Name)
            .NotEmpty()
            .WithErrorCode("ProductNameRequired")
            .WithMessage("A product name is required.")
            .MaximumLength(Product.MaxNameLength)
            .WithErrorCode("ProductNameTooLong")
            .WithMessage($"A product name cannot exceed {Product.MaxNameLength} characters.");

        RuleFor(command => command.PriceAmount)
            .GreaterThanOrEqualTo(0m)
            .WithErrorCode("PriceAmountNegative")
            .WithMessage("Price amount cannot be negative.");

        RuleFor(command => command.PriceCurrency)
            .NotEmpty()
            .WithErrorCode("PriceCurrencyRequired")
            .WithMessage("Currency code is required.")
            .Length(3)
            .WithErrorCode("PriceCurrencyInvalidLength")
            .WithMessage("Currency code must be exactly 3 characters.");

        RuleFor(command => command.CategoryId)
            .NotEmpty()
            .WithErrorCode("CategoryIdRequired")
            .WithMessage("A category identifier is required.");

        RuleFor(command => command.Description)
            .MaximumLength(Product.MaxDescriptionLength)
            .WithErrorCode("ProductDescriptionTooLong")
            .WithMessage($"A product description cannot exceed {Product.MaxDescriptionLength} characters.");
    }
}
