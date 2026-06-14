using LastHour.Application.Features.Orders.Commands;
using LastHour.Application.Features.Orders.DTOs;
using LastHour.Application.Features.Orders.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LastHour.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class OrdersController : ControllerBase
{
    private readonly IMediator _mediator;

    public OrdersController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    public async Task<IActionResult> GetOrders([FromQuery] string? status, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var result = await _mediator.Send(new GetOrdersQuery(status, page, pageSize));
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetOrderDetails(string id)
    {
        var result = await _mediator.Send(new GetOrderDetailsQuery(id));
        return result.Succeeded ? Ok(result) : StatusCode(result.StatusCode, result);
    }

    [HttpPost]
    public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequest request)
    {
        var result = await _mediator.Send(new CreateOrderCommand(
            request.StoreId, request.Items, request.PaymentMethod,
            request.IsDelivery, request.DeliveryAddress,
            request.CouponCode, request.Notes));
        return result.Succeeded ? Ok(result) : StatusCode(result.StatusCode, result);
    }

    [HttpPost("{id}/cancel")]
    public async Task<IActionResult> CancelOrder(string id)
    {
        var result = await _mediator.Send(new CancelOrderCommand(id));
        return result.Succeeded ? Ok(result) : StatusCode(result.StatusCode, result);
    }
}
