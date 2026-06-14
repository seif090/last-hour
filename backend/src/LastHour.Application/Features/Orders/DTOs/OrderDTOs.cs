namespace LastHour.Application.Features.Orders.DTOs;

public record OrderItemResponse(
    string Id,
    string OfferId,
    string OfferTitle,
    string? OfferImageUrl,
    decimal Price,
    int Quantity,
    decimal Total);

public record OrderResponse(
    string Id,
    string StoreId,
    string StoreName,
    string? StoreLogoUrl,
    List<OrderItemResponse> Items,
    decimal Subtotal,
    decimal ServiceFee,
    decimal DeliveryFee,
    decimal Discount,
    decimal Total,
    string Status,
    string? CouponCode,
    string PaymentMethod,
    bool IsDelivery,
    string? DeliveryAddress,
    DateTime CreatedAt,
    DateTime? EstimatedPickupTime,
    DateTime? PickedUpAt,
    string? Notes);

public record CreateOrderRequest(
    string StoreId,
    List<OrderItemRequest> Items,
    string PaymentMethod,
    bool IsDelivery = false,
    string? DeliveryAddress = null,
    string? CouponCode = null,
    string? Notes = null);

public record OrderItemRequest(
    string OfferId,
    string OfferTitle,
    decimal Price,
    int Quantity);
