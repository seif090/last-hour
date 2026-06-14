using LastHour.Application.Common.Interfaces;
using LastHour.Application.Common.Models;
using LastHour.Application.Features.Merchant.DTOs;
using LastHour.Application.Features.Offers.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LastHour.Application.Features.Merchant.Queries;

public record GetMerchantDashboardQuery : IRequest<Result<DashboardResponse>>;
public record GetMerchantReportsQuery : IRequest<Result<MerchantReportsResponse>>;
public record GetMyOffersQuery(int Page = 1, int PageSize = 20) : IRequest<Result<PaginatedList<OfferResponse>>>;
public record CreateMerchantOfferCommand(
    string Title, string Description, string Category,
    decimal OriginalPrice, decimal DiscountPrice, int Quantity,
    DateTime? PickupStart = null, DateTime? PickupEnd = null) : IRequest<Result<OfferResponse>>;

public class MerchantQueryHandlers :
    IRequestHandler<GetMerchantDashboardQuery, Result<DashboardResponse>>,
    IRequestHandler<GetMerchantReportsQuery, Result<MerchantReportsResponse>>,
    IRequestHandler<GetMyOffersQuery, Result<PaginatedList<OfferResponse>>>,
    IRequestHandler<CreateMerchantOfferCommand, Result<OfferResponse>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;

    public MerchantQueryHandlers(IUnitOfWork unitOfWork, ICurrentUserService currentUser)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    private async Task<string?> GetStoreIdAsync()
    {
        var userId = _currentUser.UserId;
        if (string.IsNullOrEmpty(userId)) return null;
        var store = await _unitOfWork.Stores.GetAll()
            .FirstOrDefaultAsync(s => s.OwnerId == userId);
        return store?.Id;
    }

    public async Task<Result<DashboardResponse>> Handle(GetMerchantDashboardQuery request, CancellationToken ct)
    {
        var storeId = await GetStoreIdAsync();
        if (storeId is null) return Result<DashboardResponse>.Failure("Store not found", 404);

        var offers = await _unitOfWork.Offers.GetAll()
            .Where(o => o.StoreId == storeId).ToListAsync(ct);

        var orders = await _unitOfWork.Orders.GetAll()
            .Where(o => o.StoreId == storeId).ToListAsync(ct);

        var today = DateTime.UtcNow.Date;
        var todayOrders = orders.Where(o => o.CreatedAt >= today).ToList();

        return Result<DashboardResponse>.Success(new DashboardResponse(
            offers.Count,
            offers.Count(o => o.IsActive && !o.IsExpired && !o.IsSoldOut),
            todayOrders.Count,
            (double)todayOrders.Sum(o => o.Total),
            (double)orders.Sum(o => o.Total),
            offers.Average(o => o.Rating),
            orders.Select(o => o.CustomerId).Distinct().Count()));
    }

    public async Task<Result<MerchantReportsResponse>> Handle(GetMerchantReportsQuery request, CancellationToken ct)
    {
        var storeId = await GetStoreIdAsync();
        if (storeId is null) return Result<MerchantReportsResponse>.Failure("Store not found", 404);

        var orders = await _unitOfWork.Orders.GetAll()
            .Include(o => o.Items)
            .Where(o => o.StoreId == storeId).ToListAsync(ct);

        var offers = await _unitOfWork.Offers.GetAll()
            .Where(o => o.StoreId == storeId).ToListAsync(ct);

        var topSelling = orders.SelectMany(o => o.Items)
            .GroupBy(i => new { i.OfferId, i.OfferTitle })
            .Select(g => new TopSellingItem(
                g.Key.OfferId, g.Key.OfferTitle,
                g.Sum(i => i.Quantity), (double)g.Sum(i => i.Total)))
            .OrderByDescending(x => x.QuantitySold)
            .Take(10).ToList();

        var transactions = orders
            .OrderByDescending(o => o.CreatedAt)
            .Take(20)
            .Select(o => new TransactionItem(
                o.Id, o.CreatedAt, $"Order #{o.Id[..8]}", (double)o.Total, o.Status))
            .ToList();

        return Result<MerchantReportsResponse>.Success(new MerchantReportsResponse(
            (double)orders.Sum(o => o.Total),
            orders.Count,
            offers.Count,
            offers.Any() ? offers.Average(o => o.Rating) : 0,
            topSelling,
            transactions));
    }

    public async Task<Result<PaginatedList<OfferResponse>>> Handle(GetMyOffersQuery request, CancellationToken ct)
    {
        var storeId = await GetStoreIdAsync();
        if (storeId is null) return Result<PaginatedList<OfferResponse>>.Failure("Store not found", 404);

        var query = _unitOfWork.Offers.GetAll()
            .Where(o => o.StoreId == storeId)
            .OrderByDescending(o => o.CreatedAt);

        var total = await query.CountAsync(ct);
        var items = await query.Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize).ToListAsync(ct);

        var mapped = items.Select(o => new OfferResponse(
            o.Id, o.Title, o.Description, o.StoreId, "", null,
            o.OriginalPrice, o.DiscountPrice, o.RemainingQuantity, o.OriginalQuantity,
            o.ExpiryTime, o.Category, o.ImageUrl,
            (o.ImageUrls ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries).ToList(),
            0, o.Rating, o.ReviewCount, false, o.IsActive, o.CreatedAt,
            o.DiscountPercentage, o.IsExpired, o.IsSoldOut)).ToList();

        return Result<PaginatedList<OfferResponse>>.Success(
            new PaginatedList<OfferResponse>(mapped, total, request.Page, request.PageSize));
    }

    public async Task<Result<OfferResponse>> Handle(CreateMerchantOfferCommand request, CancellationToken ct)
    {
        var storeId = await GetStoreIdAsync();
        if (storeId is null) return Result<OfferResponse>.Failure("Store not found", 404);

        var offer = new Domain.Entities.Offer
        {
            StoreId = storeId,
            Title = request.Title,
            Description = request.Description,
            Category = request.Category,
            OriginalPrice = request.OriginalPrice,
            DiscountPrice = request.DiscountPrice,
            OriginalQuantity = request.Quantity,
            RemainingQuantity = request.Quantity,
            ExpiryTime = request.PickupEnd ?? DateTime.UtcNow.AddDays(1),
            PickupStartTime = request.PickupStart,
            PickupEndTime = request.PickupEnd,
            IsActive = true
        };

        await _unitOfWork.Offers.AddAsync(offer);
        await _unitOfWork.CompleteAsync(ct);

        return Result<OfferResponse>.Success(new OfferResponse(
            offer.Id, offer.Title, offer.Description, storeId, "", null,
            offer.OriginalPrice, offer.DiscountPrice, offer.RemainingQuantity, offer.OriginalQuantity,
            offer.ExpiryTime, offer.Category, offer.ImageUrl,
            new List<string>(), 0, 0, 0, false, true, offer.CreatedAt,
            offer.DiscountPercentage, false, false));
    }
}
