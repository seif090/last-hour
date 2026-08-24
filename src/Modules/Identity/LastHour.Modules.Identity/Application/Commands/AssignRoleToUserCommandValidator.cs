using FluentValidation;

namespace LastHour.Modules.Identity.Application.Commands;

/// <summary>
/// Validates the structural shape of a role assignment: both identifiers must be present.
/// </summary>
public sealed class AssignRoleToUserCommandValidator : AbstractValidator<AssignRoleToUserCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AssignRoleToUserCommandValidator"/> class.
    /// </summary>
    public AssignRoleToUserCommandValidator()
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
