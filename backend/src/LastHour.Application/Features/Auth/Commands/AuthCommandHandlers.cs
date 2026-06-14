using LastHour.Application.Common.Interfaces;
using LastHour.Application.Common.Models;
using LastHour.Application.Features.Auth.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.IdentityModel.Tokens.Jwt;

namespace LastHour.Application.Features.Auth.Commands;

public class RegisterCommandHandler : IRequestHandler<RegisterCommand, Result<AuthResponse>>
{
    private readonly IIdentityService _identityService;
    private readonly IUnitOfWork _unitOfWork;

    public RegisterCommandHandler(IIdentityService identityService, IUnitOfWork unitOfWork)
    {
        _identityService = identityService;
        _unitOfWork = unitOfWork;
    }

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

        var profile = await _unitOfWork.UserProfiles.GetAll()
            .FirstOrDefaultAsync(p => p.IdentityId == user.Id, cancellationToken);

        return Result<AuthResponse>.Success(new AuthResponse(
            user.Id, user.FullName, user.Email, user.Phone, user.AvatarUrl,
            request.Role, accessToken, refreshToken, jwt.ValidTo,
            profile?.IsEmailVerified == true ? DateTime.UtcNow.ToString("o") : null,
            profile?.IsPhoneVerified == true ? DateTime.UtcNow.ToString("o") : null,
            profile?.CreatedAt.ToString("o")));
    }
}

public class LoginCommandHandler : IRequestHandler<LoginCommand, Result<AuthResponse>>
{
    private readonly IIdentityService _identityService;
    private readonly IUnitOfWork _unitOfWork;

    public LoginCommandHandler(IIdentityService identityService, IUnitOfWork unitOfWork)
    {
        _identityService = identityService;
        _unitOfWork = unitOfWork;
    }

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

        var profile = await _unitOfWork.UserProfiles.GetAll()
            .FirstOrDefaultAsync(p => p.IdentityId == user.Id, cancellationToken);

        return Result<AuthResponse>.Success(new AuthResponse(
            user.Id, user.FullName, user.Email, user.Phone, user.AvatarUrl,
            role, accessToken, refreshToken, jwt.ValidTo,
            profile?.IsEmailVerified == true ? DateTime.UtcNow.ToString("o") : null,
            profile?.IsPhoneVerified == true ? DateTime.UtcNow.ToString("o") : null,
            profile?.CreatedAt.ToString("o")));
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

public class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, Result<RefreshResponse>>
{
    private readonly IIdentityService _identityService;

    public RefreshTokenCommandHandler(IIdentityService identityService)
    {
        _identityService = identityService;
    }

    public async Task<Result<RefreshResponse>> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        var (success, error, accessToken, refreshToken) =
            await _identityService.RefreshTokenAsync(request.RefreshToken);

        if (!success || accessToken is null || refreshToken is null)
            return Result<RefreshResponse>.Failure(error ?? "Token refresh failed", 401);

        var handler = new JwtSecurityTokenHandler();
        var newJwt = handler.ReadJwtToken(accessToken);

        return Result<RefreshResponse>.Success(new RefreshResponse(
            accessToken, refreshToken, newJwt.ValidTo));
    }
}
