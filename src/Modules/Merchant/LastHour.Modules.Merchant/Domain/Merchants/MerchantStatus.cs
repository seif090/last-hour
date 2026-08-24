namespace LastHour.Modules.Merchant.Domain.Merchants;

/// <summary>
/// The lifecycle status of a merchant account.
/// </summary>
public enum MerchantStatus
{
    /// <summary>
    /// The merchant was created but has not been activated.
    /// </summary>
    Pending = 0,

    /// <summary>
    /// The merchant is active and can perform operations.
    /// </summary>
    Active = 1,

    /// <summary>
    /// The merchant is temporarily suspended.
    /// </summary>
    Suspended = 2,

    /// <summary>
    /// The merchant is permanently closed.
    /// </summary>
    Closed = 3,
}
