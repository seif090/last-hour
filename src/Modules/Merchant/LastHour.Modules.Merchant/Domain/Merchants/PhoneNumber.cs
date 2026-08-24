using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using LastHour.BuildingBlocks.SharedKernel.Domain;
using LastHour.Modules.Merchant.Domain.Merchants.Exceptions;

namespace LastHour.Modules.Merchant.Domain.Merchants;

/// <summary>
/// Represents a phone number as an immutable value object.
/// </summary>
public sealed class PhoneNumber : ValueObject
{
    /// <summary>
    /// The maximum length of a phone number.
    /// </summary>
    public const int MaxLength = 30;

    /// <summary>
    /// The minimum length of a phone number.
    /// </summary>
    public const int MinLength = 3;

    private static readonly Regex PhonePattern = new Regex(
        @"^\+?[0-9\s\-\(\)\.]{3,30}$",
        RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private readonly string _value;

    [JsonConstructor]
    private PhoneNumber(string value)
    {
        _value = value;
    }

    /// <summary>
    /// Gets the normalized phone number value.
    /// </summary>
    public string Value => _value;

    /// <summary>
    /// Creates a <see cref="PhoneNumber"/> from a raw string value.
    /// </summary>
    /// <param name="value">The raw phone number string.</param>
    /// <returns>A new <see cref="PhoneNumber"/> instance.</returns>
    /// <exception cref="InvalidPhoneNumberException">Thrown when the value is null, empty, whitespace, or contains invalid characters.</exception>
    public static PhoneNumber From(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidPhoneNumberException("A phone number is required.");
        }

        string trimmed = value.Trim();

        if (trimmed.Length < MinLength || trimmed.Length > MaxLength)
        {
            throw new InvalidPhoneNumberException($"A phone number must be between {MinLength} and {MaxLength} characters.");
        }

        if (!PhonePattern.IsMatch(trimmed) || !trimmed.Any(char.IsDigit))
        {
            throw new InvalidPhoneNumberException("The phone number format is invalid.");
        }

        return new PhoneNumber(trimmed);
    }

    /// <inheritdoc/>
    public override string ToString() => _value;

    /// <inheritdoc/>
    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return _value;
    }
}
