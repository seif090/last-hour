using LastHour.BuildingBlocks.SharedKernel.Domain.StronglyTypedIds;

namespace LastHour.Modules.Identity.Domain.Roles;

/// <summary>
/// The stable, immutable identifier of a <see cref="Role"/>. Wraps a <see cref="Guid"/> and is
/// persisted as a PostgreSQL <c>uuid</c> column through the shared strongly typed id value
/// converters. A dedicated type (rather than the generic <see cref="GuidId"/>) prevents
/// confusing a role id with any other entity id at compile time.
/// </summary>
public sealed class RoleId : StronglyTypedId<Guid, RoleId>, IStronglyTypedId<Guid, RoleId>
{
    private RoleId(Guid value)
        : base(value)
    {
    }

    /// <summary>
    /// Creates a role id that wraps <paramref name="value"/>.
    /// </summary>
    /// <param name="value">The <see cref="Guid"/> value to wrap.</param>
    /// <returns>A new <see cref="RoleId"/>.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="value"/> is <see cref="Guid.Empty"/>.</exception>
    public static RoleId Create(Guid value) => new RoleId(value);

    /// <summary>
    /// Creates a role id wrapping a newly generated <see cref="Guid"/>.
    /// </summary>
    /// <returns>A new <see cref="RoleId"/>.</returns>
    public static RoleId New() => Create(Guid.NewGuid());
}
