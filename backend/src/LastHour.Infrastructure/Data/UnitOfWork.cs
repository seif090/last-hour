using LastHour.Application.Common.Interfaces;
using LastHour.Domain.Common;
using LastHour.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace LastHour.Infrastructure.Data;

public class UnitOfWork : IUnitOfWork
{
    private readonly ApplicationDbContext _context;
    private IRepository<Store>? _stores;
    private IRepository<Offer>? _offers;
    private IRepository<Order>? _orders;
    private IRepository<OrderItem>? _orderItems;
    private IRepository<CartItem>? _cartItems;
    private IRepository<Category>? _categories;
    private IRepository<Coupon>? _coupons;
    private IRepository<Review>? _reviews;
    private IRepository<Address>? _addresses;
    private IRepository<Favorite>? _favorites;
    private IRepository<Notification>? _notifications;
    private IRepository<PaymentTransaction>? _paymentTransactions;
    private IRepository<UserProfile>? _userProfiles;

    public UnitOfWork(ApplicationDbContext context) => _context = context;

    public IRepository<Store> Stores => _stores ??= new Repository<Store>(_context);
    public IRepository<Offer> Offers => _offers ??= new Repository<Offer>(_context);
    public IRepository<Order> Orders => _orders ??= new Repository<Order>(_context);
    public IRepository<OrderItem> OrderItems => _orderItems ??= new Repository<OrderItem>(_context);
    public IRepository<CartItem> CartItems => _cartItems ??= new Repository<CartItem>(_context);
    public IRepository<Category> Categories => _categories ??= new Repository<Category>(_context);
    public IRepository<Coupon> Coupons => _coupons ??= new Repository<Coupon>(_context);
    public IRepository<Review> Reviews => _reviews ??= new Repository<Review>(_context);
    public IRepository<Address> Addresses => _addresses ??= new Repository<Address>(_context);
    public IRepository<Favorite> Favorites => _favorites ??= new Repository<Favorite>(_context);
    public IRepository<Notification> Notifications => _notifications ??= new Repository<Notification>(_context);
    public IRepository<PaymentTransaction> PaymentTransactions => _paymentTransactions ??= new Repository<PaymentTransaction>(_context);
    public IRepository<UserProfile> UserProfiles => _userProfiles ??= new Repository<UserProfile>(_context);

    public async Task<int> CompleteAsync(CancellationToken cancellationToken = default) =>
        await _context.SaveChangesAsync(cancellationToken);

    public void Dispose() => _context.Dispose();
}

public class Repository<T> : IRepository<T> where T : BaseEntity
{
    protected readonly ApplicationDbContext _context;
    public Repository(ApplicationDbContext context) => _context = context;

    public IQueryable<T> GetAll() => _context.Set<T>();
    public async Task<T?> GetByIdAsync(string id) => await _context.Set<T>().FindAsync(id);
    public async Task<T> AddAsync(T entity) { await _context.Set<T>().AddAsync(entity); return entity; }
    public Task UpdateAsync(T entity) { _context.Set<T>().Update(entity); return Task.CompletedTask; }
    public Task DeleteAsync(T entity) { _context.Set<T>().Remove(entity); return Task.CompletedTask; }
    public async Task<bool> ExistsAsync(string id) => await _context.Set<T>().FindAsync(id) is not null;
}
