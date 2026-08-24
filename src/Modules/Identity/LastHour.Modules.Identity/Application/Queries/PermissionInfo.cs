namespace LastHour.Modules.Identity.Application.Queries;

/// <summary>
/// The safe projection of a permission returned to callers: the stable identifier, the
/// capability code and the description. No internal state is exposed.
/// </summary>
public sealed record PermissionInfo
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PermissionInfo"/> class.
    /// </summary>
    /// <param name="permissionId">The permission identifier.</param>
    /// <param name="code">The stable capability code (Resource.Action).</param>
    /// <param name="description">The optional human-readable purpose.</param>
    public PermissionInfo(Guid permissionId, string code, string? description)
    {
        PermissionId = permissionId;
        Code = code;
        Description = description;
    }

    /// <summary>
    /// Gets the permission identifier.
    /// </summary>
    public Guid PermissionId { get; }

    /// <summary>
    /// Gets the stable capability code (Resource.Action).
    /// </summary>
    public string Code { get; }

    /// <summary>
    /// Gets the optional human-readable purpose.
    /// </summary>
    public string? Description { get; }
}
