using LastHour.Domain.Common;
using LastHour.Domain.Exceptions;

namespace LastHour.Domain.Entities;

public class CartItem : BaseEntity
{
    public string UserId { get; set; } = string.Empty;
    public string OfferId { get; set; } = string.Empty;
    public int Quantity { get; set; } = 1;

    public decimal TotalPrice => Offer?.DiscountPrice * Quantity ?? 0;
    public Offer? Offer { get; set; }
}

public class Review : BaseEntity
{
    public string UserId { get; set; } = string.Empty;
    public string? StoreId { get; set; }
    public string? OfferId { get; set; }
    public int Rating { get; set; }
    public string? Comment { get; set; }
}

public class Category : BaseAuditableEntity
{
    public string Name { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public string? Icon { get; set; }
    public string? Color { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;
}

public class Coupon : BaseAuditableEntity
{
    public string Code { get; set; } = string.Empty;
    public decimal DiscountPercentage { get; set; }
    public decimal? MaxDiscountAmount { get; set; }
    public decimal? MinOrderAmount { get; set; }
    public int? UsageLimit { get; set; }
    public int UsedCount { get; set; }
    public DateTime? ValidFrom { get; set; }
    public DateTime? ValidTo { get; set; }
    public bool IsActive { get; set; } = true;

    public bool IsValid()
    {
        if (!IsActive) return false;
        if (UsageLimit.HasValue && UsedCount >= UsageLimit.Value) return false;
        var now = DateTime.UtcNow;
        if (ValidFrom.HasValue && now < ValidFrom.Value) return false;
        if (ValidTo.HasValue && now > ValidTo.Value) return false;
        return true;
    }
}

public class Notification : BaseEntity
{
    public string UserId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Body { get; set; }
    public string? Type { get; set; }
    public string? Data { get; set; }
    public bool IsRead { get; set; }
    public DateTime? ReadAt { get; set; }
}

public class PaymentTransaction : BaseEntity
{
    public string OrderId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "USD";
    public string PaymentMethod { get; set; } = string.Empty;
    public string Status { get; set; } = "Pending";
    public string? TransactionId { get; set; }
    public string? GatewayResponse { get; set; }
}
