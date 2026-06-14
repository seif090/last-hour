using LastHour.Application.Features.Cart.Commands;
using LastHour.Application.Features.Cart.DTOs;
using LastHour.Application.Features.Cart.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LastHour.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CartController : ControllerBase
{
    private readonly IMediator _mediator;

    public CartController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    public async Task<IActionResult> GetCart()
    {
        var result = await _mediator.Send(new GetCartQuery());
        return result.Succeeded ? Ok(result) : StatusCode(result.StatusCode, result);
    }

    [HttpPost]
    public async Task<IActionResult> AddToCart([FromBody] AddToCartRequest request)
    {
        var result = await _mediator.Send(new AddToCartCommand(request.OfferId, request.Quantity));
        return result.Succeeded ? Ok(result) : StatusCode(result.StatusCode, result);
    }

    [HttpPut("{cartItemId}")]
    public async Task<IActionResult> UpdateCartItem(string cartItemId, [FromBody] Dictionary<string, object?> body)
    {
        var quantity = Convert.ToInt32(body.GetValueOrDefault("quantity") ?? 1);
        var result = await _mediator.Send(new UpdateCartItemCommand(cartItemId, quantity));
        return result.Succeeded ? Ok(result) : StatusCode(result.StatusCode, result);
    }

    [HttpDelete("{cartItemId}")]
    public async Task<IActionResult> RemoveFromCart(string cartItemId)
    {
        var result = await _mediator.Send(new RemoveFromCartCommand(cartItemId));
        return result.Succeeded ? Ok(result) : StatusCode(result.StatusCode, result);
    }

    [HttpDelete]
    public async Task<IActionResult> ClearCart()
    {
        var result = await _mediator.Send(new ClearCartCommand());
        return Ok(result);
    }

    [HttpPost("validate-coupon")]
    public async Task<IActionResult> ValidateCoupon([FromBody] ApplyCouponRequest request)
    {
        var result = await _mediator.Send(new ApplyCouponCommand(request.Code));
        return result.Succeeded ? Ok(result) : StatusCode(result.StatusCode, result);
    }
}
