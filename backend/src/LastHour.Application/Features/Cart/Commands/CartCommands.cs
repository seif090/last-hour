using LastHour.Application.Common.Models;
using LastHour.Application.Features.Orders.DTOs;
using MediatR;

namespace LastHour.Application.Features.Cart.Commands;

public record AddToCartCommand(string OfferId, int Quantity = 1) : IRequest<Result>;
public record UpdateCartItemCommand(string CartItemId, int Quantity) : IRequest<Result>;
public record RemoveFromCartCommand(string CartItemId) : IRequest<Result>;
public record ClearCartCommand : IRequest<Result>;
public record ApplyCouponCommand(string Code) : IRequest<Result<double>>;
