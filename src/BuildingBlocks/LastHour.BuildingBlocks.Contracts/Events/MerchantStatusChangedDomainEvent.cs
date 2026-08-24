using LastHour.BuildingBlocks.SharedKernel.Domain.Events;

namespace LastHour.BuildingBlocks.Contracts.Events;

/// <summary>
/// Domain event raised when a merchant's lifecycle status changes.
/// </summary>
public sealed class MerchantStatusChangedDomainEvent : BaseDomainEvent
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MerchantStatusChangedDomainEvent"/> class.
    /// </summary>
    /// <param name="merchantId">The unique identifier of the merchant.</param>
    /// <param name="previousStatus">The previous status string.</param>
    /// <param name="currentStatus">The new current status string.</param>
    public MerchantStatusChangedDomainEvent(
        Guid merchantId,
        string previousStatus,
        string currentStatus)
    {
        MerchantId = merchantId;
        PreviousStatus = previousStatus;
        CurrentStatus = currentStatus;
    }

    /// <summary>
    /// Gets the unique identifier of the merchant.
    /// </summary>
    public Guid MerchantId { get; }

    /// <summary>
    /// Gets the previous status of the merchant.
    /// </summary>
    public string PreviousStatus { get; }

    /// <summary>
    /// Gets the current status of the merchant.
    /// </summary>
    public string CurrentStatus { get; }
}
