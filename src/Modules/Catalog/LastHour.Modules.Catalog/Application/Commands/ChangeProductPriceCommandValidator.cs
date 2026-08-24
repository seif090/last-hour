using FluentValidation;

namespace LastHour.Modules.Catalog.Application.Commands;

/// <summary>
/// Validates the structural shape of a product price change request.
/// </summary>
public sealed class ChangeProductPriceCommandValidator : AbstractValidator<ChangeProductPriceCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ChangeProductPriceCommandValidator"/> class.
    /// </summary>
    public ChangeProductPriceCommandValidator()
    {
        RuleFor(command => command.ProductId)
            .NotEmpty()
            .WithErrorCode("ProductIdRequired")
            .WithMessage("A product identifier is required.");

        RuleFor(command => command.Amount)
            .GreaterThanOrEqualTo(0m)
            .WithErrorCode("PriceAmountNegative")
            .WithMessage("Price amount cannot be negative.");

        RuleFor(command => command.Currency)
            .NotEmpty()
            .WithErrorCode("PriceCurrencyRequired")
            .WithMessage("Currency code is required.")
            .Length(3)
            .WithErrorCode("PriceCurrencyInvalidLength")
            .WithMessage("Currency code must be exactly 3 characters.");
    }
}
