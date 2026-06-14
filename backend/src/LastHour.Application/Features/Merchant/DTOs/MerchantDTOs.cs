namespace LastHour.Application.Features.Merchant.DTOs;

public record MerchantLoginRequest(string Email, string Password);
public record MerchantRegisterRequest(string FullName, string Email, string Password, string Phone, string StoreName, string StoreCategory, string StoreAddress);

public record MerchantResponse(
    string Id, string FullName, string Email, string? Phone,
    string? LogoUrl, string? CoverImageUrl, string StoreName,
    string StoreCategory, string? StoreDescription, string StoreAddress,
    double? StoreLatitude, double? StoreLongitude, bool IsOnline,
    int ActiveOfferCount, int TodayOrderCount, double TodayRevenue,
    double Rating, string AccessToken, string RefreshToken);

public record DashboardResponse(
    int TotalOffers, int ActiveOffers, int TodayOrders,
    double TodayRevenue, double TotalRevenue, double AverageRating, int TotalCustomers);

public record TopSellingItem(string OfferId, string Title, int QuantitySold, double Revenue);

public record MerchantReportsResponse(
    double TotalRevenue, int TotalOrders, int TotalOffers,
    double AverageRating, List<TopSellingItem> TopSellingItems,
    List<TransactionItem> RecentTransactions);

public record TransactionItem(string Id, DateTime Date, string Description, double Amount, string Status);
