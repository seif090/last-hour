using LastHour.Application.Features.Cart.Commands;
using LastHour.Application.Features.Cart.DTOs;
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

    [HttpPost("add")]
    public async Task<IActionResult> AddToCart([FromBody] AddToCartRequest request)
    {
        var result = await _mediator.Send(new AddToCartCommand(request.OfferId, request.Quantity));
        return result.Succeeded ? Ok(result) : StatusCode(result.StatusCode, result);
    }

    [HttpPut("update")]
    public async Task<IActionResult> UpdateCartItem([FromBody] UpdateCartItemRequest request)
    {
        var result = await _mediator.Send(new UpdateCartItemCommand(request.CartItemId, request.Quantity));
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

    [HttpPost("apply-coupon")]
    public async Task<IActionResult> ApplyCoupon([FromBody] ApplyCouponRequest request)
    {
        var result = await _mediator.Send(new ApplyCouponCommand(request.Code));
        return result.Succeeded ? Ok(result) : StatusCode(result.StatusCode, result);
    }
}
