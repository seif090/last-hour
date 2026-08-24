namespace LastHour.Modules.Merchant.Application.Models;

/// <summary>
/// The safe projection of a merchant returned to read callers. Value objects are flattened
/// into their primitive values and status is formatted as a string.
/// </summary>
public sealed record MerchantDto
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MerchantDto"/> class.
    /// </summary>
    /// <param name="id">The merchant identifier.</param>
    /// <param name="ownerId">The owner identifier.</param>
    /// <param name="name">The merchant display name.</param>
    /// <param name="street">The street address.</param>
    /// <param name="city">The city name.</param>
    /// <param name="stateOrProvince">The optional state or province.</param>
    /// <param name="postalCode">The optional postal code.</param>
    /// <param name="country">The country name.</param>
    /// <param name="phone">The phone number.</param>
    /// <param name="status">The account lifecycle status.</param>
    public MerchantDto(
        Guid id,
        Guid ownerId,
        string name,
        string street,
        string city,
        string? stateOrProvince,
        string? postalCode,
        string country,
        string phone,
        string status)
    {
        Id = id;
        OwnerId = ownerId;
        Name = name;
        Street = street;
        City = city;
        StateOrProvince = stateOrProvince;
        PostalCode = postalCode;
        Country = country;
        Phone = phone;
        Status = status;
    }

    /// <summary>
    /// Gets the merchant identifier.
    /// </summary>
    public Guid Id { get; }

    /// <summary>
    /// Gets the owner identifier.
    /// </summary>
    public Guid OwnerId { get; }

    /// <summary>
    /// Gets the merchant display name.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the street address.
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
    /// Gets the country name.
    /// </summary>
    public string Country { get; }

    /// <summary>
    /// Gets the phone number.
    /// </summary>
    public string Phone { get; }

    /// <summary>
    /// Gets the account lifecycle status.
    /// </summary>
    public string Status { get; }
}
