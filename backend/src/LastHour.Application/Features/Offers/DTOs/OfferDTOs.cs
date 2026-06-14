namespace LastHour.Application.Features.Offers.DTOs;

public record OfferResponse(
    string Id,
    string Title,
    string Description,
    string StoreId,
    string StoreName,
    string? StoreLogoUrl,
    decimal OriginalPrice,
    decimal DiscountPrice,
    int RemainingQuantity,
    int OriginalQuantity,
    DateTime ExpiryTime,
    string Category,
    string? ImageUrl,
    List<string> ImageUrls,
    double Distance,
    double Rating,
    int ReviewCount,
    bool IsFavorite,
    bool IsActive,
    DateTime CreatedAt,
    decimal DiscountPercentage,
    bool IsExpired,
    bool IsSoldOut);

public record CreateOfferRequest(
    string Title,
    string Description,
    string Category,
    decimal OriginalPrice,
    decimal DiscountPrice,
    int Quantity,
    DateTime? PickupStart,
    DateTime? PickupEnd);

public record UpdateOfferRequest(
    string? Title,
    string? Description,
    decimal? OriginalPrice,
    decimal? DiscountPrice,
    int? Quantity);

public record ToggleFavoriteRequest(string OfferId);
