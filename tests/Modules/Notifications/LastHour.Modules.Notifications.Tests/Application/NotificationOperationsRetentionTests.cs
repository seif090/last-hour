using System.Data.Common;
using FluentAssertions;
using LastHour.BuildingBlocks.Infrastructure.Persistence;
using LastHour.BuildingBlocks.Infrastructure.Persistence.Repositories;
using LastHour.BuildingBlocks.Infrastructure.Persistence.UnitOfWork;
using LastHour.Modules.Notifications.Application.Persistence;
using LastHour.Modules.Notifications.Application.Retention;
using LastHour.Modules.Notifications.Domain.Notifications;
using LastHour.Modules.Notifications.Infrastructure.Retention;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace LastHour.Modules.Notifications.Tests.Application;

public sealed class NotificationOperationsRetentionTests : IDisposable
{
    private readonly DbConnection _connection;
    private readonly DbContextOptions<LastHourDbContext> _dbContextOptions;
    private readonly LastHourDbContext _dbContext;
    private readonly NotificationDeliveryAttemptRepository _attemptRepository;
    private readonly NotificationDeliveryAuditRepository _auditRepository;
    private readonly DateTime _baseTime = new(2026, 8, 20, 12, 0, 0, DateTimeKind.Utc);

    public NotificationOperationsRetentionTests()
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

    private async Task<Guid> SeedDeliveryAsync()
    {
        var notification = Notification.Create(Guid.NewGuid(), Guid.NewGuid(), "user@lasthour.local", "Subject", "Body", "UserRegistered", _baseTime);
        _dbContext.Set<Notification>().Add(notification);

        var delivery = NotificationDelivery.Create(notification.Id, DeliveryChannel.Email, _baseTime);
        _dbContext.Set<NotificationDelivery>().Add(delivery);

        await _dbContext.SaveChangesAsync();
        _dbContext.ChangeTracker.Clear();
        return delivery.Id;
    }

    [Fact]
    public void OptionsValidator_RejectsInvalidRanges()
    {
        var validator = new NotificationOperationsRetentionOptionsValidator();

        validator.Validate(null, new NotificationOperationsRetentionOptions { AttemptRetentionDays = 0 }).Failed.Should().BeTrue();
        validator.Validate(null, new NotificationOperationsRetentionOptions { AuditRetentionDays = -1 }).Failed.Should().BeTrue();
        validator.Validate(null, new NotificationOperationsRetentionOptions { CleanupBatchSize = 0 }).Failed.Should().BeTrue();
        validator.Validate(null, new NotificationOperationsRetentionOptions { CleanupBatchSize = 1001 }).Failed.Should().BeTrue();
        validator.Validate(null, new NotificationOperationsRetentionOptions { ExecutionIntervalMinutes = 0 }).Failed.Should().BeTrue();
    }

    [Fact]
    public async Task CleanupAsync_DisabledRetention_DoesNotDeleteAnything()
    {
        Guid deliveryId = await SeedDeliveryAsync();
        var oldAttempt = NotificationDeliveryAttempt.Create(deliveryId, 1, DeliveryChannel.Email, _baseTime.AddDays(-60));
        await _attemptRepository.AddAsync(oldAttempt);
        await _dbContext.SaveChangesAsync();
        _dbContext.ChangeTracker.Clear();

        var options = Options.Create(new NotificationOperationsRetentionOptions { Enabled = false });
        var service = new NotificationOperationsRetentionService(_attemptRepository, _auditRepository, new EfUnitOfWork(_dbContext), options, NullLogger<NotificationOperationsRetentionService>.Instance);

        RetentionCleanupResult result = await service.CleanupAsync(_baseTime);

        result.AttemptsDeleted.Should().Be(0);
        (await _dbContext.Set<NotificationDeliveryAttempt>().CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task CleanupAsync_DeletesOldAttemptsAndPreservesRecentAttempts()
    {
        Guid deliveryId = await SeedDeliveryAsync();
        var oldAttempt = NotificationDeliveryAttempt.Create(deliveryId, 1, DeliveryChannel.Email, _baseTime.AddDays(-31));
        var recentAttempt = NotificationDeliveryAttempt.Create(deliveryId, 2, DeliveryChannel.Email, _baseTime.AddDays(-5));

        await _attemptRepository.AddAsync(oldAttempt);
        await _attemptRepository.AddAsync(recentAttempt);
        await _dbContext.SaveChangesAsync();
        _dbContext.ChangeTracker.Clear();

        var options = Options.Create(new NotificationOperationsRetentionOptions { AttemptRetentionDays = 30, CleanupBatchSize = 100 });
        var service = new NotificationOperationsRetentionService(_attemptRepository, _auditRepository, new EfUnitOfWork(_dbContext), options, NullLogger<NotificationOperationsRetentionService>.Instance);

        RetentionCleanupResult result = await service.CleanupAsync(_baseTime);

        result.AttemptsDeleted.Should().Be(1);

        List<NotificationDeliveryAttempt> remaining = await _dbContext.Set<NotificationDeliveryAttempt>().ToListAsync();
        remaining.Should().ContainSingle();
        remaining[0].Id.Should().Be(recentAttempt.Id);
    }

    [Fact]
    public async Task CleanupAsync_DeletesOldAuditEntriesAndPreservesRecentAuditEntries()
    {
        Guid deliveryId = await SeedDeliveryAsync();
        Guid actorId = Guid.NewGuid();

        var oldAudit = NotificationDeliveryAuditEntry.Create(deliveryId, "Requeued", DeliveryStatus.FailedPermanent, DeliveryStatus.Pending, actorId, "Old", _baseTime.AddDays(-91));
        var recentAudit = NotificationDeliveryAuditEntry.Create(deliveryId, "Requeued", DeliveryStatus.FailedPermanent, DeliveryStatus.Pending, actorId, "Recent", _baseTime.AddDays(-10));

        await _auditRepository.AddAsync(oldAudit);
        await _auditRepository.AddAsync(recentAudit);
        await _dbContext.SaveChangesAsync();
        _dbContext.ChangeTracker.Clear();

        var options = Options.Create(new NotificationOperationsRetentionOptions { AuditRetentionDays = 90, CleanupBatchSize = 100 });
        var service = new NotificationOperationsRetentionService(_attemptRepository, _auditRepository, new EfUnitOfWork(_dbContext), options, NullLogger<NotificationOperationsRetentionService>.Instance);

        RetentionCleanupResult result = await service.CleanupAsync(_baseTime);

        result.AuditEntriesDeleted.Should().Be(1);

        List<NotificationDeliveryAuditEntry> remaining = await _dbContext.Set<NotificationDeliveryAuditEntry>().ToListAsync();
        remaining.Should().ContainSingle();
        remaining[0].Id.Should().Be(recentAudit.Id);
    }

    [Fact]
    public async Task CleanupAsync_NeverDeletesNotificationDeliveryAggregates()
    {
        var notification = Notification.Create(Guid.NewGuid(), Guid.NewGuid(), "user@lasthour.local", "Subject", "Body", "UserRegistered", _baseTime.AddDays(-100));
        _dbContext.Set<Notification>().Add(notification);

        var delivery = NotificationDelivery.Create(notification.Id, DeliveryChannel.Email, _baseTime.AddDays(-100));
        delivery.RecordFailure("Fatal error", _baseTime.AddDays(-100), 1, TimeSpan.FromMinutes(1));
        _dbContext.Set<NotificationDelivery>().Add(delivery);

        var oldAttempt = NotificationDeliveryAttempt.Create(delivery.Id, 1, DeliveryChannel.Email, _baseTime.AddDays(-100));
        await _attemptRepository.AddAsync(oldAttempt);
        await _dbContext.SaveChangesAsync();
        _dbContext.ChangeTracker.Clear();

        var options = Options.Create(new NotificationOperationsRetentionOptions { AttemptRetentionDays = 30 });
        var service = new NotificationOperationsRetentionService(_attemptRepository, _auditRepository, new EfUnitOfWork(_dbContext), options, NullLogger<NotificationOperationsRetentionService>.Instance);

        RetentionCleanupResult result = await service.CleanupAsync(_baseTime);

        result.AttemptsDeleted.Should().Be(1);

        // Verify NotificationDelivery aggregate is STILL intact!
        (await _dbContext.Set<NotificationDelivery>().CountAsync()).Should().Be(1);
        (await _dbContext.Set<Notification>().CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task CleanupAsync_IsIdempotentOnRepeatedRuns()
    {
        Guid deliveryId = await SeedDeliveryAsync();
        var oldAttempt = NotificationDeliveryAttempt.Create(deliveryId, 1, DeliveryChannel.Email, _baseTime.AddDays(-40));
        await _attemptRepository.AddAsync(oldAttempt);
        await _dbContext.SaveChangesAsync();
        _dbContext.ChangeTracker.Clear();

        var options = Options.Create(new NotificationOperationsRetentionOptions { AttemptRetentionDays = 30 });
        var service = new NotificationOperationsRetentionService(_attemptRepository, _auditRepository, new EfUnitOfWork(_dbContext), options, NullLogger<NotificationOperationsRetentionService>.Instance);

        RetentionCleanupResult run1 = await service.CleanupAsync(_baseTime);
        RetentionCleanupResult run2 = await service.CleanupAsync(_baseTime);

        run1.AttemptsDeleted.Should().Be(1);
        run2.AttemptsDeleted.Should().Be(0);
    }
}
