using FluentValidation;

namespace LastHour.Modules.Merchant.Application.Commands;

/// <summary>
/// Validates the structural shape of a merchant verification request.
/// </summary>
public sealed class VerifyMerchantCommandValidator : AbstractValidator<VerifyMerchantCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="VerifyMerchantCommandValidator"/> class.
    /// </summary>
    public VerifyMerchantCommandValidator()
    {
        RuleFor(command => command.MerchantId)
            .NotEmpty()
            .WithErrorCode("MerchantIdRequired")
            .WithMessage("A merchant identifier is required.");
    }
}
