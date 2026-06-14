using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LastHour.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class AdminController : ControllerBase
{
    private readonly IMediator _mediator;

    public AdminController(IMediator mediator) => _mediator = mediator;

    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboard()
    {
        return Ok(new { message = "Admin dashboard - handler implementation pending" });
    }

    [HttpGet("users")]
    public async Task<IActionResult> GetUsers()
    {
        return Ok(new { message = "User management - handler implementation pending" });
    }

    [HttpGet("stores")]
    public async Task<IActionResult> GetStores()
    {
        return Ok(new { message = "Store management - handler implementation pending" });
    }

    [HttpPut("stores/{id}/verify")]
    public async Task<IActionResult> VerifyStore(string id)
    {
        return Ok(new { message = "Store verification - handler implementation pending" });
    }
}
