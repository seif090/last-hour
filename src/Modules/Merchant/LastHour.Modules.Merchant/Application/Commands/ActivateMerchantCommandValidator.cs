using FluentValidation;

namespace LastHour.Modules.Merchant.Application.Commands;

/// <summary>
/// Validates the structural shape of a merchant activation request.
/// </summary>
public sealed class ActivateMerchantCommandValidator : AbstractValidator<ActivateMerchantCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ActivateMerchantCommandValidator"/> class.
    /// </summary>
    public ActivateMerchantCommandValidator()
    {
        RuleFor(command => command.MerchantId)
            .NotEmpty()
            .WithErrorCode("MerchantIdRequired")
            .WithMessage("A merchant identifier is required.");
    }
}
