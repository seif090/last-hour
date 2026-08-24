using System.Text.Json.Serialization;
using LastHour.BuildingBlocks.SharedKernel.Domain;
using LastHour.Modules.Merchant.Domain.Merchants.Exceptions;

namespace LastHour.Modules.Merchant.Domain.Merchants;

/// <summary>
/// Represents a physical or mailing address as an immutable value object.
/// </summary>
public sealed class Address : ValueObject
{
    [JsonConstructor]
    private Address(
        string street,
        string city,
        string? stateOrProvince,
        string? postalCode,
        string country)
    {
        Street = street;
        City = city;
        StateOrProvince = stateOrProvince;
        PostalCode = postalCode;
        Country = country;
    }

    /// <summary>
    /// Gets the street address line.
    /// </summary>
    public string Street { get; }

    /// <summary>
    /// Gets the city name.
    /// </summary>
    public string City { get; }

    /// <summary>
    /// Gets the optional state or province.
    /// </summary>
    public string? StateOrProvince { get; }

    /// <summary>
    /// Gets the optional postal code.
    /// </summary>
    public string? PostalCode { get; }

    /// <summary>
    /// Gets the country name or code.
    /// </summary>
    public string Country { get; }

    /// <summary>
    /// Creates a new <see cref="Address"/> value object after validating and normalizing components.
    /// </summary>
    /// <param name="street">The street address.</param>
    /// <param name="city">The city name.</param>
    /// <param name="stateOrProvince">The optional state or province.</param>
    /// <param name="postalCode">The optional postal code.</param>
    /// <param name="country">The country name or code.</param>
    /// <returns>A new <see cref="Address"/> instance.</returns>
    /// <exception cref="InvalidAddressException">Thrown when any required address component is missing or invalid.</exception>
    public static Address Create(
        string? street,
        string? city,
        string? stateOrProvince,
        string? postalCode,
        string? country)
    {
        if (string.IsNullOrWhiteSpace(street))
        {
            throw new InvalidAddressException("Street is required.");
        }

        if (string.IsNullOrWhiteSpace(city))
        {
            throw new InvalidAddressException("City is required.");
        }

        if (string.IsNullOrWhiteSpace(country))
        {
            throw new InvalidAddressException("Country is required.");
        }

        string trimmedStreet = street.Trim();
        string trimmedCity = city.Trim();
        string trimmedCountry = country.Trim();
        string? trimmedStateOrProvince = string.IsNullOrWhiteSpace(stateOrProvince) ? null : stateOrProvince.Trim();
        string? trimmedPostalCode = string.IsNullOrWhiteSpace(postalCode) ? null : postalCode.Trim();

        return new Address(
            trimmedStreet,
            trimmedCity,
            trimmedStateOrProvince,
            trimmedPostalCode,
            trimmedCountry);
    }

    /// <inheritdoc/>
    public override string ToString()
    {
        var parts = new List<string> { Street, City };
        if (!string.IsNullOrWhiteSpace(StateOrProvince))
        {
            parts.Add(StateOrProvince);
        }

        if (!string.IsNullOrWhiteSpace(PostalCode))
        {
            parts.Add(PostalCode);
        }

        parts.Add(Country);
        return string.Join(", ", parts);
    }

    /// <inheritdoc/>
    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Street;
        yield return City;
        yield return StateOrProvince ?? string.Empty;
        yield return PostalCode ?? string.Empty;
        yield return Country;
    }
}
