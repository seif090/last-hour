using LastHour.BuildingBlocks.SharedKernel.Domain.StronglyTypedIds;

using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace LastHour.BuildingBlocks.Infrastructure.StronglyTypedIds;

/// <summary>
/// Creates <see cref="ValueConverter{TSelf, TValue}"/> instances that persist a strongly
/// typed identifier as its primitive value. Wiring for a specific id type is one call,
/// for example: <c>StronglyTypedIdValueConverter.Create&lt;Guid, GuidId&gt;()</c>.
/// </summary>
public static class StronglyTypedIdValueConverter
{
    /// <summary>
    /// Creates a converter between a strongly typed identifier and its primitive value,
    /// using the identifier's own factory to reconstruct instances.
    /// </summary>
    /// <typeparam name="TValue">The wrapped primitive value type.</typeparam>
    /// <typeparam name="TSelf">The concrete strongly typed identifier type.</typeparam>
    /// <returns>A <see cref="ValueConverter{TSelf, TValue}"/> for use in an EF Core model configuration.</returns>
    public static ValueConverter<TSelf, TValue> Create<TValue, TSelf>()
        where TValue : struct, IEquatable<TValue>, IParsable<TValue>
        where TSelf : StronglyTypedId<TValue, TSelf>, IStronglyTypedId<TValue, TSelf>
        => Create<TValue, TSelf>(TSelf.Create);

    /// <summary>
    /// Creates a converter between a strongly typed identifier and its primitive value.
    /// </summary>
    /// <typeparam name="TValue">The wrapped primitive value type.</typeparam>
    /// <typeparam name="TSelf">The concrete strongly typed identifier type.</typeparam>
    /// <param name="factory">The factory used to reconstruct an identifier from its stored value.</param>
    /// <returns>A <see cref="ValueConverter{TSelf, TValue}"/> for use in an EF Core model configuration.</returns>
    public static ValueConverter<TSelf, TValue> Create<TValue, TSelf>(Func<TValue, TSelf> factory)
        where TValue : struct, IEquatable<TValue>, IParsable<TValue>
        where TSelf : StronglyTypedId<TValue, TSelf>, IStronglyTypedId<TValue, TSelf>
        => new ValueConverter<TSelf, TValue>(
            id => id.Value,
            value => factory(value));
}
