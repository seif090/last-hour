using LastHour.Domain.Common;
using LastHour.Domain.Exceptions;

namespace LastHour.Domain.Entities;

public class Order : BaseAuditableEntity
{
    public string CustomerId { get; set; } = string.Empty;
    public string StoreId { get; set; } = string.Empty;
    public string Status { get; set; } = "Pending";
    public decimal Subtotal { get; set; }
    public decimal ServiceFee { get; set; }
    public decimal DeliveryFee { get; set; }
    public decimal Discount { get; set; }
    public decimal Total { get; set; }
    public string? CouponCode { get; set; }
    public string PaymentMethod { get; set; } = "Cash";
    public bool IsDelivery { get; set; }
    public string? DeliveryAddress { get; set; }
    public double? DeliveryLatitude { get; set; }
    public double? DeliveryLongitude { get; set; }
    public DateTime? EstimatedPickupTime { get; set; }
    public DateTime? PickedUpAt { get; set; }
    public DateTime? DeliveredAt { get; set; }
    public string? Notes { get; set; }
    public string? CancellationReason { get; set; }

    public ICollection<OrderItem> Items { get; set; } = [];
    public ICollection<PaymentTransaction> PaymentTransactions { get; set; } = [];

    public void CalculateTotal()
    {
        Subtotal = Items.Sum(i => i.Price * i.Quantity);
        Total = Subtotal + ServiceFee + DeliveryFee - Discount;
        if (Total < 0) Total = 0;
    }

    public void Cancel(string? reason = null)
    {
        if (Status == "Delivered" || Status == "PickedUp")
            throw new BusinessRuleException("Cannot cancel a completed order");
        if (Status == "Cancelled")
            throw new BusinessRuleException("Order is already cancelled");
        Status = "Cancelled";
        CancellationReason = reason;
    }
}

public class OrderItem : BaseEntity
{
    public string OrderId { get; set; } = string.Empty;
    public string OfferId { get; set; } = string.Empty;
    public string OfferTitle { get; set; } = string.Empty;
    public string? OfferImageUrl { get; set; }
    public decimal Price { get; set; }
    public int Quantity { get; set; }

    public decimal Total => Price * Quantity;
}
