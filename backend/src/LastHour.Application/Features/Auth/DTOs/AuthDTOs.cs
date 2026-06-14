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

public record RefreshTokenRequest(
    string AccessToken,
    string RefreshToken);

public record AuthResponse(
    string UserId,
    string FullName,
    string Email,
    string? Phone,
    string? AvatarUrl,
    string Role,
    string AccessToken,
    string RefreshToken,
    DateTime ExpiresAt);

public record UserProfileResponse(
    string Id,
    string FullName,
    string Email,
    string? Phone,
    string? AvatarUrl,
    bool IsEmailVerified,
    bool IsPhoneVerified,
    DateTime CreatedAt);
