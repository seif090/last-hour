namespace LastHour.Modules.Identity.Application.Queries;

/// <summary>
/// The safe projection of a role returned to callers. It carries the stable identifier and the
/// display name only — never internal persistence state or the association rows.
/// </summary>
public sealed record RoleInfo
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RoleInfo"/> class.
    /// </summary>
    /// <param name="roleId">The role identifier.</param>
    /// <param name="name">The role display name.</param>
    public RoleInfo(Guid roleId, string name)
    {
        RoleId = roleId;
        Name = name;
    }

    /// <summary>
    /// Gets the role identifier.
    /// </summary>
    public Guid RoleId { get; }

    /// <summary>
    /// Gets the role display name.
    /// </summary>
    public string Name { get; }
}
