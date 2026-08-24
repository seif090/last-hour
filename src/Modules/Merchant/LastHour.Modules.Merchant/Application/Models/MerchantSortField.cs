namespace LastHour.Modules.Merchant.Application.Models;

/// <summary>
/// Identifies the merchant attribute used to order search results.
/// </summary>
public enum MerchantSortField
{
    /// <summary>
    /// Orders by the merchant name.
    /// </summary>
    Name,

    /// <summary>
    /// Orders by the merchant status.
    /// </summary>
    Status,

    /// <summary>
    /// Orders by creation timestamp.
    /// </summary>
    CreatedAt,
}
