using FluentValidation;
using LastHour.Modules.Catalog.Domain.Products;

namespace LastHour.Modules.Catalog.Application.Commands;

/// <summary>
/// Validates the structural shape of a product SKU change request.
/// </summary>
public sealed class ChangeProductSkuCommandValidator : AbstractValidator<ChangeProductSkuCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ChangeProductSkuCommandValidator"/> class.
    /// </summary>
    public ChangeProductSkuCommandValidator()
    {
        RuleFor(command => command.ProductId)
            .NotEmpty()
            .WithErrorCode("ProductIdRequired")
            .WithMessage("A product identifier is required.");

        RuleFor(command => command.Sku)
            .NotEmpty()
            .WithErrorCode("SkuRequired")
            .WithMessage("A SKU is required.")
            .MaximumLength(Sku.MaxLength)
            .WithErrorCode("SkuTooLong")
            .WithMessage($"A SKU cannot exceed {Sku.MaxLength} characters.");
    }
}
