using LastHour.Application.Common.Interfaces;
using LastHour.Application.Common.Models;
using LastHour.Application.Features.Orders.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LastHour.Application.Features.Orders.Queries;

public record GetOrdersQuery(string? Status = null, int Page = 1, int PageSize = 20)
    : IRequest<Result<PaginatedList<OrderResponse>>>;
public record GetOrderDetailsQuery(string OrderId) : IRequest<Result<OrderResponse>>;

public class OrderQueryHandlers :
    IRequestHandler<GetOrdersQuery, Result<PaginatedList<OrderResponse>>>,
    IRequestHandler<GetOrderDetailsQuery, Result<OrderResponse>>
{
    private readonly IUnitOfWork _unitOfWork;
    public OrderQueryHandlers(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    private static OrderResponse MapToResponse(Domain.Entities.Order o) => new(
        o.Id, o.StoreId, "", null,
        o.Items.Select(i => new OrderItemResponse(i.Id, i.OfferId, i.OfferTitle,
            i.OfferImageUrl, i.Price, i.Quantity, i.Total)).ToList(),
        o.Subtotal, o.ServiceFee, o.DeliveryFee, o.Discount, o.Total,
        o.Status, o.CouponCode, o.PaymentMethod, o.IsDelivery,
        o.DeliveryAddress, o.CreatedAt, o.EstimatedPickupTime, o.PickedUpAt, o.Notes);

    public async Task<Result<PaginatedList<OrderResponse>>> Handle(
        GetOrdersQuery request, CancellationToken ct)
    {
        var query = _unitOfWork.Orders.GetAll().Include(o => o.Items).AsQueryable();
        if (!string.IsNullOrEmpty(request.Status))
            query = query.Where(o => o.Status == request.Status);
        query = query.OrderByDescending(o => o.CreatedAt);

        var total = await query.CountAsync(ct);
        var items = await query.Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize).ToListAsync(ct);

        return Result<PaginatedList<OrderResponse>>.Success(
            new PaginatedList<OrderResponse>(items.Select(MapToResponse).ToList(), total, request.Page, request.PageSize));
    }

    public async Task<Result<OrderResponse>> Handle(
        GetOrderDetailsQuery request, CancellationToken ct)
    {
        var order = await _unitOfWork.Orders.GetAll().Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == request.OrderId, ct);
        return order is null
            ? Result<OrderResponse>.Failure("Order not found", 404)
            : Result<OrderResponse>.Success(MapToResponse(order));
    }
}
