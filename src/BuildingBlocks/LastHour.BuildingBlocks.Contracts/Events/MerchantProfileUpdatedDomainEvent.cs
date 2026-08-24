using LastHour.BuildingBlocks.SharedKernel.Domain.Events;

namespace LastHour.BuildingBlocks.Contracts.Events;

/// <summary>
/// Domain event raised when a merchant's profile details are updated.
/// </summary>
public sealed class MerchantProfileUpdatedDomainEvent : BaseDomainEvent
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MerchantProfileUpdatedDomainEvent"/> class.
    /// </summary>
    /// <param name="merchantId">The unique identifier of the merchant.</param>
    /// <param name="name">The updated merchant name.</param>
    public MerchantProfileUpdatedDomainEvent(
        Guid merchantId,
        string name)
    {
        MerchantId = merchantId;
        Name = name;
    }

    /// <summary>
    /// Gets the unique identifier of the merchant.
    /// </summary>
    public Guid MerchantId { get; }

    /// <summary>
    /// Gets the updated merchant name.
    /// </summary>
    public string Name { get; }
}
