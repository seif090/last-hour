using LastHour.Application.Common.Interfaces;
using LastHour.Application.Common.Models;
using LastHour.Application.Features.Stores.DTOs;
using LastHour.Domain.Entities;
using LastHour.Domain.ValueObjects;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LastHour.Application.Features.Stores.Queries;

public record GetNearbyStoresQuery(double Latitude, double Longitude, double RadiusKm = 5)
    : IRequest<Result<List<StoreResponse>>>;

public record GetStoreDetailsQuery(string StoreId) : IRequest<Result<StoreResponse>>;
public record GetStoresQuery(int Page = 1, int PageSize = 20) : IRequest<Result<PaginatedList<StoreResponse>>>;
public record GetCategoriesQuery : IRequest<Result<List<Category>>>;
public record GetStoreOffersQuery(string StoreId, int Page = 1, int PageSize = 20) : IRequest<Result<PaginatedList<Domain.Entities.Offer>>>;

public class StoreQueryHandlers :
    IRequestHandler<GetNearbyStoresQuery, Result<List<StoreResponse>>>,
    IRequestHandler<GetStoreDetailsQuery, Result<StoreResponse>>,
    IRequestHandler<GetStoresQuery, Result<PaginatedList<StoreResponse>>>,
    IRequestHandler<GetCategoriesQuery, Result<List<Category>>>,
    IRequestHandler<GetStoreOffersQuery, Result<PaginatedList<Domain.Entities.Offer>>>
{
    private readonly IUnitOfWork _unitOfWork;

    public StoreQueryHandlers(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    public async Task<Result<List<StoreResponse>>> Handle(GetNearbyStoresQuery request, CancellationToken ct)
    {
        var stores = await _unitOfWork.Stores.GetAll()
            .Where(s => s.IsActive)
            .ToListAsync(ct);

        var result = stores.Select(s =>
        {
            var distance = new Location(request.Latitude, request.Longitude)
                .CalculateDistanceTo(new Location(s.Latitude, s.Longitude));
            return new
            {
                Store = s,
                Distance = distance
            };
        })
        .Where(x => x.Distance <= request.RadiusKm)
        .OrderBy(x => x.Distance)
        .Select(x => MapToResponse(x.Store, x.Distance))
        .ToList();

        return Result<List<StoreResponse>>.Success(result);
    }

    public async Task<Result<StoreResponse>> Handle(GetStoreDetailsQuery request, CancellationToken ct)
    {
        var store = await _unitOfWork.Stores.GetAll()
            .Include(s => s.Offers)
            .FirstOrDefaultAsync(s => s.Id == request.StoreId, ct);

        if (store is null)
            return Result<StoreResponse>.Failure("Store not found", 404);

        return Result<StoreResponse>.Success(MapToResponse(store, 0));
    }

    public async Task<Result<PaginatedList<StoreResponse>>> Handle(GetStoresQuery request, CancellationToken ct)
    {
        var query = _unitOfWork.Stores.GetAll()
            .Where(s => s.IsActive)
            .OrderByDescending(s => s.Rating);

        var total = await query.CountAsync(ct);
        var items = await query.Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize).ToListAsync(ct);

        var mapped = items.Select(s => MapToResponse(s, 0)).ToList();
        return Result<PaginatedList<StoreResponse>>.Success(
            new PaginatedList<StoreResponse>(mapped, total, request.Page, request.PageSize));
    }

    public async Task<Result<List<Category>>> Handle(GetCategoriesQuery request, CancellationToken ct)
    {
        var categories = await _unitOfWork.Categories.GetAll()
            .Where(c => c.IsActive)
            .OrderBy(c => c.DisplayOrder)
            .ToListAsync(ct);
        return Result<List<Category>>.Success(categories);
    }

    public async Task<Result<PaginatedList<Domain.Entities.Offer>>> Handle(GetStoreOffersQuery request, CancellationToken ct)
    {
        var query = _unitOfWork.Offers.GetAll()
            .Where(o => o.StoreId == request.StoreId && o.IsActive && !o.IsExpired)
            .OrderByDescending(o => o.CreatedAt);

        var total = await query.CountAsync(ct);
        var items = await query.Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize).ToListAsync(ct);

        return Result<PaginatedList<Domain.Entities.Offer>>.Success(
            new PaginatedList<Domain.Entities.Offer>(items, total, request.Page, request.PageSize));
    }

    private static StoreResponse MapToResponse(Domain.Entities.Store s, double distance) => new(
        s.Id, s.Name, s.Description, s.CoverImageUrl, s.LogoUrl,
        s.Category, s.Rating, s.ReviewCount, s.Latitude, s.Longitude,
        s.Address, s.Phone, s.Email, s.Website, s.IsOpen,
        s.OpeningHours, s.ClosingHours, distance, s.ActiveOfferCount,
        false, (s.AcceptedPaymentMethods ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries).ToList());
}
