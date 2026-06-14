using LastHour.Application.Common.Models;
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
    private static readonly HttpClient _httpClient = new();

    public AuthController(IMediator mediator) => _mediator = mediator;

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] Dictionary<string, object?> body)
    {
        var fullName = body.GetValueOrDefault("name")?.ToString() ?? body.GetValueOrDefault("full_name")?.ToString() ?? "";
        var email = body.GetValueOrDefault("email")?.ToString() ?? "";
        var password = body.GetValueOrDefault("password")?.ToString() ?? "";
        var phone = body.GetValueOrDefault("phone")?.ToString();
        var role = body.GetValueOrDefault("role")?.ToString() ?? "Customer";

        var result = await _mediator.Send(new RegisterCommand(fullName, email, password, phone, role));
        return result.Succeeded ? Ok(result) : StatusCode(result.StatusCode, result);
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        if (string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.Password))
            return Ok(Result<object>.Failure("Invalid request"));
        var result = await _mediator.Send(new LoginCommand(request.Email, request.Password));
        return result.Succeeded ? Ok(result) : StatusCode(result.StatusCode, result);
    }

    [HttpPost("refresh-token")]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequest request)
    {
        var result = await _mediator.Send(new RefreshTokenCommand(request.RefreshToken));
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
    public async Task<IActionResult> ResetPassword([FromBody] Dictionary<string, object?> body)
    {
        var email = body.GetValueOrDefault("email")?.ToString() ?? "";
        var token = body.GetValueOrDefault("token")?.ToString() ?? body.GetValueOrDefault("otp")?.ToString() ?? "";
        var newPassword = body.GetValueOrDefault("new_password")?.ToString()
            ?? body.GetValueOrDefault("password")?.ToString() ?? "";
        var result = await _mediator.Send(new ResetPasswordCommand(email, token, newPassword));
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

    [HttpPost("verify-otp")]
    public IActionResult VerifyOtp([FromBody] Dictionary<string, string> body)
    {
        return Ok(Result<object>.Success(new { verified = true }));
    }

    [HttpPost("social-login")]
    public IActionResult SocialLogin([FromBody] Dictionary<string, string> body)
    {
        return Ok(Result<object>.Failure("Social login not implemented", 501));
    }
}
