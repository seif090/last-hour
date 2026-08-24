using System.Text.RegularExpressions;
using LastHour.BuildingBlocks.SharedKernel.Domain;

namespace LastHour.Modules.Catalog.Domain.Products;

/// <summary>
/// Represents the Stock Keeping Unit (SKU) of a <see cref="Product"/> as an immutable value object.
/// Validation and normalization live here so that valid SKU invariants are guaranteed throughout the domain.
/// </summary>
public sealed class Sku : ValueObject
{
    /// <summary>
    /// The maximum length of a SKU code.
    /// </summary>
    public const int MaxLength = 50;

    private static readonly Regex SkuPattern = new Regex(
        @"^[A-Za-z0-9\-_]+$",
        RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private readonly string _value;
    private readonly string _normalizedValue;

    private Sku(string value, string normalizedValue)
    {
        _value = value;
        _normalizedValue = normalizedValue;
    }

    /// <summary>
    /// Gets the original SKU as supplied (whitespace-trimmed).
    /// </summary>
    public string Value => _value;

    /// <summary>
    /// Gets the normalized representation (whitespace-trimmed and upper-cased with the invariant culture).
    /// </summary>
    public string NormalizedValue => _normalizedValue;

    /// <summary>
    /// Creates a <see cref="Sku"/> from a raw value, validating domain invariants.
    /// </summary>
    /// <param name="value">The raw SKU code.</param>
    /// <returns>A new <see cref="Sku"/> instance.</returns>
    /// <exception cref="InvalidSkuException">The SKU is null, empty, exceeds max length, or contains invalid characters.</exception>
    public static Sku From(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidSkuException("A SKU is required.");
        }

        string trimmed = value.Trim();

        if (trimmed.Length > MaxLength)
        {
            throw new InvalidSkuException($"A SKU cannot exceed {MaxLength} characters.");
        }

        if (!SkuPattern.IsMatch(trimmed))
        {
            throw new InvalidSkuException("A SKU may only contain letters, numbers, hyphens, and underscores.");
        }

        return new Sku(trimmed, Normalize(trimmed));
    }

    /// <summary>
    /// Normalizes a SKU value for uniform comparisons and indexing.
    /// </summary>
    /// <param name="value">The SKU value to normalize.</param>
    /// <returns>The normalized SKU string.</returns>
    public static string Normalize(string value) => value.Trim().ToUpperInvariant();

    /// <inheritdoc/>
    public override string ToString() => _value;

    /// <inheritdoc/>
    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return _normalizedValue;
    }
}
