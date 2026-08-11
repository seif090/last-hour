using LastHour.BuildingBlocks.SharedKernel.Domain.StronglyTypedIds;

using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace LastHour.BuildingBlocks.Infrastructure.StronglyTypedIds;

/// <summary>
/// Provides a cached <see cref="ValueConverter{TSelf, TValue}"/> between a strongly typed
/// identifier and its primitive value. The EF Core model convention in
/// <see cref="Persistence.LastHourDbContext"/> discovers these converters automatically for
/// every property whose CLR type implements <see cref="IStronglyTypedId{TValue, TSelf}"/>,
/// so module entities never need to configure value conversions manually.
/// </summary>
/// <typeparam name="TValue">The wrapped primitive value type.</typeparam>
/// <typeparam name="TSelf">The concrete strongly typed identifier type.</typeparam>
public static class StronglyTypedIdValueConverter<TValue, TSelf>
    where TValue : struct, IEquatable<TValue>, IParsable<TValue>
    where TSelf : StronglyTypedId<TValue, TSelf>, IStronglyTypedId<TValue, TSelf>
{
    /// <summary>
    /// Gets the shared converter instance between the identifier and its primitive value.
    /// </summary>
    public static readonly ValueConverter<TSelf, TValue> Instance;

    static StronglyTypedIdValueConverter()
    {
        Func<TValue, TSelf> factory = TSelf.Create;
        Instance = new ValueConverter<TSelf, TValue>(
            id => id.Value,
            value => factory(value));
    }
}
