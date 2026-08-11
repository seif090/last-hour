using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace LastHour.BuildingBlocks.SharedKernel.Domain.StronglyTypedIds;

/// <summary>
/// Provides the shared behavior of a strongly typed identifier: an immutable wrapper
/// around a primitive value that prevents type confusion between identifiers of
/// different entities while remaining serializable, comparable and persistence-friendly.
/// Instances are created through static factory and parse methods, never through a
/// public constructor.
/// </summary>
/// <typeparam name="TValue">The wrapped primitive value type.</typeparam>
/// <typeparam name="TSelf">The concrete strongly typed identifier type.</typeparam>
[SuppressMessage(
    "Design",
    "CA1000",
    Justification = "The strongly typed id base intentionally exposes static factory and parse methods.")]
public abstract class StronglyTypedId<TValue, TSelf> : IEquatable<TSelf>
    where TValue : struct, IEquatable<TValue>, IParsable<TValue>
    where TSelf : StronglyTypedId<TValue, TSelf>, IStronglyTypedId<TValue, TSelf>
{
    private readonly TValue _value;

    /// <summary>
    /// Initializes a new instance of the <see cref="StronglyTypedId{TValue, TSelf}"/> class.
    /// </summary>
    /// <param name="value">The primitive value to wrap.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="value"/> is rejected by <see cref="ValidateValue"/>.</exception>
    protected StronglyTypedId(TValue value)
    {
        ValidateValue(value);
        _value = value;
    }

    /// <summary>
    /// Gets the wrapped primitive value.
    /// </summary>
    public TValue Value => _value;

    /// <summary>
    /// Implicitly converts an identifier to its wrapped primitive value.
    /// </summary>
    /// <param name="id">The identifier to convert.</param>
    public static implicit operator TValue(StronglyTypedId<TValue, TSelf> id) => id._value;

    /// <summary>
    /// Compares two identifiers for equality.
    /// </summary>
    /// <param name="left">The left identifier.</param>
    /// <param name="right">The right identifier.</param>
    /// <returns><see langword="true"/> when both identifiers wrap the same value; otherwise <see langword="false"/>.</returns>
    public static bool operator ==(StronglyTypedId<TValue, TSelf>? left, StronglyTypedId<TValue, TSelf>? right)
        => ReferenceEquals(left, right) || (left is not null && left.Equals(right));

    /// <summary>
    /// Compares two identifiers for inequality.
    /// </summary>
    /// <param name="left">The left identifier.</param>
    /// <param name="right">The right identifier.</param>
    /// <returns><see langword="true"/> when the identifiers are not equal; otherwise <see langword="false"/>.</returns>
    public static bool operator !=(StronglyTypedId<TValue, TSelf>? left, StronglyTypedId<TValue, TSelf>? right)
        => !(left == right);

    /// <summary>
    /// Parses a string representation of the identifier.
    /// </summary>
    /// <param name="value">The string to parse.</param>
    /// <returns>The parsed identifier.</returns>
    /// <exception cref="FormatException"><paramref name="value"/> is not a valid identifier.</exception>
    public static TSelf Parse(string value)
    {
        if (!TryParse(value, out TSelf? result))
        {
            throw new FormatException($"The value '{value}' is not a valid {typeof(TSelf).Name}.");
        }

        return result!;
    }

    /// <summary>
    /// Attempts to parse a string representation of the identifier.
    /// </summary>
    /// <param name="value">The string to parse.</param>
    /// <param name="result">The parsed identifier when successful; otherwise <see langword="null"/>.</param>
    /// <returns><see langword="true"/> when <paramref name="value"/> represents a valid identifier; otherwise <see langword="false"/>.</returns>
    public static bool TryParse(string value, out TSelf? result)
    {
        result = default;

        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        if (!TValue.TryParse(value, CultureInfo.InvariantCulture, out TValue parsed))
        {
            return false;
        }

        try
        {
            result = TSelf.Create(parsed);
        }
        catch (ArgumentException)
        {
            return false;
        }
        catch (FormatException)
        {
            return false;
        }

        return true;
    }

    /// <inheritdoc/>
    public bool Equals(TSelf? other) => other is not null && _value.Equals(other._value);

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is TSelf other && _value.Equals(other._value);

    /// <inheritdoc/>
    public override int GetHashCode() => HashCode.Combine(typeof(TSelf), _value);

    /// <inheritdoc/>
    public override string ToString() => _value.ToString() ?? string.Empty;

    /// <summary>
    /// Validates the value before it is stored. The default implementation rejects the
    /// default value of <typeparamref name="TValue"/>; derived types may override this
    /// to apply stricter rules.
    /// </summary>
    /// <param name="value">The value to validate.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="value"/> is the default value.</exception>
    protected virtual void ValidateValue(TValue value)
    {
        if (EqualityComparer<TValue>.Default.Equals(value, default))
        {
            throw new ArgumentOutOfRangeException(
                nameof(value),
                $"The value of a strongly typed id cannot be the default value of '{typeof(TValue).Name}'.");
        }
    }
}
