using System.Data.Common;
using FluentAssertions;
using LastHour.BuildingBlocks.Application.Contracts;
using LastHour.BuildingBlocks.Infrastructure.Persistence;
using LastHour.BuildingBlocks.Infrastructure.Persistence.Repositories;
using LastHour.BuildingBlocks.Infrastructure.Persistence.UnitOfWork;
using LastHour.BuildingBlocks.SharedKernel.Results;
using LastHour.Modules.Notifications.Application.Commands;
using LastHour.Modules.Notifications.Application.Models;
using LastHour.Modules.Notifications.Application.Persistence;
using LastHour.Modules.Notifications.Domain.Notifications;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace LastHour.Modules.Notifications.Tests.Application.Commands;

public class MarkAllNotificationsAsReadCommandHandlerTests : IDisposable
{
    private readonly DbConnection _connection;
    private readonly DbContextOptions<LastHourDbContext> _dbContextOptions;
    private readonly LastHourDbContext _dbContext;
    private readonly INotificationRepository _notificationRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TestClock _clock;
    private readonly MarkAllNotificationsAsReadCommandHandler _handler;
    private readonly DateTime _baseTime = new(2026, 8, 20, 10, 0, 0, DateTimeKind.Utc);

    public MarkAllNotificationsAsReadCommandHandlerTests()
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

        _handler = new MarkAllNotificationsAsReadCommandHandler(
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
    public async Task Handle_MultipleUnreadNotifications_MarksAllAsReadAndReturnsCount()
    {
        Guid userId = Guid.NewGuid();
        Guid otherUser = Guid.NewGuid();

        Notification unread1 = Notification.Create(null, userId, "user@example.com", "Title 1", "Body", "UserRegistered", _baseTime);
        Notification unread2 = Notification.Create(null, userId, "user@example.com", "Title 2", "Body", "UserRegistered", _baseTime);
        Notification otherUnread = Notification.Create(null, otherUser, "other@example.com", "Other", "Body", "UserRegistered", _baseTime);

        _dbContext.Set<Notification>().AddRange(unread1, unread2, otherUnread);
        await _dbContext.SaveChangesAsync();

        DateTime markTime = _baseTime.AddMinutes(15);
        _clock.UtcNow = markTime;

        MarkAllNotificationsAsReadCommand command = new(userId);
        Result<MarkAllNotificationsAsReadResponse> result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.UpdatedCount.Should().Be(2);

        List<Notification> userNotifications = await _dbContext.Set<Notification>()
            .Where(n => n.RecipientId == userId)
            .ToListAsync();

        userNotifications.Should().OnlyContain(n => n.IsRead && n.ReadAtUtc == markTime);

        Notification otherNotification = await _dbContext.Set<Notification>()
            .SingleAsync(n => n.RecipientId == otherUser);
        otherNotification.IsRead.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_WhenAllAlreadyRead_ReturnsZeroAndIsIdempotent()
    {
        Guid userId = Guid.NewGuid();
        Notification readNotification = Notification.Create(null, userId, "user@example.com", "Title", "Body", "UserRegistered", _baseTime);
        readNotification.MarkAsRead(_baseTime.AddMinutes(5));

        _dbContext.Set<Notification>().Add(readNotification);
        await _dbContext.SaveChangesAsync();

        MarkAllNotificationsAsReadCommand command = new(userId);
        Result<MarkAllNotificationsAsReadResponse> result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.UpdatedCount.Should().Be(0);
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
