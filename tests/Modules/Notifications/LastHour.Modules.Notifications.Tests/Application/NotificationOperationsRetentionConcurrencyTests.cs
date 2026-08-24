using System.Data.Common;
using FluentAssertions;
using LastHour.BuildingBlocks.Infrastructure.Persistence;
using LastHour.BuildingBlocks.Infrastructure.Persistence.Repositories;
using LastHour.BuildingBlocks.Infrastructure.Persistence.UnitOfWork;
using LastHour.Modules.Notifications.Application.Persistence;
using LastHour.Modules.Notifications.Application.Retention;
using LastHour.Modules.Notifications.Domain.Notifications;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace LastHour.Modules.Notifications.Tests.Application;

public sealed class NotificationOperationsRetentionConcurrencyTests : IDisposable
{
    private readonly DbConnection _connection;
    private readonly DbContextOptions<LastHourDbContext> _dbContextOptions;
    private readonly LastHourDbContext _dbContext;
    private readonly NotificationDeliveryAttemptRepository _attemptRepository;
    private readonly NotificationDeliveryAuditRepository _auditRepository;
    private readonly DateTime _baseTime = new(2026, 8, 20, 12, 0, 0, DateTimeKind.Utc);

    public NotificationOperationsRetentionConcurrencyTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        _dbContextOptions = new DbContextOptionsBuilder<LastHourDbContext>()
            .UseSqlite(_connection)
            .Options;

        _dbContext = new LastHourDbContext(_dbContextOptions, new[] { typeof(Notification).Assembly });
        _dbContext.Database.EnsureCreated();

        _attemptRepository = new NotificationDeliveryAttemptRepository(
            new EfRepository<NotificationDeliveryAttempt>(_dbContext),
            new EfReadRepository<NotificationDeliveryAttempt, Guid>(_dbContext));

        _auditRepository = new NotificationDeliveryAuditRepository(
            new EfRepository<NotificationDeliveryAuditEntry>(_dbContext),
            new EfReadRepository<NotificationDeliveryAuditEntry, Guid>(_dbContext));
    }

    public void Dispose()
    {
        _dbContext.Dispose();
        _connection.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task ConcurrentRetentionCleanup_DoesNotCorruptDeliveryStateOrThrow()
    {
        var notification = Notification.Create(Guid.NewGuid(), Guid.NewGuid(), "user@lasthour.local", "Subject", "Body", "UserRegistered", _baseTime);
        _dbContext.Set<Notification>().Add(notification);

        var delivery = NotificationDelivery.Create(notification.Id, DeliveryChannel.Email, _baseTime);
        _dbContext.Set<NotificationDelivery>().Add(delivery);

        var oldAttempt = NotificationDeliveryAttempt.Create(delivery.Id, 1, DeliveryChannel.Email, _baseTime.AddDays(-40));
        await _attemptRepository.AddAsync(oldAttempt);
        await _dbContext.SaveChangesAsync();
        _dbContext.ChangeTracker.Clear();

        var options = Options.Create(new NotificationOperationsRetentionOptions { AttemptRetentionDays = 30 });
        var service1 = new NotificationOperationsRetentionService(_attemptRepository, _auditRepository, new EfUnitOfWork(_dbContext), options, NullLogger<NotificationOperationsRetentionService>.Instance);
        var service2 = new NotificationOperationsRetentionService(_attemptRepository, _auditRepository, new EfUnitOfWork(_dbContext), options, NullLogger<NotificationOperationsRetentionService>.Instance);

        Task<RetentionCleanupResult> task1 = service1.CleanupAsync(_baseTime);
        Task<RetentionCleanupResult> task2 = service2.CleanupAsync(_baseTime);

        RetentionCleanupResult[] results = await Task.WhenAll(task1, task2);

        int totalDeleted = results[0].AttemptsDeleted + results[1].AttemptsDeleted;
        totalDeleted.Should().Be(1);

        NotificationDelivery intactDelivery = await _dbContext.Set<NotificationDelivery>().SingleAsync(d => d.Id == delivery.Id);
        intactDelivery.Should().NotBeNull();
        intactDelivery.Status.Should().Be(DeliveryStatus.Pending);
    }
}
