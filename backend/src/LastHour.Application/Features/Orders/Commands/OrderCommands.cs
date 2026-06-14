using LastHour.Application.Common.Interfaces;
using LastHour.Application.Common.Models;
using LastHour.Application.Features.Orders.DTOs;
using MediatR;

namespace LastHour.Application.Features.Orders.Commands;

public record CreateOrderCommand(
    string StoreId, List<OrderItemRequest> Items, string PaymentMethod,
    bool IsDelivery = false, string? DeliveryAddress = null,
    string? CouponCode = null, string? Notes = null) : IRequest<Result<OrderResponse>>;

public record CancelOrderCommand(string OrderId) : IRequest<Result>;

public class CreateOrderCommandHandler : IRequestHandler<CreateOrderCommand, Result<OrderResponse>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;

    public CreateOrderCommandHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUser)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<Result<OrderResponse>> Handle(CreateOrderCommand request, CancellationToken ct)
    {
        var userId = _currentUser.UserId;
        if (string.IsNullOrEmpty(userId)) return Result<OrderResponse>.Failure("Not authenticated", 401);

        var order = new Domain.Entities.Order
        {
            CustomerId = userId, StoreId = request.StoreId, Status = "Pending",
            PaymentMethod = request.PaymentMethod, IsDelivery = request.IsDelivery,
            DeliveryAddress = request.DeliveryAddress, CouponCode = request.CouponCode, Notes = request.Notes
        };

        foreach (var item in request.Items)
            order.Items.Add(new Domain.Entities.OrderItem
            {
                OfferId = item.OfferId, OfferTitle = item.OfferTitle,
                Price = item.Price, Quantity = item.Quantity
            });

        order.CalculateTotal();
        await _unitOfWork.Orders.AddAsync(order);
        await _unitOfWork.CompleteAsync(ct);

        return Result<OrderResponse>.Success(new OrderResponse(
            order.Id, order.StoreId, "", null,
            order.Items.Select(i => new OrderItemResponse(i.Id, i.OfferId, i.OfferTitle,
                i.OfferImageUrl, i.Price, i.Quantity, i.Total)).ToList(),
            order.Subtotal, order.ServiceFee, order.DeliveryFee, order.Discount,
            order.Total, order.Status, order.CouponCode, order.PaymentMethod,
            order.IsDelivery, order.DeliveryAddress, order.CreatedAt,
            order.EstimatedPickupTime, order.PickedUpAt, order.Notes));
    }
}

public class CancelOrderCommandHandler : IRequestHandler<CancelOrderCommand, Result>
{
    private readonly IUnitOfWork _unitOfWork;
    public CancelOrderCommandHandler(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    public async Task<Result> Handle(CancelOrderCommand request, CancellationToken ct)
    {
        var order = await _unitOfWork.Orders.GetByIdAsync(request.OrderId);
        if (order is null) return Result.Failure("Order not found", 404);

        try
        {
            order.Cancel();
            await _unitOfWork.CompleteAsync(ct);
            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure(ex.Message);
        }
    }
}
