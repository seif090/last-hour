using LastHour.BuildingBlocks.Application.Cqrs;

namespace LastHour.Modules.Identity.Application.Commands;

/// <summary>
/// Removes a role assignment from a user. A role the user does not hold cannot be removed.
/// Privilege-sensitive — must only ever be dispatched by a protected management flow, never by
/// public input.
/// </summary>
public sealed record RemoveRoleFromUserCommand : ICommand
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RemoveRoleFromUserCommand"/> class.
    /// </summary>
    /// <param name="userId">The user losing the role.</param>
    /// <param name="roleId">The role being removed.</param>
    public RemoveRoleFromUserCommand(Guid userId, Guid roleId)
    {
        UserId = userId;
        RoleId = roleId;
    }

    /// <summary>
    /// Gets the user losing the role.
    /// </summary>
    public Guid UserId { get; }

    /// <summary>
    /// Gets the role being removed.
    /// </summary>
    public Guid RoleId { get; }
}
