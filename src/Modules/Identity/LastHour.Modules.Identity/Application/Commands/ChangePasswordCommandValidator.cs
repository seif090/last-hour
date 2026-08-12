using FluentValidation;

namespace LastHour.Modules.Identity.Application.Commands;

/// <summary>
/// Validates the structural shape of a password-change request: both passwords must be present
/// and respect the same length bounds the policy enforces. The policy itself (minimum length,
/// maximum length) is applied by the credential service during the change, so the validator does
/// not duplicate the rules — it only rejects empty or obviously oversized payloads before any
/// hashing work.
/// </summary>
public sealed class ChangePasswordCommandValidator : AbstractValidator<ChangePasswordCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ChangePasswordCommandValidator"/> class.
    /// </summary>
    public ChangePasswordCommandValidator()
    {
        RuleFor(command => command.CurrentPassword)
            .NotEmpty()
            .WithErrorCode("CurrentPasswordRequired")
            .WithMessage("The current password is required.")
            .MaximumLength(1024)
            .WithErrorCode("CurrentPasswordTooLong")
            .WithMessage("The current password cannot exceed 1024 characters.");

        RuleFor(command => command.NewPassword)
            .NotEmpty()
            .WithErrorCode("NewPasswordRequired")
            .WithMessage("A new password is required.")
            .MaximumLength(1024)
            .WithErrorCode("NewPasswordTooLong")
            .WithMessage("The new password cannot exceed 1024 characters.");
    }
}
