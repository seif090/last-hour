namespace LastHour.BuildingBlocks.SharedKernel.Domain.StronglyTypedIds;

/// <summary>
/// A strongly typed identifier wrapping a <see cref="Guid"/>. Suitable as the default
/// primary key type for domain entities or as a base for more specific identifier types.
/// </summary>
public sealed class GuidId : StronglyTypedId<Guid, GuidId>, IStronglyTypedId<Guid, GuidId>
{
    private GuidId(Guid value)
        : base(value)
    {
    }

    /// <summary>
    /// Creates an identifier that wraps <paramref name="value"/>.
    /// </summary>
    /// <param name="value">The <see cref="Guid"/> value to wrap.</param>
    /// <returns>A new <see cref="GuidId"/>.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="value"/> is <see cref="Guid.Empty"/>.</exception>
    public static GuidId Create(Guid value) => new GuidId(value);

    /// <summary>
    /// Creates an identifier wrapping a newly generated <see cref="Guid"/>.
    /// </summary>
    /// <returns>A new <see cref="GuidId"/>.</returns>
    public static GuidId New() => Create(Guid.NewGuid());
}
