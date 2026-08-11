namespace LastHour.BuildingBlocks.SharedKernel.Domain.StronglyTypedIds;

/// <summary>
/// Defines the contract implemented by every strongly typed identifier, exposing the
/// wrapped primitive <typeparamref name="TValue"/> and a static factory that a derived
/// type uses to construct itself without exposing a public constructor.
/// </summary>
/// <typeparam name="TValue">The wrapped primitive value type.</typeparam>
/// <typeparam name="TSelf">The concrete strongly typed identifier type.</typeparam>
public interface IStronglyTypedId<TValue, TSelf>
    where TValue : struct, IEquatable<TValue>, IParsable<TValue>
    where TSelf : IStronglyTypedId<TValue, TSelf>
{
    /// <summary>
    /// Gets the wrapped primitive value.
    /// </summary>
    TValue Value { get; }

    /// <summary>
    /// Creates an identifier instance that wraps <paramref name="value"/>.
    /// </summary>
    /// <param name="value">The primitive value to wrap.</param>
    /// <returns>A new identifier instance.</returns>
    static abstract TSelf Create(TValue value);
}
