namespace LastHour.BuildingBlocks.SharedKernel.Domain.StronglyTypedIds;

/// <summary>
/// A strongly typed identifier wrapping an <see cref="int"/>. Suitable for identifiers
/// whose underlying value is a database-generated integer.
/// </summary>
public sealed class IntId : StronglyTypedId<int, IntId>, IStronglyTypedId<int, IntId>
{
    private IntId(int value)
        : base(value)
    {
    }

    /// <summary>
    /// Creates an identifier that wraps <paramref name="value"/>.
    /// </summary>
    /// <param name="value">The <see cref="int"/> value to wrap.</param>
    /// <returns>A new <see cref="IntId"/>.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="value"/> is zero.</exception>
    public static IntId Create(int value) => new IntId(value);
}
