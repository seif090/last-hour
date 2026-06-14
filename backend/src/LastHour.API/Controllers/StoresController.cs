using LastHour.Application.Features.Stores.DTOs;
using LastHour.Application.Features.Stores.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace LastHour.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class StoresController : ControllerBase
{
    private readonly IMediator _mediator;

    public StoresController(IMediator mediator) => _mediator = mediator;

    [HttpGet("nearby")]
    public async Task<IActionResult> GetNearbyStores([FromQuery] double latitude, [FromQuery] double longitude, [FromQuery] double radiusKm = 5)
    {
        var result = await _mediator.Send(new GetNearbyStoresQuery(latitude, longitude, radiusKm));
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetStoreDetails(string id)
    {
        var result = await _mediator.Send(new GetStoreDetailsQuery(id));
        return result.Succeeded ? Ok(result) : StatusCode(result.StatusCode, result);
    }
}
