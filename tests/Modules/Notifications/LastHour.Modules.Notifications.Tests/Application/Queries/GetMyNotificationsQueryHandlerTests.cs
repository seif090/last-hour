using System.Data.Common;
using FluentAssertions;
using LastHour.BuildingBlocks.Application.Models;
using LastHour.BuildingBlocks.Infrastructure.Persistence;
using LastHour.BuildingBlocks.Infrastructure.Persistence.Repositories;
using LastHour.BuildingBlocks.SharedKernel.Results;
using LastHour.Modules.Notifications.Application.Models;
using LastHour.Modules.Notifications.Application.Persistence;
using LastHour.Modules.Notifications.Application.Queries;
using LastHour.Modules.Notifications.Domain.Notifications;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace LastHour.Modules.Notifications.Tests.Application.Queries;

public class GetMyNotificationsQueryHandlerTests : IDisposable
{
    private readonly DbConnection _connection;
    private readonly DbContextOptions<LastHourDbContext> _dbContextOptions;
    private readonly LastHourDbContext _dbContext;
    private readonly INotificationRepository _notificationRepository;
    private readonly GetMyNotificationsQueryHandler _handler;
    private readonly DateTime _baseTime = new(2026, 8, 20, 10, 0, 0, DateTimeKind.Utc);

    public GetMyNotificationsQueryHandlerTests()
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

        _handler = new GetMyNotificationsQueryHandler(_notificationRepository);
    }

    public void Dispose()
    {
        _dbContext.Dispose();
        _connection.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task Handle_AuthenticatedUser_RetrievesOwnNotificationsOnly()
    {
        Guid userA = Guid.NewGuid();
        Guid userB = Guid.NewGuid();

        Notification notifA = Notification.Create(
            null,
            userA,
            "userA@example.com",
            "Title A",
            "Body A",
            "UserRegistered",
            _baseTime);

        Notification notifB = Notification.Create(
            null,
            userB,
            "userB@example.com",
            "Title B",
            "Body B",
            "UserRegistered",
            _baseTime);

        _dbContext.Set<Notification>().AddRange(notifA, notifB);
        await _dbContext.SaveChangesAsync();

        GetMyNotificationsQuery query = new(userA);
        Result<PagedResult<NotificationDto>> result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().ContainSingle();
        result.Value.Items[0].Id.Should().Be(notifA.Id);
        result.Value.Items[0].Title.Should().Be("Title A");
        result.Value.TotalCount.Should().Be(1);
    }

    [Fact]
    public async Task Handle_Ordering_ReturnsNewestFirst()
    {
        Guid userId = Guid.NewGuid();

        Notification notifOld = Notification.Create(
            null,
            userId,
            "user@example.com",
            "Old",
            "Old body",
            "UserRegistered",
            _baseTime.AddHours(-2));

        Notification notifNew = Notification.Create(
            null,
            userId,
            "user@example.com",
            "New",
            "New body",
            "UserRegistered",
            _baseTime);

        _dbContext.Set<Notification>().AddRange(notifOld, notifNew);
        await _dbContext.SaveChangesAsync();

        GetMyNotificationsQuery query = new(userId);
        Result<PagedResult<NotificationDto>> result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(2);
        result.Value.Items[0].Id.Should().Be(notifNew.Id);
        result.Value.Items[1].Id.Should().Be(notifOld.Id);
    }

    [Fact]
    public async Task Handle_Pagination_CorrectlyPagesResults()
    {
        Guid userId = Guid.NewGuid();

        for (int i = 0; i < 5; i++)
        {
            Notification n = Notification.Create(
                null,
                userId,
                "user@example.com",
                $"Title {i}",
                $"Body {i}",
                "UserRegistered",
                _baseTime.AddMinutes(i));
            _dbContext.Set<Notification>().Add(n);
        }

        await _dbContext.SaveChangesAsync();

        GetMyNotificationsQuery page1Query = new(userId, pageNumber: 1, pageSize: 2);
        Result<PagedResult<NotificationDto>> page1Result = await _handler.Handle(page1Query, CancellationToken.None);

        page1Result.IsSuccess.Should().BeTrue();
        page1Result.Value.Items.Should().HaveCount(2);
        page1Result.Value.TotalCount.Should().Be(5);
        page1Result.Value.TotalPages.Should().Be(3);

        GetMyNotificationsQuery page3Query = new(userId, pageNumber: 3, pageSize: 2);
        Result<PagedResult<NotificationDto>> page3Result = await _handler.Handle(page3Query, CancellationToken.None);

        page3Result.IsSuccess.Should().BeTrue();
        page3Result.Value.Items.Should().HaveCount(1);
    }

    [Fact]
    public async Task Handle_FilterByUnread_ReturnsOnlyUnreadNotifications()
    {
        Guid userId = Guid.NewGuid();

        Notification unread = Notification.Create(
            null,
            userId,
            "user@example.com",
            "Unread",
            "Body",
            "UserRegistered",
            _baseTime);

        Notification read = Notification.Create(
            null,
            userId,
            "user@example.com",
            "Read",
            "Body",
            "UserRegistered",
            _baseTime.AddHours(-1));
        read.MarkAsRead(_baseTime);

        _dbContext.Set<Notification>().AddRange(unread, read);
        await _dbContext.SaveChangesAsync();

        GetMyNotificationsQuery query = new(userId, isRead: false);
        Result<PagedResult<NotificationDto>> result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().ContainSingle();
        result.Value.Items[0].Id.Should().Be(unread.Id);
        result.Value.Items[0].IsRead.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_FilterByType_ReturnsOnlyMatchingType()
    {
        Guid userId = Guid.NewGuid();

        Notification registered = Notification.Create(
            null,
            userId,
            "user@example.com",
            "Registered",
            "Body",
            "UserRegistered",
            _baseTime);

        Notification lockedOut = Notification.Create(
            null,
            userId,
            "user@example.com",
            "Locked",
            "Body",
            "UserLockedOut",
            _baseTime);

        _dbContext.Set<Notification>().AddRange(registered, lockedOut);
        await _dbContext.SaveChangesAsync();

        GetMyNotificationsQuery query = new(userId, type: "UserLockedOut");
        Result<PagedResult<NotificationDto>> result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().ContainSingle();
        result.Value.Items[0].Id.Should().Be(lockedOut.Id);
        result.Value.Items[0].Type.Should().Be("UserLockedOut");
    }

    [Fact]
    public async Task Handle_EmptyResults_ReturnsEmptyPageWithZeroCount()
    {
        Guid userId = Guid.NewGuid();

        GetMyNotificationsQuery query = new(userId);
        Result<PagedResult<NotificationDto>> result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().BeEmpty();
        result.Value.TotalCount.Should().Be(0);
        result.Value.TotalPages.Should().Be(0);
    }

    [Fact]
    public async Task Handle_EmptyUserId_ReturnsUnauthorizedFailure()
    {
        GetMyNotificationsQuery query = new(Guid.Empty);
        Result<PagedResult<NotificationDto>> result = await _handler.Handle(query, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.ErrorType.Should().Be(ErrorType.Unauthorized);
    }
}
