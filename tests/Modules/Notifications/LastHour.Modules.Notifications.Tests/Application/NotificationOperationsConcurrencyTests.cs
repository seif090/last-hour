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

namespace LastHour.Modules.Notifications.Tests.Application;

public sealed class NotificationOperationsConcurrencyTests : IDisposable
{
    private readonly DbConnection _connection;
    private readonly DbContextOptions<LastHourDbContext> _dbContextOptions;
    private readonly LastHourDbContext _dbContext;
    private readonly INotificationDeliveryRepository _deliveryRepository;
    private readonly INotificationDeliveryAuditRepository _auditRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TestClock _clock;
    private readonly DateTime _baseTime = new(2026, 8, 20, 12, 0, 0, DateTimeKind.Utc);

    public NotificationOperationsConcurrencyTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        _dbContextOptions = new DbContextOptionsBuilder<LastHourDbContext>()
            .UseSqlite(_connection)
            .Options;

        _dbContext = new LastHourDbContext(_dbContextOptions, new[] { typeof(Notification).Assembly });
        _dbContext.Database.EnsureCreated();

        _deliveryRepository = new NotificationDeliveryRepository(
            new EfRepository<NotificationDelivery>(_dbContext),
            new EfReadRepository<NotificationDelivery, Guid>(_dbContext),
            new EfTrackedRepository<NotificationDelivery, Guid>(_dbContext));

        _auditRepository = new NotificationDeliveryAuditRepository(
            new EfRepository<NotificationDeliveryAuditEntry>(_dbContext),
            new EfReadRepository<NotificationDeliveryAuditEntry, Guid>(_dbContext));

        _unitOfWork = new EfUnitOfWork(_dbContext);
        _clock = new TestClock(_baseTime);
    }

    public void Dispose()
    {
        _dbContext.Dispose();
        _connection.Dispose();
        GC.SuppressFinalize(this);
    }

    private async Task<NotificationDelivery> SeedDeliveryAsync(DeliveryStatus status = DeliveryStatus.FailedPermanent)
    {
        var notification = Notification.Create(Guid.NewGuid(), Guid.NewGuid(), "user@lasthour.local", "Title", "Body", "UserRegistered", _baseTime);
        _dbContext.Set<Notification>().Add(notification);

        var delivery = NotificationDelivery.Create(notification.Id, DeliveryChannel.Email, _baseTime);
        if (status == DeliveryStatus.FailedPermanent)
        {
            delivery.RecordFailure("Fatal error", _baseTime, 1, TimeSpan.FromMinutes(1));
        }

        _dbContext.Set<NotificationDelivery>().Add(delivery);
        await _dbContext.SaveChangesAsync();
        return delivery;
    }

    [Fact]
    public async Task SingleRequeue_ConcurrentClaimTokenModification_ThrowsConcurrencyAndReturnsConflictResult()
    {
        var delivery = await SeedDeliveryAsync();
        Guid adminId = Guid.NewGuid();

        // Simulate concurrent modification by creating a separate DbContext updating the same delivery
        using var concurrentContext = new LastHourDbContext(_dbContextOptions, new[] { typeof(Notification).Assembly });
        var concurrentDelivery = await concurrentContext.Set<NotificationDelivery>().SingleAsync(d => d.Id == delivery.Id);
        concurrentDelivery.Claim(Guid.NewGuid(), _baseTime.AddMinutes(5), _baseTime);
        await concurrentContext.SaveChangesAsync();

        var handler = new RequeueNotificationDeliveryCommandHandler(_deliveryRepository, _auditRepository, _unitOfWork, _clock);
        var command = new RequeueNotificationDeliveryCommand(delivery.Id, adminId, "Race condition requeue");

        Result result = await handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.FirstError!.Code.Should().Be("ConcurrencyConflict");
    }

    [Fact]
    public async Task BatchRequeue_ConcurrentClaimTokenModificationOnOneDelivery_SkipsConflictedItemAndSucceedsRemaining()
    {
        var d1 = await SeedDeliveryAsync(); // Concurrency conflict
        var d2 = await SeedDeliveryAsync(); // Normal success

        Guid adminId = Guid.NewGuid();

        // Simulate concurrent modification on d1 only
        using (var concurrentContext = new LastHourDbContext(_dbContextOptions, new[] { typeof(Notification).Assembly }))
        {
            var concurrentD1 = await concurrentContext.Set<NotificationDelivery>().SingleAsync(d => d.Id == d1.Id);
            concurrentD1.Claim(Guid.NewGuid(), _baseTime.AddMinutes(5), _baseTime);
            await concurrentContext.SaveChangesAsync();
        }

        var handler = new RequeueNotificationDeliveriesCommandHandler(_deliveryRepository, _auditRepository, _unitOfWork, _clock);
        var command = new RequeueNotificationDeliveriesCommand(new[] { d1.Id, d2.Id }, adminId, "Batch requeue with conflict");

        Result<BatchRequeueResponseDto> result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.RequestedCount.Should().Be(2);
        result.Value.RequeuedCount.Should().Be(1);
        result.Value.RejectedCount.Should().Be(1);

        result.Value.Results.First(r => r.DeliveryId == d1.Id).Requeued.Should().BeFalse();
        result.Value.Results.First(r => r.DeliveryId == d2.Id).Requeued.Should().BeTrue();
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
