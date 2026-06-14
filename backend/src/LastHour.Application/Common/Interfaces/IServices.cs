using LastHour.Application.Common.Models;
using LastHour.Domain.Common;

namespace LastHour.Application.Common.Interfaces;

public interface IIdentityService
{
    Task<(bool Success, string? Error, AuthUserResult? User)> RegisterAsync(
        string email, string password, string fullName, string? phone, string role);
    Task<(bool Success, string? Error, AuthUserResult? User)> LoginAsync(
        string email, string password);
    Task<(bool Success, string? Error)> ChangePasswordAsync(string userId, string currentPassword, string newPassword);
    Task<(bool Success, string? Error)> ForgotPasswordAsync(string email);
    Task<(bool Success, string? Error)> ResetPasswordAsync(string email, string token, string newPassword);
    Task<(bool Success, string? Error, string? AccessToken, string? RefreshToken)> GenerateTokensAsync(
        string userId, string email, string role);
    Task<(bool Success, string? Error, string? AccessToken, string? RefreshToken)> RefreshTokenAsync(
        string refreshToken);
    Task<(bool Success, string? Error)> RevokeRefreshTokenAsync(string userId);
}

public record AuthUserResult(string Id, string FullName, string Email, string? Phone, string? AvatarUrl);

public interface IUnitOfWork : IDisposable
{
    IRepository<Domain.Entities.Store> Stores { get; }
    IRepository<Domain.Entities.Offer> Offers { get; }
    IRepository<Domain.Entities.Order> Orders { get; }
    IRepository<Domain.Entities.OrderItem> OrderItems { get; }
    IRepository<Domain.Entities.CartItem> CartItems { get; }
    IRepository<Domain.Entities.Category> Categories { get; }
    IRepository<Domain.Entities.Coupon> Coupons { get; }
    IRepository<Domain.Entities.Review> Reviews { get; }
    IRepository<Domain.Entities.Address> Addresses { get; }
    IRepository<Domain.Entities.Favorite> Favorites { get; }
    IRepository<Domain.Entities.Notification> Notifications { get; }
    IRepository<Domain.Entities.PaymentTransaction> PaymentTransactions { get; }
    IRepository<Domain.Entities.UserProfile> UserProfiles { get; }
    Task<int> CompleteAsync(CancellationToken cancellationToken = default);
}

public interface IRepository<T> where T : BaseEntity
{
    IQueryable<T> GetAll();
    Task<T?> GetByIdAsync(string id);
    Task<T> AddAsync(T entity);
    Task UpdateAsync(T entity);
    Task DeleteAsync(T entity);
    Task<bool> ExistsAsync(string id);
}
