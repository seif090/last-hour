using FluentValidation;
using LastHour.Modules.Identity.Domain.Users;

namespace LastHour.Modules.Identity.Application.Commands;

/// <summary>
/// Validates the structural shape of a login request: the email and password must be present and
/// the email must respect the domain length bound. Login deliberately does not apply the
/// registration password policy — a password only needs to be present and safely verified — and
/// the password is never normalized or transformed.
/// </summary>
public sealed class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LoginCommandValidator"/> class.
    /// </summary>
    public LoginCommandValidator()
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
