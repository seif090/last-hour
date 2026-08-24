using LastHour.BuildingBlocks.Application.Cqrs;

namespace LastHour.Modules.Identity.Application.Commands;

/// <summary>
/// Assigns an existing role to an existing user. The assignment is a set membership owned by
/// the user aggregate: a duplicate assignment is rejected and the database composite key
/// enforces it under concurrency. Privilege-sensitive — must only ever be dispatched by a
/// protected management flow, never by public input, and neither identifier may come from an
/// unauthenticated request.
/// </summary>
public sealed record AssignRoleToUserCommand : ICommand
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AssignRoleToUserCommand"/> class.
    /// </summary>
    /// <param name="userId">The user receiving the role.</param>
    /// <param name="roleId">The role being assigned.</param>
    public AssignRoleToUserCommand(Guid userId, Guid roleId)
    {
        UserId = userId;
        RoleId = roleId;
    }

    /// <summary>
    /// Gets the user receiving the role.
    /// </summary>
    public Guid UserId { get; }

    /// <summary>
    /// Gets the role being assigned.
    /// </summary>
    public Guid RoleId { get; }
}
