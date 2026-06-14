namespace LastHour.Application.Features.Auth.DTOs;

public record RegisterRequest(
    string FullName,
    string Email,
    string Password,
    string? Phone,
    string Role = "Customer");

public record LoginRequest(string Email, string Password);

public record ForgotPasswordRequest(string Email);

public record ResetPasswordRequest(
    string Email,
    string Token,
    string NewPassword);

public record ChangePasswordRequest(
    string CurrentPassword,
    string NewPassword);

public record RefreshTokenRequest(string RefreshToken);

public record RefreshResponse(
    string Token,
    string RefreshToken,
    DateTime ExpiresAt);

public record AuthResponse(
    string Id,
    string Name,
    string Email,
    string? Phone,
    string? AvatarUrl,
    string Role,
    string AccessToken,
    string RefreshToken,
    DateTime ExpiresAt,
    string? EmailVerifiedAt = null,
    string? PhoneVerifiedAt = null,
    string? CreatedAt = null,
    string? UpdatedAt = null);

public record UserProfileResponse(
    string Id,
    string Name,
    string Email,
    string? Phone,
    string? AvatarUrl,
    string Role,
    string? EmailVerifiedAt,
    string? PhoneVerifiedAt,
    DateTime CreatedAt,
    DateTime? UpdatedAt);
