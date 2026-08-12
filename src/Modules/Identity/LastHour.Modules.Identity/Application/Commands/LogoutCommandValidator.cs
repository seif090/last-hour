using FluentValidation;

namespace LastHour.Modules.Identity.Application.Commands;

/// <summary>
/// Validates the structural shape of a logout request: the refresh token must be present and
/// respect the same generous length bound as refresh requests.
/// </summary>
public sealed class LogoutCommandValidator : AbstractValidator<LogoutCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LogoutCommandValidator"/> class.
    /// </summary>
    public LogoutCommandValidator()
    {
        RuleFor(command => command.RefreshToken)
            .NotEmpty()
            .WithErrorCode("RefreshTokenRequired")
            .WithMessage("A refresh token is required.")
            .MaximumLength(RefreshTokenCommandValidator.MaxRefreshTokenLength)
            .WithErrorCode("RefreshTokenTooLong")
            .WithMessage($"A refresh token cannot exceed {RefreshTokenCommandValidator.MaxRefreshTokenLength} characters.");
    }
}
