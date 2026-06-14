using LastHour.Domain.Common;
using LastHour.Domain.Exceptions;

namespace LastHour.Domain.Entities;

public class Offer : BaseAuditableEntity
{
    public string StoreId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal OriginalPrice { get; set; }
    public decimal DiscountPrice { get; set; }
    public int OriginalQuantity { get; set; }
    public int RemainingQuantity { get; set; }
    public DateTime ExpiryTime { get; set; }
    public string Category { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public string? ImageUrls { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsFeatured { get; set; }
    public double Rating { get; set; }
    public int ReviewCount { get; set; }
    public DateTime? PickupStartTime { get; set; }
    public DateTime? PickupEndTime { get; set; }

    public Store Store { get; set; } = null!;
    public ICollection<OrderItem> OrderItems { get; set; } = [];
    public ICollection<Review> Reviews { get; set; } = [];
    public ICollection<Favorite> Favorites { get; set; } = [];
    public ICollection<CartItem> CartItems { get; set; } = [];

    public decimal DiscountPercentage =>
        OriginalPrice > 0
            ? Math.Round((OriginalPrice - DiscountPrice) / OriginalPrice * 100, 1)
            : 0;

    public bool IsExpired => DateTime.UtcNow >= ExpiryTime;
    public bool IsSoldOut => RemainingQuantity <= 0;

    public void DecreaseQuantity(int quantity)
    {
        if (quantity <= 0) throw new ArgumentException("Quantity must be positive");
        if (quantity > RemainingQuantity)
            throw new DomainException($"Only {RemainingQuantity} items remaining for '{Title}'");
        RemainingQuantity -= quantity;
    }

    public void IncreaseQuantity(int quantity)
    {
        if (quantity <= 0) throw new ArgumentException("Quantity must be positive");
        if (RemainingQuantity + quantity > OriginalQuantity)
            throw new DomainException("Cannot exceed original quantity");
        RemainingQuantity += quantity;
    }
}
