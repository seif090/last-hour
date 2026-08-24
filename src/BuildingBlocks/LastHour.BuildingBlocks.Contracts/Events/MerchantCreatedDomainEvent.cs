using LastHour.BuildingBlocks.SharedKernel.Domain.Events;

namespace LastHour.BuildingBlocks.Contracts.Events;

/// <summary>
/// Domain event raised when a new merchant is registered on the platform.
/// </summary>
public sealed class MerchantCreatedDomainEvent : BaseDomainEvent
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MerchantCreatedDomainEvent"/> class.
    /// </summary>
    /// <param name="merchantId">The unique identifier of the registered merchant.</param>
    /// <param name="name">The name of the merchant.</param>
    /// <param name="address">The formatted address of the merchant.</param>
    /// <param name="phone">The primary phone number of the merchant.</param>
    public MerchantCreatedDomainEvent(
        Guid merchantId,
        string name,
        string address,
        string phone)
    {
        MerchantId = merchantId;
        Name = name;
        Address = address;
        Phone = phone;
    }

    /// <summary>
    /// Gets the unique identifier of the registered merchant.
    /// </summary>
    public Guid MerchantId { get; }

    /// <summary>
    /// Gets the name of the merchant.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the formatted address of the merchant.
    /// </summary>
    public string Address { get; }

    /// <summary>
    /// Gets the primary phone number of the merchant.
    /// </summary>
    public string Phone { get; }
}
