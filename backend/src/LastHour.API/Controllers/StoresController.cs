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
        return Ok(new { message = "Nearby stores endpoint - query implementation pending" });
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetStoreDetails(string id)
    {
        return Ok(new { message = "Store details endpoint - query implementation pending" });
    }
}
