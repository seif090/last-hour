using LastHour.Application.Common.Interfaces;
using LastHour.Application.Common.Models;
using LastHour.Application.Features.Offers.DTOs;
using MediatR;

namespace LastHour.Application.Features.Offers.Commands;

public record ToggleOfferFavoriteCommand(string OfferId) : IRequest<Result>;

public class ToggleOfferFavoriteCommandHandler : IRequestHandler<ToggleOfferFavoriteCommand, Result>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;

    public ToggleOfferFavoriteCommandHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUser)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<Result> Handle(ToggleOfferFavoriteCommand request, CancellationToken ct)
    {
        var userId = _currentUser.UserId;
        if (string.IsNullOrEmpty(userId)) return Result.Failure("Not authenticated", 401);

        var existing = _unitOfWork.Favorites.GetAll()
            .FirstOrDefault(f => f.UserId == userId && f.OfferId == request.OfferId);

        if (existing is not null)
            await _unitOfWork.Favorites.DeleteAsync(existing);
        else
            await _unitOfWork.Favorites.AddAsync(new Domain.Entities.Favorite { UserId = userId, OfferId = request.OfferId });

        await _unitOfWork.CompleteAsync(ct);
        return Result.Success();
    }
}
