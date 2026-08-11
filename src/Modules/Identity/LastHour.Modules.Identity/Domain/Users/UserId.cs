using LastHour.BuildingBlocks.SharedKernel.Domain.StronglyTypedIds;

namespace LastHour.Modules.Identity.Domain.Users;

/// <summary>
/// The stable, immutable identifier of a <see cref="User"/>. Wraps a <see cref="Guid"/> and
/// is persisted as a PostgreSQL <c>uuid</c> column through the shared strongly typed id
/// value converters. A dedicated type (rather than the generic <see cref="GuidId"/>) prevents
/// confusing a user id with any other entity id at compile time.
/// </summary>
public sealed class UserId : StronglyTypedId<Guid, UserId>, IStronglyTypedId<Guid, UserId>
{
    private UserId(Guid value)
        : base(value)
    {
    }

    /// <summary>
    /// Creates a user id that wraps <paramref name="value"/>.
    /// </summary>
    /// <param name="value">The <see cref="Guid"/> value to wrap.</param>
    /// <returns>A new <see cref="UserId"/>.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="value"/> is <see cref="Guid.Empty"/>.</exception>
    public static UserId Create(Guid value) => new UserId(value);

    /// <summary>
    /// Creates a user id wrapping a newly generated <see cref="Guid"/>.
    /// </summary>
    /// <returns>A new <see cref="UserId"/>.</returns>
    public static UserId New() => Create(Guid.NewGuid());
}
