using LastHour.Application.Common.Interfaces;
using LastHour.Application.Common.Models;
using LastHour.Application.Features.Cart.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LastHour.Application.Features.Cart.Queries;

public record GetCartQuery : IRequest<Result<List<CartItemResponse>>>;

public class GetCartQueryHandler : IRequestHandler<GetCartQuery, Result<List<CartItemResponse>>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;

    public GetCartQueryHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUser)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<Result<List<CartItemResponse>>> Handle(GetCartQuery request, CancellationToken ct)
    {
        var userId = _currentUser.UserId;
        if (string.IsNullOrEmpty(userId)) return Result<List<CartItemResponse>>.Failure("Not authenticated", 401);

        var items = await _unitOfWork.CartItems.GetAll()
            .Include(c => c.Offer!)
            .ThenInclude(o => o!.Store)
            .Where(c => c.UserId == userId)
            .ToListAsync(ct);

        var response = items.Select(c => new CartItemResponse(
            c.Id, c.OfferId, c.Offer?.Title ?? "", c.Offer?.ImageUrl,
            c.Offer?.StoreId ?? "", c.Offer?.Store?.Name ?? "",
            c.Offer?.DiscountPrice ?? 0, c.Quantity,
            c.Offer?.RemainingQuantity ?? 0, c.Offer?.ExpiryTime ?? DateTime.MinValue,
            c.TotalPrice)).ToList();

        return Result<List<CartItemResponse>>.Success(response);
    }
}
