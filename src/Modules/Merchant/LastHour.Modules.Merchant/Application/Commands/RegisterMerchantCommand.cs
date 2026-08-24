using LastHour.BuildingBlocks.Application.Cqrs;
using LastHour.Modules.Merchant.Domain.Merchants;

namespace LastHour.Modules.Merchant.Application.Commands;

/// <summary>
/// Registers a new merchant.
/// </summary>
public sealed record RegisterMerchantCommand : ICommand<MerchantId>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RegisterMerchantCommand"/> class.
    /// </summary>
    /// <param name="ownerId">The unique owner identifier.</param>
    /// <param name="name">The merchant display name.</param>
    /// <param name="street">The street address.</param>
    /// <param name="city">The city name.</param>
    /// <param name="stateOrProvince">The optional state or province.</param>
    /// <param name="postalCode">The optional postal code.</param>
    /// <param name="country">The country name.</param>
    /// <param name="phone">The contact phone number.</param>
    public RegisterMerchantCommand(
        Guid ownerId,
        string name,
        string street,
        string city,
        string? stateOrProvince,
        string? postalCode,
        string country,
        string phone)
    {
        OwnerId = ownerId;
        Name = name;
        Street = street;
        City = city;
        StateOrProvince = stateOrProvince;
        PostalCode = postalCode;
        Country = country;
        Phone = phone;
    }

    /// <summary>
    /// Gets the unique owner identifier.
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
    /// Gets the contact phone number.
    /// </summary>
    public string Phone { get; }
}
