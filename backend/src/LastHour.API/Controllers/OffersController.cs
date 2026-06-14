using LastHour.Application.Features.Offers.Commands;
using LastHour.Application.Features.Offers.DTOs;
using LastHour.Application.Features.Offers.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LastHour.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OffersController : ControllerBase
{
    private readonly IMediator _mediator;

    public OffersController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    public async Task<IActionResult> GetOffers([FromQuery] int page = 1, [FromQuery(Name = "per_page")] int pageSize = 20)
    {
        var result = await _mediator.Send(new GetOffersQuery(page, pageSize));
        return Ok(result);
    }

    [HttpGet("featured")]
    public async Task<IActionResult> GetFeatured([FromQuery] int page = 1, [FromQuery(Name = "per_page")] int pageSize = 10)
    {
        var result = await _mediator.Send(new GetFeaturedOffersQuery(page, pageSize));
        return Ok(result);
    }

    [HttpGet("category/{category}")]
    public async Task<IActionResult> GetByCategory(string category, [FromQuery] int page = 1, [FromQuery(Name = "per_page")] int pageSize = 20)
    {
        var result = await _mediator.Send(new GetOffersByCategoryQuery(category, page, pageSize));
        return Ok(result);
    }

    [HttpGet("nearby")]
    public async Task<IActionResult> GetNearby([FromQuery] double latitude, [FromQuery] double longitude,
        [FromQuery(Name = "radius_km")] double radiusKm = 5)
    {
        var result = await _mediator.Send(new GetNearbyOffersQuery(latitude, longitude, radiusKm));
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetDetails(string id)
    {
        var result = await _mediator.Send(new GetOfferDetailsQuery(id));
        return result.Succeeded ? Ok(result) : StatusCode(result.StatusCode, result);
    }

    [HttpGet("search")]
    public async Task<IActionResult> Search([FromQuery] string q, [FromQuery] int page = 1, [FromQuery(Name = "per_page")] int pageSize = 20)
    {
        var result = await _mediator.Send(new SearchOffersQuery(q, page, pageSize));
        return Ok(result);
    }

    [Authorize]
    [HttpPost("{id}/favorite")]
    public async Task<IActionResult> ToggleFavorite(string id)
    {
        var result = await _mediator.Send(new ToggleOfferFavoriteCommand(id));
        return result.Succeeded ? Ok(result) : StatusCode(result.StatusCode, result);
    }
}
