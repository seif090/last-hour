using LastHour.Application.Features.Merchant.Queries;
using LastHour.Application.Features.Offers.DTOs;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LastHour.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Merchant")]
public class MerchantController : ControllerBase
{
    private readonly IMediator _mediator;

    public MerchantController(IMediator mediator) => _mediator = mediator;

    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboard()
    {
        var result = await _mediator.Send(new GetMerchantDashboardQuery());
        return result.Succeeded ? Ok(result) : StatusCode(result.StatusCode, result);
    }

    [HttpGet("reports")]
    public async Task<IActionResult> GetReports()
    {
        var result = await _mediator.Send(new GetMerchantReportsQuery());
        return result.Succeeded ? Ok(result) : StatusCode(result.StatusCode, result);
    }

    [HttpGet("offers")]
    public async Task<IActionResult> GetMyOffers([FromQuery] int page = 1, [FromQuery(Name = "per_page")] int pageSize = 20)
    {
        var result = await _mediator.Send(new GetMyOffersQuery(page, pageSize));
        return Ok(result);
    }

    [HttpPost("offers")]
    public async Task<IActionResult> CreateOffer([FromBody] CreateOfferRequest request)
    {
        var result = await _mediator.Send(new CreateMerchantOfferCommand(
            request.Title, request.Description, request.Category,
            request.OriginalPrice, request.DiscountPrice, request.Quantity,
            request.PickupStart, request.PickupEnd));
        return result.Succeeded ? Ok(result) : StatusCode(result.StatusCode, result);
    }

    [HttpPut("offers/{offerId}")]
    public async Task<IActionResult> UpdateOffer(string offerId, [FromBody] UpdateOfferRequest request)
    {
        var result = await _mediator.Send(new UpdateMerchantOfferCommand(
            offerId, request.Title, request.Description,
            request.OriginalPrice, request.DiscountPrice, request.Quantity));
        return result.Succeeded ? Ok(result) : StatusCode(result.StatusCode, result);
    }

    [HttpPatch("offers/{offerId}")]
    public async Task<IActionResult> ToggleOfferActive(string offerId)
    {
        var result = await _mediator.Send(new ToggleMerchantOfferActiveCommand(offerId));
        return result.Succeeded ? Ok(result) : StatusCode(result.StatusCode, result);
    }

    [HttpGet("orders")]
    public async Task<IActionResult> GetOrders([FromQuery] int page = 1, [FromQuery(Name = "per_page")] int pageSize = 20)
    {
        var result = await _mediator.Send(new GetMerchantOrdersQuery(page, pageSize));
        return Ok(result);
    }

    [HttpPatch("orders/{orderId}")]
    public async Task<IActionResult> UpdateOrderStatus(string orderId, [FromBody] Dictionary<string, string?> body)
    {
        var status = body.GetValueOrDefault("status") ?? "confirmed";
        var result = await _mediator.Send(new UpdateMerchantOrderCommand(orderId, status));
        return result.Succeeded ? Ok(result) : StatusCode(result.StatusCode, result);
    }
}
