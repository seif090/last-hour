using LastHour.Application.Common.Interfaces;
using LastHour.Application.Common.Models;
using LastHour.Application.Features.Auth.DTOs;
using MediatR;
using System.IdentityModel.Tokens.Jwt;

namespace LastHour.Application.Features.Auth.Commands;

public class RegisterCommandHandler : IRequestHandler<RegisterCommand, Result<AuthResponse>>
{
    private readonly IIdentityService _identityService;

    public RegisterCommandHandler(IIdentityService identityService) => _identityService = identityService;

    public async Task<Result<AuthResponse>> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        var (success, error, user) = await _identityService.RegisterAsync(
            request.Email, request.Password, request.FullName, request.Phone, request.Role);

        if (!success || user is null)
            return Result<AuthResponse>.Failure(error ?? "Registration failed");

        var (tokenSuccess, tokenError, accessToken, refreshToken) =
            await _identityService.GenerateTokensAsync(user.Id, user.Email, request.Role);

        if (!tokenSuccess || accessToken is null || refreshToken is null)
            return Result<AuthResponse>.Failure(tokenError ?? "Token generation failed");

        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(accessToken);

        return Result<AuthResponse>.Success(new AuthResponse(
            user.Id, user.FullName, user.Email, user.Phone, user.AvatarUrl,
            request.Role, accessToken, refreshToken, jwt.ValidTo));
    }
}

public class LoginCommandHandler : IRequestHandler<LoginCommand, Result<AuthResponse>>
{
    private readonly IIdentityService _identityService;

    public LoginCommandHandler(IIdentityService identityService) => _identityService = identityService;

    public async Task<Result<AuthResponse>> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var (success, error, user) = await _identityService.LoginAsync(request.Email, request.Password);
        if (!success || user is null)
            return Result<AuthResponse>.Failure(error ?? "Login failed");

        var role = "Customer";
        var (tokenSuccess, tokenError, accessToken, refreshToken) =
            await _identityService.GenerateTokensAsync(user.Id, user.Email, role);

        if (!tokenSuccess || accessToken is null || refreshToken is null)
            return Result<AuthResponse>.Failure(tokenError ?? "Token generation failed");

        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(accessToken);

        return Result<AuthResponse>.Success(new AuthResponse(
            user.Id, user.FullName, user.Email, user.Phone, user.AvatarUrl,
            role, accessToken, refreshToken, jwt.ValidTo));
    }
}

public class ChangePasswordCommandHandler : IRequestHandler<ChangePasswordCommand, Result>
{
    private readonly IIdentityService _identityService;
    private readonly ICurrentUserService _currentUser;

    public ChangePasswordCommandHandler(IIdentityService identityService, ICurrentUserService currentUser)
    {
        _identityService = identityService;
        _currentUser = currentUser;
    }

    public async Task<Result> Handle(ChangePasswordCommand request, CancellationToken ct)
    {
        var userId = _currentUser.UserId;
        if (string.IsNullOrEmpty(userId)) return Result.Failure("Not authenticated", 401);

        var (success, error) = await _identityService.ChangePasswordAsync(
            userId, request.CurrentPassword, request.NewPassword);
        return success ? Result.Success() : Result.Failure(error ?? "Failed to change password");
    }
}

public class ForgotPasswordCommandHandler : IRequestHandler<ForgotPasswordCommand, Result>
{
    private readonly IIdentityService _identityService;

    public ForgotPasswordCommandHandler(IIdentityService identityService) => _identityService = identityService;

    public async Task<Result> Handle(ForgotPasswordCommand request, CancellationToken ct)
    {
        var (success, error) = await _identityService.ForgotPasswordAsync(request.Email);
        return Result.Success();
    }
}

public class ResetPasswordCommandHandler : IRequestHandler<ResetPasswordCommand, Result>
{
    private readonly IIdentityService _identityService;

    public ResetPasswordCommandHandler(IIdentityService identityService) => _identityService = identityService;

    public async Task<Result> Handle(ResetPasswordCommand request, CancellationToken ct)
    {
        var (success, error) = await _identityService.ResetPasswordAsync(
            request.Email, request.Token, request.NewPassword);
        return success ? Result.Success() : Result.Failure(error ?? "Failed to reset password");
    }
}

public class LogoutCommandHandler : IRequestHandler<LogoutCommand, Result>
{
    private readonly IIdentityService _identityService;
    private readonly ICurrentUserService _currentUser;

    public LogoutCommandHandler(IIdentityService identityService, ICurrentUserService currentUser)
    {
        _identityService = identityService;
        _currentUser = currentUser;
    }

    public async Task<Result> Handle(LogoutCommand request, CancellationToken ct)
    {
        var userId = _currentUser.UserId;
        if (string.IsNullOrEmpty(userId)) return Result.Failure("Not authenticated", 401);

        var (success, error) = await _identityService.RevokeRefreshTokenAsync(userId);
        return success ? Result.Success() : Result.Failure(error ?? "Failed to logout");
    }
}

public class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, Result<AuthResponse>>
{
    private readonly IIdentityService _identityService;

    public RefreshTokenCommandHandler(IIdentityService identityService) => _identityService = identityService;

    public async Task<Result<AuthResponse>> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(request.AccessToken);
        var jti = jwt.Claims.First(c => c.Type == "jti").Value;

        var (success, error, accessToken, refreshToken) =
            await _identityService.RefreshTokenAsync(request.RefreshToken, jti);

        if (!success || accessToken is null || refreshToken is null)
            return Result<AuthResponse>.Failure(error ?? "Token refresh failed", 401);

        var newJwt = handler.ReadJwtToken(accessToken);
        var userId = newJwt.Claims.First(c => c.Type == "nameid" || c.Type == "sub").Value;
        var email = newJwt.Claims.First(c => c.Type == "email").Value;
        var role = newJwt.Claims.First(c => c.Type == "role").Value;

        return Result<AuthResponse>.Success(new AuthResponse(
            userId, "", email, null, null, role, accessToken, refreshToken, newJwt.ValidTo));
    }
}
