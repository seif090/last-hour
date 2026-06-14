using LastHour.Application.Common.Interfaces;
using LastHour.Application.Common.Models;
using LastHour.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LastHour.Application.Features.Stores.Commands;

public record ToggleStoreFavoriteCommand(string StoreId) : IRequest<Result>;

public class ToggleStoreFavoriteCommandHandler : IRequestHandler<ToggleStoreFavoriteCommand, Result>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;

    public ToggleStoreFavoriteCommandHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUser)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<Result> Handle(ToggleStoreFavoriteCommand request, CancellationToken ct)
    {
        var userId = _currentUser.UserId;
        if (string.IsNullOrEmpty(userId)) return Result.Failure("Not authenticated", 401);

        var store = await _unitOfWork.Stores.GetByIdAsync(request.StoreId);
        if (store is null) return Result.Failure("Store not found", 404);

        var existing = await _unitOfWork.Favorites.GetAll()
            .FirstOrDefaultAsync(f => f.UserId == userId && f.StoreId == request.StoreId, ct);

        if (existing is not null)
        {
            await _unitOfWork.Favorites.DeleteAsync(existing);
        }
        else
        {
            await _unitOfWork.Favorites.AddAsync(new Favorite
            {
                UserId = userId,
                StoreId = request.StoreId
            });
        }

        await _unitOfWork.CompleteAsync(ct);
        return Result.Success();
    }
}
