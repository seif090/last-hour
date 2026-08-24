using System.Data.Common;
using FluentAssertions;
using LastHour.BuildingBlocks.Application.Contracts;
using LastHour.BuildingBlocks.Application.Models;
using LastHour.BuildingBlocks.Infrastructure.Persistence;
using LastHour.BuildingBlocks.Infrastructure.Persistence.Repositories;
using LastHour.BuildingBlocks.Infrastructure.Persistence.UnitOfWork;
using LastHour.BuildingBlocks.SharedKernel.Results;
using LastHour.Modules.Notifications.Application.Commands;
using LastHour.Modules.Notifications.Application.Models;
using LastHour.Modules.Notifications.Application.Persistence;
using LastHour.Modules.Notifications.Application.Queries;
using LastHour.Modules.Notifications.Domain.Notifications;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace LastHour.Modules.Notifications.Tests.Application;

public sealed class NotificationOperationsApplicationTests : IDisposable
{
    private readonly DbConnection _connection;
    private readonly DbContextOptions<LastHourDbContext> _dbContextOptions;
    private readonly LastHourDbContext _dbContext;
    private readonly INotificationDeliveryRepository _deliveryRepository;
    private readonly NotificationDeliveryAttemptRepository _attemptRepository;
    private readonly NotificationDeliveryAuditRepository _auditRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TestClock _clock;
    private readonly DateTime _baseTime = new(2026, 8, 20, 12, 0, 0, DateTimeKind.Utc);

    public NotificationOperationsApplicationTests()
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

        _attemptRepository = new NotificationDeliveryAttemptRepository(
            new EfRepository<NotificationDeliveryAttempt>(_dbContext),
            new EfReadRepository<NotificationDeliveryAttempt, Guid>(_dbContext));

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

    private async Task<NotificationDelivery> SeedDeliveryAsync(DeliveryChannel channel = DeliveryChannel.Email, DeliveryStatus status = DeliveryStatus.Pending)
    {
        var notification = Notification.Create(Guid.NewGuid(), Guid.NewGuid(), "user@lasthour.local", "Title", "Body", "UserRegistered", _baseTime);
        _dbContext.Set<Notification>().Add(notification);

        var delivery = NotificationDelivery.Create(notification.Id, channel, _baseTime);
        if (status == DeliveryStatus.Delivered)
        {
            delivery.MarkDelivered(_baseTime);
        }
        else if (status == DeliveryStatus.FailedPermanent)
        {
            delivery.RecordFailure("Fatal error", _baseTime, 1, TimeSpan.FromMinutes(1));
        }
        else if (status == DeliveryStatus.RetryScheduled)
        {
            delivery.RecordFailure("Temp error", _baseTime, 3, TimeSpan.FromMinutes(5));
        }

        _dbContext.Set<NotificationDelivery>().Add(delivery);
        await _dbContext.SaveChangesAsync();
        return delivery;
    }

    [Fact]
    public async Task GetNotificationDeliveriesQuery_ReturnsPaginatedProjections()
    {
        var d1 = await SeedDeliveryAsync(DeliveryChannel.Email);
        var d2 = await SeedDeliveryAsync(DeliveryChannel.InApp);

        var handler = new GetNotificationDeliveriesQueryHandler(_deliveryRepository);
        var query = new GetNotificationDeliveriesQuery();

        Result<PagedResult<NotificationDeliverySummaryDto>> result = await handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Count.Should().Be(2);
    }

    [Fact]
    public async Task GetNotificationDeliveriesQuery_AppliesStatusAndChannelFilters()
    {
        var d1 = await SeedDeliveryAsync(DeliveryChannel.Email, DeliveryStatus.Pending);
        var d2 = await SeedDeliveryAsync(DeliveryChannel.InApp, DeliveryStatus.Delivered);

        var handler = new GetNotificationDeliveriesQueryHandler(_deliveryRepository);
        var query = new GetNotificationDeliveriesQuery(status: DeliveryStatus.Pending, channel: DeliveryChannel.Email);

        Result<PagedResult<NotificationDeliverySummaryDto>> result = await handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().ContainSingle();
        result.Value.Items[0].Id.Should().Be(d1.Id);
    }

    [Fact]
    public void GetNotificationDeliveriesQueryValidator_RejectsInvalidPageAndDates()
    {
        var validator = new GetNotificationDeliveriesQueryValidator();

        validator.Validate(new GetNotificationDeliveriesQuery(pageNumber: 0)).IsValid.Should().BeFalse();
        validator.Validate(new GetNotificationDeliveriesQuery(pageSize: 101)).IsValid.Should().BeFalse();

        DateTime now = DateTime.UtcNow;
        validator.Validate(new GetNotificationDeliveriesQuery(createdFromUtc: now, createdToUtc: now.AddHours(-1))).IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task GetNotificationDeliveryDetailsQuery_ReturnsDetailWhenFound()
    {
        var delivery = await SeedDeliveryAsync(DeliveryChannel.Email);

        var handler = new GetNotificationDeliveryDetailsQueryHandler(_deliveryRepository);
        var query = new GetNotificationDeliveryDetailsQuery(delivery.Id);

        Result<NotificationDeliveryDetailDto> result = await handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be(delivery.Id);
        result.Value.Channel.Should().Be(DeliveryChannel.Email);
    }

    [Fact]
    public async Task GetNotificationDeliveryDetailsQuery_ReturnsNotFoundWhenMissing()
    {
        var handler = new GetNotificationDeliveryDetailsQueryHandler(_deliveryRepository);
        var query = new GetNotificationDeliveryDetailsQuery(Guid.NewGuid());

        Result<NotificationDeliveryDetailDto> result = await handler.Handle(query, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.FirstError!.Code.Should().Be("NotificationDeliveryNotFound");
    }

    [Fact]
    public async Task GetNotificationDeliveryAttemptsQuery_ReturnsAttemptsInOrder()
    {
        var delivery = await SeedDeliveryAsync();
        var attempt1 = NotificationDeliveryAttempt.Create(delivery.Id, 1, DeliveryChannel.Email, _baseTime.AddMinutes(-5));
        var attempt2 = NotificationDeliveryAttempt.Create(delivery.Id, 2, DeliveryChannel.Email, _baseTime);

        await _attemptRepository.AddAsync(attempt1);
        await _attemptRepository.AddAsync(attempt2);
        await _dbContext.SaveChangesAsync();

        var handler = new GetNotificationDeliveryAttemptsQueryHandler(_attemptRepository);
        var query = new GetNotificationDeliveryAttemptsQuery(delivery.Id);

        Result<IReadOnlyCollection<NotificationDeliveryAttemptDto>> result = await handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Count.Should().Be(2);
        result.Value.First().AttemptNumber.Should().Be(2);
    }

    [Fact]
    public async Task GetNotificationDeliveryAuditQuery_ReturnsAuditEntries()
    {
        var delivery = await SeedDeliveryAsync();
        Guid adminId = Guid.NewGuid();
        var audit = NotificationDeliveryAuditEntry.Create(delivery.Id, "Requeued", DeliveryStatus.FailedPermanent, DeliveryStatus.Pending, adminId, "Test reason", _baseTime);

        await _auditRepository.AddAsync(audit);
        await _dbContext.SaveChangesAsync();

        var handler = new GetNotificationDeliveryAuditQueryHandler(_auditRepository);
        var query = new GetNotificationDeliveryAuditQuery(delivery.Id);

        Result<IReadOnlyCollection<NotificationDeliveryAuditDto>> result = await handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainSingle();
        result.Value.First().Action.Should().Be("Requeued");
        result.Value.First().ActorId.Should().Be(adminId);
    }

    [Fact]
    public async Task RequeueNotificationDeliveryCommand_FailedPermanentDelivery_RequeuesAndCreatesAuditEntry()
    {
        var delivery = await SeedDeliveryAsync(status: DeliveryStatus.FailedPermanent);
        Guid adminId = Guid.NewGuid();

        var handler = new RequeueNotificationDeliveryCommandHandler(_deliveryRepository, _auditRepository, _unitOfWork, _clock);
        var command = new RequeueNotificationDeliveryCommand(delivery.Id, adminId, "Manual admin recovery", "corr-1");

        Result result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();

        NotificationDelivery updated = await _dbContext.Set<NotificationDelivery>().SingleAsync(d => d.Id == delivery.Id);
        updated.Status.Should().Be(DeliveryStatus.Pending);
        updated.RetryCount.Should().Be(0);
        updated.LastError.Should().BeNull();

        List<NotificationDeliveryAuditEntry> audits = await _dbContext.Set<NotificationDeliveryAuditEntry>().Where(a => a.DeliveryId == delivery.Id).ToListAsync();
        audits.Should().ContainSingle();
        audits[0].Action.Should().Be("Requeued");
        audits[0].ActorId.Should().Be(adminId);
        audits[0].Reason.Should().Be("Manual admin recovery");
    }

    [Fact]
    public async Task RequeueNotificationDeliveryCommand_DeliveredDelivery_RejectsRequeue()
    {
        var delivery = await SeedDeliveryAsync(status: DeliveryStatus.Delivered);
        Guid adminId = Guid.NewGuid();

        var handler = new RequeueNotificationDeliveryCommandHandler(_deliveryRepository, _auditRepository, _unitOfWork, _clock);
        var command = new RequeueNotificationDeliveryCommand(delivery.Id, adminId, "Attempt on delivered");

        Result result = await handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.FirstError!.Code.Should().Be("RequeueRejected");
    }

    [Fact]
    public async Task RequeueNotificationDeliveryCommand_ActiveProcessingLease_RejectsRequeue()
    {
        var delivery = await SeedDeliveryAsync();
        delivery.Claim(Guid.NewGuid(), _baseTime.AddMinutes(5), _baseTime);
        await _dbContext.SaveChangesAsync();

        Guid adminId = Guid.NewGuid();

        var handler = new RequeueNotificationDeliveryCommandHandler(_deliveryRepository, _auditRepository, _unitOfWork, _clock);
        var command = new RequeueNotificationDeliveryCommand(delivery.Id, adminId, "Attempt on active lease");

        Result result = await handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.FirstError!.Code.Should().Be("RequeueRejected");
    }

    [Fact]
    public async Task RequeueNotificationDeliveriesCommand_BatchRequeue_HandlesPartialSuccess()
    {
        var d1 = await SeedDeliveryAsync(status: DeliveryStatus.FailedPermanent); // Eligible
        var d2 = await SeedDeliveryAsync(status: DeliveryStatus.Delivered); // Rejected (delivered)
        Guid missingId = Guid.NewGuid(); // Rejected (missing)

        Guid adminId = Guid.NewGuid();

        var handler = new RequeueNotificationDeliveriesCommandHandler(_deliveryRepository, _auditRepository, _unitOfWork, _clock);
        var command = new RequeueNotificationDeliveriesCommand(new[] { d1.Id, d2.Id, missingId }, adminId, "Batch requeue test");

        Result<BatchRequeueResponseDto> result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.RequestedCount.Should().Be(3);
        result.Value.RequeuedCount.Should().Be(1);
        result.Value.RejectedCount.Should().Be(2);

        result.Value.Results.First(r => r.DeliveryId == d1.Id).Requeued.Should().BeTrue();
        result.Value.Results.First(r => r.DeliveryId == d2.Id).Requeued.Should().BeFalse();
        result.Value.Results.First(r => r.DeliveryId == missingId).Requeued.Should().BeFalse();
    }

    [Fact]
    public void RequeueNotificationDeliveriesCommandValidator_ValidatesLimitsAndDuplicates()
    {
        var validator = new RequeueNotificationDeliveriesCommandValidator();

        validator.Validate(new RequeueNotificationDeliveriesCommand(Array.Empty<Guid>(), Guid.NewGuid(), "Reason")).IsValid.Should().BeFalse();

        Guid dup = Guid.NewGuid();
        validator.Validate(new RequeueNotificationDeliveriesCommand(new[] { dup, dup }, Guid.NewGuid(), "Reason")).IsValid.Should().BeFalse();
        validator.Validate(new RequeueNotificationDeliveriesCommand(new[] { Guid.Empty }, Guid.NewGuid(), "Reason")).IsValid.Should().BeFalse();
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
