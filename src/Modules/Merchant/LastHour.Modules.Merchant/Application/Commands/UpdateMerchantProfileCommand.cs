using LastHour.BuildingBlocks.Application.Cqrs;

namespace LastHour.Modules.Merchant.Application.Commands;

/// <summary>
/// Updates the profile information of an existing merchant.
/// </summary>
public sealed record UpdateMerchantProfileCommand : ICommand
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateMerchantProfileCommand"/> class.
    /// </summary>
    /// <param name="merchantId">The merchant identifier.</param>
    /// <param name="callerUserId">The authenticated user identifier performing the update.</param>
    /// <param name="name">The updated merchant display name.</param>
    /// <param name="street">The updated street address.</param>
    /// <param name="city">The updated city name.</param>
    /// <param name="stateOrProvince">The updated optional state or province.</param>
    /// <param name="postalCode">The updated optional postal code.</param>
    /// <param name="country">The updated country name.</param>
    /// <param name="phone">The updated contact phone number.</param>
    public UpdateMerchantProfileCommand(
        Guid merchantId,
        Guid callerUserId,
        string name,
        string street,
        string city,
        string? stateOrProvince,
        string? postalCode,
        string country,
        string phone)
    {
        MerchantId = merchantId;
        CallerUserId = callerUserId;
        Name = name;
        Street = street;
        City = city;
        StateOrProvince = stateOrProvince;
        PostalCode = postalCode;
        Country = country;
        Phone = phone;
    }

    /// <summary>
    /// Gets the merchant identifier.
    /// </summary>
    public Guid MerchantId { get; }

    /// <summary>
    /// Gets the authenticated user identifier performing the update.
    /// </summary>
    public Guid CallerUserId { get; }

    /// <summary>
    /// Gets the updated merchant display name.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the updated street address.
    /// </summary>
    public string Street { get; }

    /// <summary>
    /// Gets the updated city name.
    /// </summary>
    public string City { get; }

    /// <summary>
    /// Gets the updated optional state or province.
    /// </summary>
    public string? StateOrProvince { get; }

    /// <summary>
    /// Gets the updated optional postal code.
    /// </summary>
    public string? PostalCode { get; }

    /// <summary>
    /// Gets the updated country name.
    /// </summary>
    public string Country { get; }

    /// <summary>
    /// Gets the updated contact phone number.
    /// </summary>
    public string Phone { get; }
}
