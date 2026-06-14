namespace LastHour.Application.Features.Cart.DTOs;

public record CartItemResponse(
    string Id,
    string OfferId,
    string Title,
    string? ImageUrl,
    string StoreId,
    string StoreName,
    decimal Price,
    int Quantity,
    int MaxQuantity,
    DateTime ExpiryTime,
    decimal Total);

public record AddToCartRequest(
    string OfferId,
    int Quantity = 1);

public record UpdateCartItemRequest(
    string CartItemId,
    int Quantity);

public record ApplyCouponRequest(string Code);
