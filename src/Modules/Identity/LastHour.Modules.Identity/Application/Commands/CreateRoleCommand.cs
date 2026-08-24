using LastHour.BuildingBlocks.Application.Cqrs;
using LastHour.Modules.Identity.Domain.Roles;

namespace LastHour.Modules.Identity.Application.Commands;

/// <summary>
/// Creates a new role with a validated, normalized name. The name is normalized with the
/// identity module's single rule (<see cref="RoleName.Normalize"/>), and the database unique
/// index on the normalized name makes duplicate roles impossible even under concurrency.
/// Privilege-sensitive — must only ever be dispatched by a protected management flow, never
/// by public input.
/// </summary>
public sealed record CreateRoleCommand : ICommand<RoleId>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CreateRoleCommand"/> class.
    /// </summary>
    /// <param name="name">The role name, normalized by the domain's single rule.</param>
    /// <param name="description">An optional human-readable purpose.</param>
    public CreateRoleCommand(string name, string? description)
    {
        Name = name;
        Description = description;
    }

    /// <summary>
    /// Gets the role name.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the optional human-readable purpose.
    /// </summary>
    public string? Description { get; }
}
