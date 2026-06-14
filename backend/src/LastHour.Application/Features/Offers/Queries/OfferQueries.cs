using LastHour.Application.Common.Interfaces;
using LastHour.Application.Common.Models;
using LastHour.Application.Features.Offers.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LastHour.Application.Features.Offers.Queries;

public record GetFeaturedOffersQuery(int Page = 1, int PageSize = 10) : IRequest<Result<PaginatedList<OfferResponse>>>;
public record GetOffersByCategoryQuery(string Category, int Page = 1, int PageSize = 20) : IRequest<Result<PaginatedList<OfferResponse>>>;
public record GetNearbyOffersQuery(double Latitude, double Longitude, double RadiusKm = 5) : IRequest<Result<List<OfferResponse>>>;
public record GetOfferDetailsQuery(string OfferId) : IRequest<Result<OfferResponse>>;
public record SearchOffersQuery(string SearchTerm, int Page = 1, int PageSize = 20) : IRequest<Result<PaginatedList<OfferResponse>>>;
public record GetOffersQuery(int Page = 1, int PageSize = 20) : IRequest<Result<PaginatedList<OfferResponse>>>;

public class OfferQueryHandlers :
    IRequestHandler<GetFeaturedOffersQuery, Result<PaginatedList<OfferResponse>>>,
    IRequestHandler<GetOffersByCategoryQuery, Result<PaginatedList<OfferResponse>>>,
    IRequestHandler<GetNearbyOffersQuery, Result<List<OfferResponse>>>,
    IRequestHandler<GetOfferDetailsQuery, Result<OfferResponse>>,
    IRequestHandler<SearchOffersQuery, Result<PaginatedList<OfferResponse>>>,
    IRequestHandler<GetOffersQuery, Result<PaginatedList<OfferResponse>>>
{
    private readonly IUnitOfWork _unitOfWork;

    public OfferQueryHandlers(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    private static OfferResponse MapToResponse(Domain.Entities.Offer o) => new(
        o.Id, o.Title, o.Description, o.StoreId, o.Store?.Name ?? "", o.Store?.LogoUrl,
        o.OriginalPrice, o.DiscountPrice, o.RemainingQuantity, o.OriginalQuantity,
        o.ExpiryTime, o.Category, o.ImageUrl,
        (o.ImageUrls ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries).ToList(),
        0, o.Rating, o.ReviewCount, false, o.IsActive, o.CreatedAt,
        o.DiscountPercentage, o.IsExpired, o.IsSoldOut);

    public async Task<Result<PaginatedList<OfferResponse>>> Handle(
        GetFeaturedOffersQuery request, CancellationToken ct)
    {
        var query = _unitOfWork.Offers.GetAll()
            .Include(o => o.Store)
            .Where(o => o.IsActive && o.IsFeatured && !o.IsExpired && !o.IsSoldOut)
            .OrderByDescending(o => o.CreatedAt);

        var total = await query.CountAsync(ct);
        var items = await query.Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize).ToListAsync(ct);

        return Result<PaginatedList<OfferResponse>>.Success(
            new PaginatedList<OfferResponse>(items.Select(MapToResponse).ToList(), total, request.Page, request.PageSize));
    }

    public async Task<Result<PaginatedList<OfferResponse>>> Handle(
        GetOffersByCategoryQuery request, CancellationToken ct)
    {
        var query = _unitOfWork.Offers.GetAll()
            .Include(o => o.Store)
            .Where(o => o.IsActive && o.Category == request.Category && !o.IsExpired && !o.IsSoldOut)
            .OrderByDescending(o => o.CreatedAt);

        var total = await query.CountAsync(ct);
        var items = await query.Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize).ToListAsync(ct);

        return Result<PaginatedList<OfferResponse>>.Success(
            new PaginatedList<OfferResponse>(items.Select(MapToResponse).ToList(), total, request.Page, request.PageSize));
    }

    public async Task<Result<List<OfferResponse>>> Handle(
        GetNearbyOffersQuery request, CancellationToken ct)
    {
        var offers = await _unitOfWork.Offers.GetAll()
            .Include(o => o.Store)
            .Where(o => o.IsActive && !o.IsExpired && !o.IsSoldOut)
            .ToListAsync(ct);

        var result = offers
            .Where(o => o.Store is not null &&
                new Domain.ValueObjects.Location(request.Latitude, request.Longitude)
                    .CalculateDistanceTo(new Domain.ValueObjects.Location(o.Store.Latitude, o.Store.Longitude)) <= request.RadiusKm)
            .Select(MapToResponse).ToList();

        return Result<List<OfferResponse>>.Success(result);
    }

    public async Task<Result<OfferResponse>> Handle(
        GetOfferDetailsQuery request, CancellationToken ct)
    {
        var offer = await _unitOfWork.Offers.GetAll()
            .Include(o => o.Store)
            .FirstOrDefaultAsync(o => o.Id == request.OfferId, ct);

        return offer is null
            ? Result<OfferResponse>.Failure("Offer not found", 404)
            : Result<OfferResponse>.Success(MapToResponse(offer));
    }

    public async Task<Result<PaginatedList<OfferResponse>>> Handle(
        GetOffersQuery request, CancellationToken ct)
    {
        var query = _unitOfWork.Offers.GetAll()
            .Include(o => o.Store)
            .Where(o => o.IsActive && !o.IsExpired && !o.IsSoldOut)
            .OrderByDescending(o => o.CreatedAt);

        var total = await query.CountAsync(ct);
        var items = await query.Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize).ToListAsync(ct);

        return Result<PaginatedList<OfferResponse>>.Success(
            new PaginatedList<OfferResponse>(items.Select(MapToResponse).ToList(), total, request.Page, request.PageSize));
    }

    public async Task<Result<PaginatedList<OfferResponse>>> Handle(
        SearchOffersQuery request, CancellationToken ct)
    {
        var term = request.SearchTerm.ToLower();
        var query = _unitOfWork.Offers.GetAll()
            .Include(o => o.Store)
            .Where(o => o.IsActive && !o.IsExpired &&
                (o.Title.ToLower().Contains(term) || o.Description.ToLower().Contains(term) || o.Category.ToLower().Contains(term)))
            .OrderByDescending(o => o.CreatedAt);

        var total = await query.CountAsync(ct);
        var items = await query.Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize).ToListAsync(ct);

        return Result<PaginatedList<OfferResponse>>.Success(
            new PaginatedList<OfferResponse>(items.Select(MapToResponse).ToList(), total, request.Page, request.PageSize));
    }
}
