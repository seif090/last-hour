using System.Data.Common;
using FluentAssertions;
using LastHour.BuildingBlocks.Application.Contracts;
using LastHour.BuildingBlocks.Infrastructure.Persistence;
using LastHour.BuildingBlocks.Infrastructure.Persistence.Repositories;
using LastHour.BuildingBlocks.Infrastructure.Persistence.UnitOfWork;
using LastHour.BuildingBlocks.SharedKernel.Results;
using LastHour.Modules.Notifications.Application.Commands;
using LastHour.Modules.Notifications.Application.Persistence;
using LastHour.Modules.Notifications.Domain.Notifications;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace LastHour.Modules.Notifications.Tests.Application.Commands;

public class MarkNotificationAsReadCommandHandlerTests : IDisposable
{
    private readonly DbConnection _connection;
    private readonly DbContextOptions<LastHourDbContext> _dbContextOptions;
    private readonly LastHourDbContext _dbContext;
    private readonly INotificationRepository _notificationRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TestClock _clock;
    private readonly MarkNotificationAsReadCommandHandler _handler;
    private readonly DateTime _baseTime = new(2026, 8, 20, 10, 0, 0, DateTimeKind.Utc);

    public MarkNotificationAsReadCommandHandlerTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        _dbContextOptions = new DbContextOptionsBuilder<LastHourDbContext>()
            .UseSqlite(_connection)
            .Options;

        _dbContext = new LastHourDbContext(_dbContextOptions, new[] { typeof(Notification).Assembly });
        _dbContext.Database.EnsureCreated();

        _notificationRepository = new NotificationRepository(
            new EfRepository<Notification>(_dbContext),
            new EfReadRepository<Notification, Guid>(_dbContext),
            new EfTrackedRepository<Notification, Guid>(_dbContext));

        _unitOfWork = new EfUnitOfWork(_dbContext);
        _clock = new TestClock(_baseTime);

        _handler = new MarkNotificationAsReadCommandHandler(
            _notificationRepository,
            _unitOfWork,
            _clock);
    }

    public void Dispose()
    {
        _dbContext.Dispose();
        _connection.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task Handle_UnreadNotification_MarksAsReadWithClockTimestamp()
    {
        Guid userId = Guid.NewGuid();
        Notification notification = Notification.Create(
            null,
            userId,
            "user@example.com",
            "Welcome",
            "Welcome body",
            "UserRegistered",
            _baseTime.AddHours(-1));

        _dbContext.Set<Notification>().Add(notification);
        await _dbContext.SaveChangesAsync();

        DateTime markTime = _baseTime.AddMinutes(10);
        _clock.UtcNow = markTime;

        MarkNotificationAsReadCommand command = new(notification.Id, userId);
        Result result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();

        Notification updated = await _dbContext.Set<Notification>().SingleAsync(n => n.Id == notification.Id);
        updated.IsRead.Should().BeTrue();
        updated.ReadAtUtc.Should().Be(markTime);
    }

    [Fact]
    public async Task Handle_AlreadyReadNotification_IsIdempotentAndDoesNotChangeReadAtUtc()
    {
        Guid userId = Guid.NewGuid();
        Notification notification = Notification.Create(
            null,
            userId,
            "user@example.com",
            "Welcome",
            "Welcome body",
            "UserRegistered",
            _baseTime.AddHours(-1));

        DateTime originalReadTime = _baseTime.AddMinutes(5);
        notification.MarkAsRead(originalReadTime);

        _dbContext.Set<Notification>().Add(notification);
        await _dbContext.SaveChangesAsync();

        _clock.UtcNow = _baseTime.AddHours(2);

        MarkNotificationAsReadCommand command = new(notification.Id, userId);
        Result result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();

        Notification updated = await _dbContext.Set<Notification>().SingleAsync(n => n.Id == notification.Id);
        updated.IsRead.Should().BeTrue();
        updated.ReadAtUtc.Should().Be(originalReadTime);
    }

    [Fact]
    public async Task Handle_NonExistentNotification_ReturnsNotFound()
    {
        Guid userId = Guid.NewGuid();
        MarkNotificationAsReadCommand command = new(Guid.NewGuid(), userId);

        Result result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.ErrorType.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task Handle_NotificationBelongingToAnotherUser_ReturnsNotFoundToPreventLeakage()
    {
        Guid userA = Guid.NewGuid();
        Guid userB = Guid.NewGuid();

        Notification notification = Notification.Create(
            null,
            userA,
            "userA@example.com",
            "Welcome",
            "Welcome body",
            "UserRegistered",
            _baseTime);

        _dbContext.Set<Notification>().Add(notification);
        await _dbContext.SaveChangesAsync();

        // User B tries to mark User A's notification as read
        MarkNotificationAsReadCommand command = new(notification.Id, userB);
        Result result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.ErrorType.Should().Be(ErrorType.NotFound);

        Notification unchanged = await _dbContext.Set<Notification>().SingleAsync(n => n.Id == notification.Id);
        unchanged.IsRead.Should().BeFalse();
    }

    private sealed class TestClock : IClock
    {
        public TestClock(DateTime utcNow)
        {
            UtcNow = utcNow;
        }

        public DateTime UtcNow { get; set; }
    }
}
