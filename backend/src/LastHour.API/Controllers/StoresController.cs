using LastHour.Application.Features.Stores.Commands;
using LastHour.Application.Features.Stores.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LastHour.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class StoresController : ControllerBase
{
    private readonly IMediator _mediator;

    public StoresController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    public async Task<IActionResult> GetStores([FromQuery] int page = 1, [FromQuery(Name = "per_page")] int pageSize = 20)
    {
        var result = await _mediator.Send(new GetStoresQuery(page, pageSize));
        return Ok(result);
    }

    [HttpGet("nearby")]
    public async Task<IActionResult> GetNearbyStores([FromQuery] double latitude, [FromQuery] double longitude,
        [FromQuery(Name = "radius_km")] double radiusKm = 5)
    {
        var result = await _mediator.Send(new GetNearbyStoresQuery(latitude, longitude, radiusKm));
        return Ok(result);
    }

    [HttpGet("categories")]
    public async Task<IActionResult> GetCategories()
    {
        var result = await _mediator.Send(new GetCategoriesQuery());
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetStoreDetails(string id)
    {
        var result = await _mediator.Send(new GetStoreDetailsQuery(id));
        return result.Succeeded ? Ok(result) : StatusCode(result.StatusCode, result);
    }

    [HttpGet("{storeId}/offers")]
    public async Task<IActionResult> GetStoreOffers(string storeId, [FromQuery] int page = 1, [FromQuery(Name = "per_page")] int pageSize = 20)
    {
        var result = await _mediator.Send(new GetStoreOffersQuery(storeId, page, pageSize));
        return Ok(result);
    }

    [Authorize]
    [HttpPost("{storeId}/favorite")]
    public async Task<IActionResult> ToggleFavorite(string storeId)
    {
        var result = await _mediator.Send(new ToggleStoreFavoriteCommand(storeId));
        return result.Succeeded ? Ok(result) : StatusCode(result.StatusCode, result);
    }
}
