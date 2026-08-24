using FluentValidation;

namespace LastHour.Modules.Merchant.Application.Commands;

/// <summary>
/// Validates the structural shape of a merchant suspension request.
/// </summary>
public sealed class SuspendMerchantCommandValidator : AbstractValidator<SuspendMerchantCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SuspendMerchantCommandValidator"/> class.
    /// </summary>
    public SuspendMerchantCommandValidator()
    {
        RuleFor(command => command.MerchantId)
            .NotEmpty()
            .WithErrorCode("MerchantIdRequired")
            .WithMessage("A merchant identifier is required.");
    }
}
