using System.Data.Common;
using LastHour.BuildingBlocks.Infrastructure.Persistence;
using LastHour.BuildingBlocks.Infrastructure.Persistence.Repositories;
using LastHour.Modules.Notifications.Application.Persistence;
using LastHour.Modules.Notifications.Domain.Notifications;
using LastHour.Modules.Notifications.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace LastHour.Modules.Notifications.Tests.Infrastructure;

public sealed class NotificationDeliveryAttemptRepositoryTests : IDisposable
{
    private readonly DbConnection _connection;
    private readonly DbContextOptions<LastHourDbContext> _dbContextOptions;

    public NotificationDeliveryAttemptRepositoryTests()
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
    public async Task AddAttempt_PersistsSuccessfully()
    {
        using LastHourDbContext context = CreateDbContext();
        var delivery = await SeedDeliveryAsync(context);

        var repo = new NotificationDeliveryAttemptRepository(
            new EfRepository<NotificationDeliveryAttempt>(context),
            new EfReadRepository<NotificationDeliveryAttempt, Guid>(context));

        var attempt = NotificationDeliveryAttempt.Create(delivery.Id, 1, DeliveryChannel.Email, DateTime.UtcNow);
        await repo.AddAsync(attempt);
        await context.SaveChangesAsync();

        NotificationDeliveryAttempt? result = await repo.GetByDeliveryIdAndAttemptNumberAsync(delivery.Id, 1);
        Assert.NotNull(result);
        Assert.Equal(delivery.Id, result.DeliveryId);
        Assert.Equal(1, result.AttemptNumber);
        Assert.Equal(DeliveryChannel.Email, result.Channel);
    }

    [Fact]
    public async Task GetByDeliveryId_ReturnsAllAttemptsForDelivery()
    {
        using LastHourDbContext context = CreateDbContext();
        var delivery = await SeedDeliveryAsync(context);

        var repo = new NotificationDeliveryAttemptRepository(
            new EfRepository<NotificationDeliveryAttempt>(context),
            new EfReadRepository<NotificationDeliveryAttempt, Guid>(context));

        DateTime now = DateTime.UtcNow;
        var attempt1 = NotificationDeliveryAttempt.Create(delivery.Id, 1, DeliveryChannel.Email, now.AddMinutes(-10));
        var attempt2 = NotificationDeliveryAttempt.Create(delivery.Id, 2, DeliveryChannel.Email, now.AddMinutes(-5));

        await repo.AddAsync(attempt1);
        await repo.AddAsync(attempt2);
        await context.SaveChangesAsync();

        IReadOnlyList<NotificationDeliveryAttempt> attempts = await repo.GetByDeliveryIdAsync(delivery.Id);
        Assert.Equal(2, attempts.Count);
    }

    [Fact]
    public async Task GetByDeliveryId_AttemptsAreDeterministicallyOrdered()
    {
        using LastHourDbContext context = CreateDbContext();
        var delivery = await SeedDeliveryAsync(context);

        var repo = new NotificationDeliveryAttemptRepository(
            new EfRepository<NotificationDeliveryAttempt>(context),
            new EfReadRepository<NotificationDeliveryAttempt, Guid>(context));

        DateTime now = DateTime.UtcNow;
        var attempt1 = NotificationDeliveryAttempt.Create(delivery.Id, 1, DeliveryChannel.Email, now.AddMinutes(-10));
        var attempt2 = NotificationDeliveryAttempt.Create(delivery.Id, 2, DeliveryChannel.Email, now.AddMinutes(-5));

        await repo.AddAsync(attempt1);
        await repo.AddAsync(attempt2);
        await context.SaveChangesAsync();

        IReadOnlyList<NotificationDeliveryAttempt> attempts = await repo.GetByDeliveryIdAsync(delivery.Id);
        // Ordered descending by attempt number (or started at)
        Assert.Equal(2, attempts[0].AttemptNumber);
        Assert.Equal(1, attempts[1].AttemptNumber);
    }

    [Fact]
    public async Task GetByDeliveryIdAndAttemptNumber_RetrievesSpecificAttempt()
    {
        using LastHourDbContext context = CreateDbContext();
        var delivery = await SeedDeliveryAsync(context);

        var repo = new NotificationDeliveryAttemptRepository(
            new EfRepository<NotificationDeliveryAttempt>(context),
            new EfReadRepository<NotificationDeliveryAttempt, Guid>(context));

        var attempt = NotificationDeliveryAttempt.Create(delivery.Id, 1, DeliveryChannel.Email, DateTime.UtcNow);
        await repo.AddAsync(attempt);
        await context.SaveChangesAsync();

        NotificationDeliveryAttempt? found = await repo.GetByDeliveryIdAndAttemptNumberAsync(delivery.Id, 1);
        Assert.NotNull(found);
        Assert.Equal(attempt.Id, found.Id);

        NotificationDeliveryAttempt? notFound = await repo.GetByDeliveryIdAndAttemptNumberAsync(delivery.Id, 99);
        Assert.Null(notFound);
    }

    [Fact]
    public async Task DifferentDeliveries_CanBothHaveAttemptNumberOne()
    {
        using LastHourDbContext context = CreateDbContext();
        var delivery1 = await SeedDeliveryAsync(context);
        var delivery2 = await SeedDeliveryAsync(context);

        var repo = new NotificationDeliveryAttemptRepository(
            new EfRepository<NotificationDeliveryAttempt>(context),
            new EfReadRepository<NotificationDeliveryAttempt, Guid>(context));

        var attempt1 = NotificationDeliveryAttempt.Create(delivery1.Id, 1, DeliveryChannel.Email, DateTime.UtcNow);
        var attempt2 = NotificationDeliveryAttempt.Create(delivery2.Id, 1, DeliveryChannel.Email, DateTime.UtcNow);

        await repo.AddAsync(attempt1);
        await repo.AddAsync(attempt2);
        await context.SaveChangesAsync();

        NotificationDeliveryAttempt? res1 = await repo.GetByDeliveryIdAndAttemptNumberAsync(delivery1.Id, 1);
        NotificationDeliveryAttempt? res2 = await repo.GetByDeliveryIdAndAttemptNumberAsync(delivery2.Id, 1);

        Assert.NotNull(res1);
        Assert.NotNull(res2);
        Assert.NotEqual(res1.DeliveryId, res2.DeliveryId);
    }

    [Fact]
    public async Task DuplicateAttemptNumberForSameDelivery_ViolatesUniqueConstraint()
    {
        using LastHourDbContext context = CreateDbContext();
        var delivery = await SeedDeliveryAsync(context);

        var repo = new NotificationDeliveryAttemptRepository(
            new EfRepository<NotificationDeliveryAttempt>(context),
            new EfReadRepository<NotificationDeliveryAttempt, Guid>(context));

        var attempt1 = NotificationDeliveryAttempt.Create(delivery.Id, 1, DeliveryChannel.Email, DateTime.UtcNow);
        var attempt2 = NotificationDeliveryAttempt.Create(delivery.Id, 1, DeliveryChannel.Email, DateTime.UtcNow);

        await repo.AddAsync(attempt1);
        await context.SaveChangesAsync();

        await repo.AddAsync(attempt2);
        await Assert.ThrowsAsync<DbUpdateException>(async () => await context.SaveChangesAsync());
    }

    [Fact]
    public async Task AttemptReadQueries_AreNoTracking()
    {
        using LastHourDbContext context = CreateDbContext();
        var delivery = await SeedDeliveryAsync(context);

        var repo = new NotificationDeliveryAttemptRepository(
            new EfRepository<NotificationDeliveryAttempt>(context),
            new EfReadRepository<NotificationDeliveryAttempt, Guid>(context));

        var attempt = NotificationDeliveryAttempt.Create(delivery.Id, 1, DeliveryChannel.Email, DateTime.UtcNow);
        await repo.AddAsync(attempt);
        await context.SaveChangesAsync();

        context.ChangeTracker.Clear();

        NotificationDeliveryAttempt? loaded = await repo.GetByDeliveryIdAndAttemptNumberAsync(delivery.Id, 1);
        Assert.NotNull(loaded);
        Assert.False(context.Entry(loaded).State == EntityState.Unchanged);
    }

    [Fact]
    public async Task AttemptPersistence_ContainsNoRecipientPiiFields()
    {
        using LastHourDbContext context = CreateDbContext();
        var entityType = context.Model.FindEntityType(typeof(NotificationDeliveryAttempt));
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
    public void AttemptRepository_ExposesNoArbitraryUpdateOperation()
    {
        Type repoType = typeof(NotificationDeliveryAttemptRepository);
        var methods = repoType.GetMethods().Select(m => m.Name).ToList();

        Assert.DoesNotContain("Update", methods);
        Assert.DoesNotContain("UpdateAsync", methods);
        Assert.DoesNotContain("Delete", methods);
        Assert.DoesNotContain("DeleteAsync", methods);
    }
}
