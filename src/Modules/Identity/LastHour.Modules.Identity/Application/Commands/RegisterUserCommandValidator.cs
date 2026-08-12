using FluentValidation;
using LastHour.Modules.Identity.Domain.Users;

namespace LastHour.Modules.Identity.Application.Commands;

/// <summary>
/// Validates the structural shape of a registration request: required fields and the email
/// length bound. Email format and the password policy are enforced by the domain and the
/// credential service respectively (their single sources of truth), so this validator never
/// re-implements those rules.
/// </summary>
public sealed class RegisterUserCommandValidator : AbstractValidator<RegisterUserCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RegisterUserCommandValidator"/> class.
    /// </summary>
    public RegisterUserCommandValidator()
    {
        RuleFor(command => command.Email)
            .NotEmpty()
            .WithErrorCode("EmailRequired")
            .WithMessage("An email address is required.")
            .MaximumLength(Email.MaxLength)
            .WithErrorCode("EmailTooLong")
            .WithMessage($"An email address cannot exceed {Email.MaxLength} characters.");

        RuleFor(command => command.Password)
            .NotEmpty()
            .WithErrorCode("PasswordRequired")
            .WithMessage("A password is required.");
    }
}
