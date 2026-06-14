using LastHour.Application.Common.Models;
using LastHour.Application.Features.Auth.DTOs;
using MediatR;

namespace LastHour.Application.Features.Auth.Commands;

public record RegisterCommand(
    string FullName,
    string Email,
    string Password,
    string? Phone,
    string Role = "Customer") : IRequest<Result<AuthResponse>>;

public record LoginCommand(
    string Email,
    string Password) : IRequest<Result<AuthResponse>>;

public record ForgotPasswordCommand(
    string Email) : IRequest<Result>;

public record ResetPasswordCommand(
    string Email,
    string Token,
    string NewPassword) : IRequest<Result>;

public record ChangePasswordCommand(
    string CurrentPassword,
    string NewPassword) : IRequest<Result>;

public record RefreshTokenCommand(string RefreshToken) : IRequest<Result<RefreshResponse>>;

public record LogoutCommand : IRequest<Result>;
