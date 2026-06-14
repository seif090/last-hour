using LastHour.Application.Features.Auth.Commands;
using LastHour.Application.Features.Auth.DTOs;
using LastHour.Application.Features.Auth.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LastHour.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IMediator _mediator;

    public AuthController(IMediator mediator) => _mediator = mediator;

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        var result = await _mediator.Send(new RegisterCommand(
            request.FullName, request.Email, request.Password,
            request.Phone, request.Role));
        return result.Succeeded ? Ok(result) : StatusCode(result.StatusCode, result);
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var result = await _mediator.Send(new LoginCommand(request.Email, request.Password));
        return result.Succeeded ? Ok(result) : StatusCode(result.StatusCode, result);
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequest request)
    {
        var result = await _mediator.Send(new RefreshTokenCommand(request.AccessToken, request.RefreshToken));
        return result.Succeeded ? Ok(result) : StatusCode(result.StatusCode, result);
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        var result = await _mediator.Send(new LogoutCommand());
        return Ok(result);
    }

    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
    {
        var result = await _mediator.Send(new ForgotPasswordCommand(request.Email));
        return Ok(result);
    }

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
    {
        var result = await _mediator.Send(new ResetPasswordCommand(
            request.Email, request.Token, request.NewPassword));
        return result.Succeeded ? Ok(result) : StatusCode(result.StatusCode, result);
    }

    [Authorize]
    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        var result = await _mediator.Send(new ChangePasswordCommand(
            request.CurrentPassword, request.NewPassword));
        return result.Succeeded ? Ok(result) : StatusCode(result.StatusCode, result);
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> GetCurrentUser()
    {
        var result = await _mediator.Send(new GetCurrentUserQuery());
        return result.Succeeded ? Ok(result) : StatusCode(result.StatusCode, result);
    }
}
