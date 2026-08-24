using FluentValidation;

namespace LastHour.Modules.Identity.Application.Commands;

/// <summary>
/// Validates the structural shape of a role removal: both identifiers must be present.
/// </summary>
public sealed class RemoveRoleFromUserCommandValidator : AbstractValidator<RemoveRoleFromUserCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RemoveRoleFromUserCommandValidator"/> class.
    /// </summary>
    public RemoveRoleFromUserCommandValidator()
    {
        RuleFor(command => command.UserId)
            .NotEmpty()
            .WithErrorCode("UserIdRequired")
            .WithMessage("A user identifier is required.");

        RuleFor(command => command.RoleId)
            .NotEmpty()
            .WithErrorCode("RoleIdRequired")
            .WithMessage("A role identifier is required.");
    }
}
