namespace LastHour.Application.Features.Stores.DTOs;

public record StoreResponse(
    string Id,
    string Name,
    string? Description,
    string? CoverImageUrl,
    string? LogoUrl,
    string Category,
    double Rating,
    int ReviewCount,
    double Latitude,
    double Longitude,
    string Address,
    string? Phone,
    string? Email,
    string? Website,
    bool IsOpen,
    string? OpeningHours,
    string? ClosingHours,
    double Distance,
    int ActiveOfferCount,
    bool IsFavorite,
    List<string> AcceptedPaymentMethods);

public record NearbyStoresRequest(
    double Latitude,
    double Longitude,
    double RadiusKm = 5);
