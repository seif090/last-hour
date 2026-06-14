using LastHour.Application.Common.Interfaces;
using LastHour.Application.Common.Models;
using LastHour.Application.Features.Stores.DTOs;
using LastHour.Domain.ValueObjects;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LastHour.Application.Features.Stores.Queries;

public record GetNearbyStoresQuery(double Latitude, double Longitude, double RadiusKm = 5)
    : IRequest<Result<List<StoreResponse>>>;

public record GetStoreDetailsQuery(string StoreId) : IRequest<Result<StoreResponse>>;

public class StoreQueryHandlers :
    IRequestHandler<GetNearbyStoresQuery, Result<List<StoreResponse>>>,
    IRequestHandler<GetStoreDetailsQuery, Result<StoreResponse>>
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

    private static StoreResponse MapToResponse(Domain.Entities.Store s, double distance) => new(
        s.Id, s.Name, s.Description, s.CoverImageUrl, s.LogoUrl,
        s.Category, s.Rating, s.ReviewCount, s.Latitude, s.Longitude,
        s.Address, s.Phone, s.Email, s.Website, s.IsOpen,
        s.OpeningHours, s.ClosingHours, distance, s.ActiveOfferCount,
        false, (s.AcceptedPaymentMethods ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries).ToList());
}
