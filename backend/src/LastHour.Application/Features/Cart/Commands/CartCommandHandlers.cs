using LastHour.Application.Common.Interfaces;
using LastHour.Application.Common.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LastHour.Application.Features.Cart.Commands;

public class AddToCartCommandHandler : IRequestHandler<AddToCartCommand, Result>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;

    public AddToCartCommandHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUser)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<Result> Handle(AddToCartCommand request, CancellationToken ct)
    {
        var userId = _currentUser.UserId;
        if (string.IsNullOrEmpty(userId)) return Result.Failure("Not authenticated", 401);

        var offer = await _unitOfWork.Offers.GetByIdAsync(request.OfferId);
        if (offer is null) return Result.Failure("Offer not found", 404);
        if (offer.IsExpired) return Result.Failure("Offer has expired");
        if (offer.RemainingQuantity < request.Quantity) return Result.Failure("Not enough stock");

        var existing = await _unitOfWork.CartItems.GetAll()
            .FirstOrDefaultAsync(c => c.UserId == userId && c.OfferId == request.OfferId, ct);

        if (existing is not null)
        {
            existing.Quantity += request.Quantity;
            if (existing.Quantity > offer.RemainingQuantity)
                return Result.Failure("Not enough stock");
        }
        else
        {
            await _unitOfWork.CartItems.AddAsync(new Domain.Entities.CartItem
            {
                UserId = userId,
                OfferId = request.OfferId,
                Quantity = request.Quantity
            });
        }

        await _unitOfWork.CompleteAsync(ct);
        return Result.Success();
    }
}

public class UpdateCartItemCommandHandler : IRequestHandler<UpdateCartItemCommand, Result>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;

    public UpdateCartItemCommandHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUser)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<Result> Handle(UpdateCartItemCommand request, CancellationToken ct)
    {
        var userId = _currentUser.UserId;
        if (string.IsNullOrEmpty(userId)) return Result.Failure("Not authenticated", 401);

        var item = await _unitOfWork.CartItems.GetByIdAsync(request.CartItemId);
        if (item is null || item.UserId != userId) return Result.Failure("Cart item not found", 404);

        if (request.Quantity <= 0)
        {
            await _unitOfWork.CartItems.DeleteAsync(item);
        }
        else
        {
            var offer = await _unitOfWork.Offers.GetByIdAsync(item.OfferId);
            if (offer is null) return Result.Failure("Offer not found", 404);
            if (request.Quantity > offer.RemainingQuantity) return Result.Failure("Not enough stock");
            item.Quantity = request.Quantity;
        }

        await _unitOfWork.CompleteAsync(ct);
        return Result.Success();
    }
}

public class RemoveFromCartCommandHandler : IRequestHandler<RemoveFromCartCommand, Result>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;

    public RemoveFromCartCommandHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUser)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<Result> Handle(RemoveFromCartCommand request, CancellationToken ct)
    {
        var userId = _currentUser.UserId;
        if (string.IsNullOrEmpty(userId)) return Result.Failure("Not authenticated", 401);

        var item = await _unitOfWork.CartItems.GetByIdAsync(request.CartItemId);
        if (item is null || item.UserId != userId) return Result.Failure("Cart item not found", 404);

        await _unitOfWork.CartItems.DeleteAsync(item);
        await _unitOfWork.CompleteAsync(ct);
        return Result.Success();
    }
}

public class ClearCartCommandHandler : IRequestHandler<ClearCartCommand, Result>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;

    public ClearCartCommandHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUser)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<Result> Handle(ClearCartCommand request, CancellationToken ct)
    {
        var userId = _currentUser.UserId;
        if (string.IsNullOrEmpty(userId)) return Result.Failure("Not authenticated", 401);

        var items = await _unitOfWork.CartItems.GetAll()
            .Where(c => c.UserId == userId).ToListAsync(ct);

        foreach (var item in items)
            await _unitOfWork.CartItems.DeleteAsync(item);

        await _unitOfWork.CompleteAsync(ct);
        return Result.Success();
    }
}

public class ApplyCouponCommandHandler : IRequestHandler<ApplyCouponCommand, Result<double>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;

    public ApplyCouponCommandHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUser)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<Result<double>> Handle(ApplyCouponCommand request, CancellationToken ct)
    {
        var coupon = await _unitOfWork.Coupons.GetAll()
            .FirstOrDefaultAsync(c => c.Code == request.Code && c.IsActive, ct);

        if (coupon is null) return Result<double>.Failure("Invalid coupon code", 404);
        if (!coupon.IsValid()) return Result<double>.Failure("Coupon has expired or reached usage limit");

        var userId = _currentUser.UserId;
        if (string.IsNullOrEmpty(userId)) return Result<double>.Failure("Not authenticated", 401);

        var cartItems = await _unitOfWork.CartItems.GetAll()
            .Include(c => c.Offer)
            .Where(c => c.UserId == userId).ToListAsync(ct);

        var subtotal = cartItems.Sum(c => c.TotalPrice);
        if (coupon.MinOrderAmount.HasValue && subtotal < coupon.MinOrderAmount.Value)
            return Result<double>.Failure($"Minimum order amount is {coupon.MinOrderAmount.Value:C}");

        var discount = (double)(subtotal * coupon.DiscountPercentage / 100m);
        if (coupon.MaxDiscountAmount.HasValue)
            discount = Math.Min(discount, (double)coupon.MaxDiscountAmount.Value);

        return Result<double>.Success(discount);
    }
}
