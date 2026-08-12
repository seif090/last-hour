using FluentValidation;

namespace LastHour.Modules.Identity.Application.Commands;

/// <summary>
/// Validates the structural shape of a refresh request: the token must be present and respect a
/// generous length bound. Real refresh tokens are 43 characters; the bound exists only to reject
/// obviously malformed or abusive payloads before hashing.
/// </summary>
public sealed class RefreshTokenCommandValidator : AbstractValidator<RefreshTokenCommand>
{
    /// <summary>
    /// The maximum accepted length of a submitted refresh token. Real tokens are 43 characters;
    /// the headroom rejects garbage without ever constraining the format.
    /// </summary>
    public const int MaxRefreshTokenLength = 256;

    /// <summary>
    /// Initializes a new instance of the <see cref="RefreshTokenCommandValidator"/> class.
    /// </summary>
    public RefreshTokenCommandValidator()
    {
        RuleFor(command => command.RefreshToken)
            .NotEmpty()
            .WithErrorCode("RefreshTokenRequired")
            .WithMessage("A refresh token is required.")
            .MaximumLength(MaxRefreshTokenLength)
            .WithErrorCode("RefreshTokenTooLong")
            .WithMessage($"A refresh token cannot exceed {MaxRefreshTokenLength} characters.");
    }
}
