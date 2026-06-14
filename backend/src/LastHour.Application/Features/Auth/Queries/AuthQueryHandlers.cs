using LastHour.Application.Common.Interfaces;
using LastHour.Application.Common.Models;
using LastHour.Application.Features.Auth.DTOs;
using LastHour.Application.Features.Auth.Queries;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LastHour.Application.Features.Auth.Queries;

public class GetCurrentUserQueryHandler : IRequestHandler<GetCurrentUserQuery, Result<UserProfileResponse>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;

    public GetCurrentUserQueryHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUser)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<Result<UserProfileResponse>> Handle(
        GetCurrentUserQuery request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(_currentUser.UserId))
            return Result<UserProfileResponse>.Failure("User not authenticated", 401);

        var profile = await _unitOfWork.UserProfiles
            .GetAll()
            .FirstOrDefaultAsync(up => up.IdentityId == _currentUser.UserId, cancellationToken);

        if (profile is null)
            return Result<UserProfileResponse>.Failure("User profile not found", 404);

        return Result<UserProfileResponse>.Success(new UserProfileResponse(
            profile.Id, profile.FullName, profile.Email,
            profile.Phone, profile.AvatarUrl,
            profile.IsEmailVerified, profile.IsPhoneVerified,
            profile.CreatedAt));
    }
}
