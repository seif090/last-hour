using FluentValidation;
using LastHour.Modules.Identity.Domain.Roles;

namespace LastHour.Modules.Identity.Application.Commands;

/// <summary>
/// Validates the structural shape of a role creation: a required name within the domain bound
/// and an optional description within its bound. The name format itself is the domain's
/// invariant (<see cref="RoleName.From"/>); this validator rejects obviously malformed
/// payloads before the handler runs.
/// </summary>
public sealed class CreateRoleCommandValidator : AbstractValidator<CreateRoleCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CreateRoleCommandValidator"/> class.
    /// </summary>
    public CreateRoleCommandValidator()
    {
        RuleFor(command => command.Name)
            .NotEmpty()
            .WithErrorCode("RoleNameRequired")
            .WithMessage("A role name is required.")
            .MaximumLength(RoleName.MaxLength)
            .WithErrorCode("RoleNameTooLong")
            .WithMessage($"A role name cannot exceed {RoleName.MaxLength} characters.");

        RuleFor(command => command.Description)
            .MaximumLength(Role.MaxDescriptionLength)
            .WithErrorCode("RoleDescriptionTooLong")
            .WithMessage($"A role description cannot exceed {Role.MaxDescriptionLength} characters.");
    }
}
