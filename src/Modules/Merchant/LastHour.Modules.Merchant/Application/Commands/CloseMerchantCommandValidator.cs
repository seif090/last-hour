using FluentValidation;

namespace LastHour.Modules.Merchant.Application.Commands;

/// <summary>
/// Validates the structural shape of a merchant closure request.
/// </summary>
public sealed class CloseMerchantCommandValidator : AbstractValidator<CloseMerchantCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CloseMerchantCommandValidator"/> class.
    /// </summary>
    public CloseMerchantCommandValidator()
    {
        RuleFor(command => command.MerchantId)
            .NotEmpty()
            .WithErrorCode("MerchantIdRequired")
            .WithMessage("A merchant identifier is required.");
    }
}
