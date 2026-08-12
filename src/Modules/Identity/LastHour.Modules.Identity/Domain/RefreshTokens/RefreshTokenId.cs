using LastHour.BuildingBlocks.SharedKernel.Domain.StronglyTypedIds;

namespace LastHour.Modules.Identity.Domain.RefreshTokens;

/// <summary>
/// The stable, immutable identifier of a <see cref="RefreshToken"/> row. Wraps a
/// <see cref="Guid"/> and is persisted as a PostgreSQL <c>uuid</c> column through the shared
/// strongly typed id value converters. A dedicated type (rather than the generic
/// <see cref="GuidId"/>) prevents confusing a refresh-token id with any other entity id at
/// compile time.
/// </summary>
public sealed class RefreshTokenId : StronglyTypedId<Guid, RefreshTokenId>, IStronglyTypedId<Guid, RefreshTokenId>
{
    private RefreshTokenId(Guid value)
        : base(value)
    {
    }

    /// <summary>
    /// Creates a refresh-token id that wraps <paramref name="value"/>.
    /// </summary>
    /// <param name="value">The <see cref="Guid"/> value to wrap.</param>
    /// <returns>A new <see cref="RefreshTokenId"/>.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="value"/> is <see cref="Guid.Empty"/>.</exception>
    public static RefreshTokenId Create(Guid value) => new RefreshTokenId(value);

    /// <summary>
    /// Creates a refresh-token id wrapping a newly generated <see cref="Guid"/>.
    /// </summary>
    /// <returns>A new <see cref="RefreshTokenId"/>.</returns>
    public static RefreshTokenId New() => Create(Guid.NewGuid());
}
