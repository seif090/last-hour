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
        return Ok(new { message = "Merchant dashboard - handler implementation pending" });
    }

    [HttpGet("reports")]
    public async Task<IActionResult> GetReports()
    {
        return Ok(new { message = "Merchant reports - handler implementation pending" });
    }

    [HttpGet("offers")]
    public async Task<IActionResult> GetMyOffers()
    {
        return Ok(new { message = "My offers - handler implementation pending" });
    }

    [HttpPost("offers")]
    public async Task<IActionResult> CreateOffer()
    {
        return Ok(new { message = "Create offer - handler implementation pending" });
    }
}
