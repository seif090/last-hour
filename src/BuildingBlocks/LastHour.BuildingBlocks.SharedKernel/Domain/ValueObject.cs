namespace LastHour.BuildingBlocks.SharedKernel.Domain;

/// <summary>
/// Provides a common base class for value objects whose equality is based on the values of
/// their components rather than on reference identity.
/// </summary>
public abstract class ValueObject : IEquatable<ValueObject>
{
    /// <summary>
    /// Determines whether two value objects are equal.
    /// </summary>
    /// <param name="left">The first value object to compare.</param>
    /// <param name="right">The second value object to compare.</param>
    /// <returns><see langword="true"/> when both value objects are equal; otherwise, <see langword="false"/>.</returns>
    public static bool operator ==(ValueObject? left, ValueObject? right) => Equals(left, right);

    /// <summary>
    /// Determines whether two value objects are not equal.
    /// </summary>
    /// <param name="left">The first value object to compare.</param>
    /// <param name="right">The second value object to compare.</param>
    /// <returns><see langword="true"/> when both value objects are not equal; otherwise, <see langword="false"/>.</returns>
    public static bool operator !=(ValueObject? left, ValueObject? right) => !Equals(left, right);

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is ValueObject other && Equals(other);

    /// <inheritdoc/>
    public bool Equals(ValueObject? other)
        => other is not null
           && other.GetType() == GetType()
           && ValuesEqual(other);

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        HashCode hash = default(HashCode);
        hash.Add(GetType());
        foreach (object component in GetEqualityComponents())
        {
            hash.Add(component);
        }

        return hash.ToHashCode();
    }

    /// <summary>
    /// Gets the atomic values that participate in the equality comparison.
    /// </summary>
    /// <returns>An enumerable of the component values used to define equality.</returns>
    protected abstract IEnumerable<object> GetEqualityComponents();

    private bool ValuesEqual(ValueObject other)
    {
        using IEnumerator<object> leftValues = GetEqualityComponents().GetEnumerator();
        using IEnumerator<object> rightValues = other.GetEqualityComponents().GetEnumerator();

        while (leftValues.MoveNext() && rightValues.MoveNext())
        {
            if (leftValues.Current is null ^ rightValues.Current is null)
            {
                return false;
            }

            if (leftValues.Current is not null && !leftValues.Current.Equals(rightValues.Current))
            {
                return false;
            }
        }

        return !leftValues.MoveNext() && !rightValues.MoveNext();
    }
}
