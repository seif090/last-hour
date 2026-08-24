using System.Data.Common;
using LastHour.BuildingBlocks.Infrastructure.Persistence;
using LastHour.BuildingBlocks.Infrastructure.Persistence.Repositories;
using LastHour.Modules.Notifications.Application.Persistence;
using LastHour.Modules.Notifications.Domain.Notifications;
using LastHour.Modules.Notifications.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace LastHour.Modules.Notifications.Tests.Infrastructure;

public sealed class NotificationDeliveryAuditRepositoryTests : IDisposable
{
    private readonly DbConnection _connection;
    private readonly DbContextOptions<LastHourDbContext> _dbContextOptions;

    public NotificationDeliveryAuditRepositoryTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        _dbContextOptions = new DbContextOptionsBuilder<LastHourDbContext>()
            .UseSqlite(_connection)
            .Options;

        using LastHourDbContext context = CreateDbContext();
        context.Database.EnsureCreated();
    }

    public void Dispose()
    {
        _connection.Dispose();
        GC.SuppressFinalize(this);
    }

    private LastHourDbContext CreateDbContext()
        => new(_dbContextOptions, new[] { typeof(NotificationConfiguration).Assembly });

    private static async Task<NotificationDelivery> SeedDeliveryAsync(LastHourDbContext context)
    {
        var notification = Notification.Create(Guid.NewGuid(), Guid.NewGuid(), "user@lasthour.local", "Subject", "Body", "UserRegistered", DateTime.UtcNow);
        context.Set<Notification>().Add(notification);

        var delivery = NotificationDelivery.Create(notification.Id, DeliveryChannel.Email, DateTime.UtcNow);
        context.Set<NotificationDelivery>().Add(delivery);

        await context.SaveChangesAsync();
        return delivery;
    }

    [Fact]
    public async Task AddAuditEntry_PersistsSuccessfully()
    {
        using LastHourDbContext context = CreateDbContext();
        var delivery = await SeedDeliveryAsync(context);
        Guid actorId = Guid.NewGuid();

        var repo = new NotificationDeliveryAuditRepository(
            new EfRepository<NotificationDeliveryAuditEntry>(context),
            new EfReadRepository<NotificationDeliveryAuditEntry, Guid>(context));

        var entry = NotificationDeliveryAuditEntry.Create(
            delivery.Id,
            "Requeued",
            DeliveryStatus.FailedPermanent,
            DeliveryStatus.Pending,
            actorId,
            "Admin requeue override",
            DateTime.UtcNow,
            "corr-123");

        await repo.AddAsync(entry);
        await context.SaveChangesAsync();

        IReadOnlyList<NotificationDeliveryAuditEntry> audits = await repo.GetByDeliveryIdAsync(delivery.Id);
        Assert.Single(audits);
        Assert.Equal("Requeued", audits[0].Action);
        Assert.Equal(actorId, audits[0].ActorId);
        Assert.Equal("Admin requeue override", audits[0].Reason);
        Assert.Equal("corr-123", audits[0].CorrelationId);
    }

    [Fact]
    public async Task GetByDeliveryId_RetrievesAuditHistory()
    {
        using LastHourDbContext context = CreateDbContext();
        var delivery = await SeedDeliveryAsync(context);
        Guid actorId = Guid.NewGuid();

        var repo = new NotificationDeliveryAuditRepository(
            new EfRepository<NotificationDeliveryAuditEntry>(context),
            new EfReadRepository<NotificationDeliveryAuditEntry, Guid>(context));

        DateTime now = DateTime.UtcNow;
        var entry1 = NotificationDeliveryAuditEntry.Create(delivery.Id, "Requeued", DeliveryStatus.FailedPermanent, DeliveryStatus.Pending, actorId, "Reason 1", now.AddMinutes(-10));
        var entry2 = NotificationDeliveryAuditEntry.Create(delivery.Id, "Requeued", DeliveryStatus.RetryScheduled, DeliveryStatus.Pending, actorId, "Reason 2", now.AddMinutes(-5));

        await repo.AddAsync(entry1);
        await repo.AddAsync(entry2);
        await context.SaveChangesAsync();

        IReadOnlyList<NotificationDeliveryAuditEntry> audits = await repo.GetByDeliveryIdAsync(delivery.Id);
        Assert.Equal(2, audits.Count);
    }

    [Fact]
    public async Task GetByDeliveryId_AuditHistoryOrderingIsDeterministic()
    {
        using LastHourDbContext context = CreateDbContext();
        var delivery = await SeedDeliveryAsync(context);
        Guid actorId = Guid.NewGuid();

        var repo = new NotificationDeliveryAuditRepository(
            new EfRepository<NotificationDeliveryAuditEntry>(context),
            new EfReadRepository<NotificationDeliveryAuditEntry, Guid>(context));

        DateTime now = DateTime.UtcNow;
        var entry1 = NotificationDeliveryAuditEntry.Create(delivery.Id, "Requeued", DeliveryStatus.FailedPermanent, DeliveryStatus.Pending, actorId, "Reason 1", now.AddMinutes(-10));
        var entry2 = NotificationDeliveryAuditEntry.Create(delivery.Id, "BatchRequeued", DeliveryStatus.RetryScheduled, DeliveryStatus.Pending, actorId, "Reason 2", now.AddMinutes(-5));

        await repo.AddAsync(entry1);
        await repo.AddAsync(entry2);
        await context.SaveChangesAsync();

        IReadOnlyList<NotificationDeliveryAuditEntry> audits = await repo.GetByDeliveryIdAsync(delivery.Id);
        // Ordered descending by OccurredAtUtc
        Assert.Equal("BatchRequeued", audits[0].Action);
        Assert.Equal("Requeued", audits[1].Action);
    }

    [Fact]
    public async Task GetByActorId_RetrievesAuditHistoryByActor()
    {
        using LastHourDbContext context = CreateDbContext();
        var delivery = await SeedDeliveryAsync(context);
        Guid actorId1 = Guid.NewGuid();
        Guid actorId2 = Guid.NewGuid();

        var repo = new NotificationDeliveryAuditRepository(
            new EfRepository<NotificationDeliveryAuditEntry>(context),
            new EfReadRepository<NotificationDeliveryAuditEntry, Guid>(context));

        DateTime now = DateTime.UtcNow;
        var entry1 = NotificationDeliveryAuditEntry.Create(delivery.Id, "Requeued", DeliveryStatus.FailedPermanent, DeliveryStatus.Pending, actorId1, "Reason 1", now.AddMinutes(-10));
        var entry2 = NotificationDeliveryAuditEntry.Create(delivery.Id, "Requeued", DeliveryStatus.RetryScheduled, DeliveryStatus.Pending, actorId2, "Reason 2", now.AddMinutes(-5));

        await repo.AddAsync(entry1);
        await repo.AddAsync(entry2);
        await context.SaveChangesAsync();

        IReadOnlyList<NotificationDeliveryAuditEntry> actor1Audits = await repo.GetByActorIdAsync(actorId1);
        Assert.Single(actor1Audits);
        Assert.Equal("Reason 1", actor1Audits[0].Reason);
    }

    [Fact]
    public async Task DifferentDeliveries_CanContainIndependentAuditHistories()
    {
        using LastHourDbContext context = CreateDbContext();
        var delivery1 = await SeedDeliveryAsync(context);
        var delivery2 = await SeedDeliveryAsync(context);
        Guid actorId = Guid.NewGuid();

        var repo = new NotificationDeliveryAuditRepository(
            new EfRepository<NotificationDeliveryAuditEntry>(context),
            new EfReadRepository<NotificationDeliveryAuditEntry, Guid>(context));

        var entry1 = NotificationDeliveryAuditEntry.Create(delivery1.Id, "Requeued", DeliveryStatus.FailedPermanent, DeliveryStatus.Pending, actorId, "Delivery 1 reason", DateTime.UtcNow);
        var entry2 = NotificationDeliveryAuditEntry.Create(delivery2.Id, "Requeued", DeliveryStatus.FailedPermanent, DeliveryStatus.Pending, actorId, "Delivery 2 reason", DateTime.UtcNow);

        await repo.AddAsync(entry1);
        await repo.AddAsync(entry2);
        await context.SaveChangesAsync();

        IReadOnlyList<NotificationDeliveryAuditEntry> audits1 = await repo.GetByDeliveryIdAsync(delivery1.Id);
        IReadOnlyList<NotificationDeliveryAuditEntry> audits2 = await repo.GetByDeliveryIdAsync(delivery2.Id);

        Assert.Single(audits1);
        Assert.Single(audits2);
        Assert.Equal("Delivery 1 reason", audits1[0].Reason);
        Assert.Equal("Delivery 2 reason", audits2[0].Reason);
    }

    [Fact]
    public async Task AuditPersistence_ContainsNoRecipientPiiFields()
    {
        using LastHourDbContext context = CreateDbContext();
        var entityType = context.Model.FindEntityType(typeof(NotificationDeliveryAuditEntry));
        Assert.NotNull(entityType);

        var columnNames = entityType.GetProperties().Select(p => p.GetColumnName()).ToList();
        string[] forbiddenPiiNames = new[] { "Recipient", "Email", "Phone", "Body", "Template", "Password", "Secret", "Token" };

        foreach (string forbidden in forbiddenPiiNames)
        {
            Assert.DoesNotContain(columnNames, col => col.Equals(forbidden, StringComparison.OrdinalIgnoreCase));
        }

        await Task.CompletedTask;
    }

    [Fact]
    public void AuditRepository_ExposesNoArbitraryUpdateOperation()
    {
        Type repoType = typeof(NotificationDeliveryAuditRepository);
        var methods = repoType.GetMethods().Select(m => m.Name).ToList();

        Assert.DoesNotContain("Update", methods);
        Assert.DoesNotContain("UpdateAsync", methods);
        Assert.DoesNotContain("Delete", methods);
        Assert.DoesNotContain("DeleteAsync", methods);
    }
}
