using LastHour.BuildingBlocks.SharedKernel.Domain.StronglyTypedIds;

namespace LastHour.Modules.Identity.Domain.Roles;

/// <summary>
/// The stable, immutable identifier of a <see cref="Permission"/>. Wraps a <see cref="Guid"/>
/// and is persisted as a PostgreSQL <c>uuid</c> column through the shared strongly typed id
/// value converters. A dedicated type (rather than the generic <see cref="GuidId"/>) prevents
/// confusing a permission id with any other entity id at compile time.
/// </summary>
public sealed class PermissionId : StronglyTypedId<Guid, PermissionId>, IStronglyTypedId<Guid, PermissionId>
{
    private PermissionId(Guid value)
        : base(value)
    {
    }

    /// <summary>
    /// Creates a permission id that wraps <paramref name="value"/>.
    /// </summary>
    /// <param name="value">The <see cref="Guid"/> value to wrap.</param>
    /// <returns>A new <see cref="PermissionId"/>.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="value"/> is <see cref="Guid.Empty"/>.</exception>
    public static PermissionId Create(Guid value) => new PermissionId(value);

    /// <summary>
    /// Creates a permission id wrapping a newly generated <see cref="Guid"/>.
    /// </summary>
    /// <returns>A new <see cref="PermissionId"/>.</returns>
    public static PermissionId New() => Create(Guid.NewGuid());
}
