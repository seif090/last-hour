using LastHour.Application.Common.Models;
using LastHour.Application.Features.Auth.DTOs;
using MediatR;

namespace LastHour.Application.Features.Auth.Queries;

public record GetCurrentUserQuery : IRequest<Result<UserProfileResponse>>;
