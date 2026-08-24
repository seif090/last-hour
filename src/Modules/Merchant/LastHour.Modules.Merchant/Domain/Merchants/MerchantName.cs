using System.Text.Json.Serialization;
using LastHour.BuildingBlocks.SharedKernel.Domain;
using LastHour.Modules.Merchant.Domain.Merchants.Exceptions;

namespace LastHour.Modules.Merchant.Domain.Merchants;

/// <summary>
/// Represents the name of a merchant as an immutable value object.
/// </summary>
public sealed class MerchantName : ValueObject
{
    /// <summary>
    /// The maximum length of a merchant name.
    /// </summary>
    public const int MaxLength = 200;

    private readonly string _value;

    [JsonConstructor]
    private MerchantName(string value)
    {
        _value = value;
    }

    /// <summary>
    /// Gets the merchant name value.
    /// </summary>
    public string Value => _value;

    /// <summary>
    /// Creates a <see cref="MerchantName"/> from a raw value, validating domain invariants.
    /// </summary>
    /// <param name="value">The raw merchant name.</param>
    /// <returns>A new <see cref="MerchantName"/> instance.</returns>
    /// <exception cref="MerchantNameInvalidException">The name is null, empty, whitespace, or exceeds <see cref="MaxLength"/> characters.</exception>
    public static MerchantName From(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new MerchantNameInvalidException("A merchant name is required.");
        }

        string trimmed = value.Trim();

        if (trimmed.Length > MaxLength)
        {
            throw new MerchantNameInvalidException($"A merchant name cannot exceed {MaxLength} characters.");
        }

        return new MerchantName(trimmed);
    }

    /// <inheritdoc/>
    public override string ToString() => _value;

    /// <inheritdoc/>
    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return _value;
    }
}
