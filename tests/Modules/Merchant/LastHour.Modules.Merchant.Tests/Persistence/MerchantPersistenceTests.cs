using FluentAssertions;
using LastHour.BuildingBlocks.Infrastructure.Persistence;
using LastHour.Modules.Merchant.Domain.Merchants;
using LastHour.Modules.Merchant.Tests.Persistence.TestSupport;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using MerchantAggregate = LastHour.Modules.Merchant.Domain.Merchants.Merchant;

namespace LastHour.Modules.Merchant.Tests.Persistence;

public class MerchantPersistenceTests
{
    private static readonly Guid DefaultOwnerId = Guid.NewGuid();
    private static readonly MerchantName DefaultName = MerchantName.From("Quick Mart");
    private static readonly Address DefaultAddress = Address.Create("789 Broadway", "New York", "NY", "10003", "USA");
    private static readonly PhoneNumber DefaultPhone = PhoneNumber.From("+1-212-555-0150");

    [Fact]
    public async Task SaveAndLoad_Merchant_RoundTripsAllPropertiesAndValueObjects()
    {
        using SqliteConnection connection = TestMerchantPersistence.CreateOpenConnection();
        MerchantId persistedId;

        await using (LastHourDbContext context = await TestMerchantPersistence.CreateContextAsync(connection))
        {
            MerchantAggregate merchant = MerchantAggregate.Register(
                DefaultOwnerId,
                DefaultName,
                DefaultAddress,
                DefaultPhone);

            merchant.Verify();

            context.Set<MerchantAggregate>().Add(merchant);
            await context.SaveChangesAsync();

            persistedId = merchant.Id;
        }

        await using (LastHourDbContext reloaded = TestMerchantPersistence.CreateSqliteContext(connection))
        {
            MerchantAggregate? loaded = await reloaded.Set<MerchantAggregate>()
                .SingleOrDefaultAsync(m => m.Id == persistedId);

            loaded.Should().NotBeNull();
            loaded!.Id.Should().Be(persistedId);
            loaded.OwnerId.Should().Be(DefaultOwnerId);
            loaded.Name.Should().Be(DefaultName);
            loaded.Name.Value.Should().Be("Quick Mart");
            loaded.Address.Should().Be(DefaultAddress);
            loaded.Address.Street.Should().Be("789 Broadway");
            loaded.Address.City.Should().Be("New York");
            loaded.Address.StateOrProvince.Should().Be("NY");
            loaded.Address.PostalCode.Should().Be("10003");
            loaded.Address.Country.Should().Be("USA");
            loaded.Phone.Should().Be(DefaultPhone);
            loaded.Phone.Value.Should().Be("+1-212-555-0150");
            loaded.Status.Should().Be(MerchantStatus.Active);
            loaded.CreatedAt.Should().Be(TestMerchantPersistence.FixedUtcNow);
            loaded.CreatedBy.Should().Be(TestMerchantPersistence.CurrentUserId);
        }
    }

    [Fact]
    public async Task AuditInterceptor_StampsCreatedByAndCreatedAt()
    {
        using SqliteConnection connection = TestMerchantPersistence.CreateOpenConnection();
        await using LastHourDbContext context = await TestMerchantPersistence.CreateContextAsync(connection);

        MerchantAggregate merchant = MerchantAggregate.Register(
            DefaultOwnerId,
            DefaultName,
            DefaultAddress,
            DefaultPhone);

        context.Set<MerchantAggregate>().Add(merchant);
        await context.SaveChangesAsync();

        merchant.CreatedBy.Should().Be(TestMerchantPersistence.CurrentUserId);
        merchant.CreatedAt.Should().Be(TestMerchantPersistence.FixedUtcNow);
    }

    [Fact]
    public async Task Repository_GetByIdAsync_ReturnsPersistedMerchant()
    {
        using SqliteConnection connection = TestMerchantPersistence.CreateOpenConnection();
        MerchantId persistedId;

        await using (LastHourDbContext context = await TestMerchantPersistence.CreateContextAsync(connection))
        {
            MerchantAggregate merchant = MerchantAggregate.Register(
                DefaultOwnerId,
                DefaultName,
                DefaultAddress,
                DefaultPhone);

            context.Set<MerchantAggregate>().Add(merchant);
            await context.SaveChangesAsync();
            persistedId = merchant.Id;
        }

        await using (LastHourDbContext context = TestMerchantPersistence.CreateSqliteContext(connection))
        {
            IMerchantRepository repository = TestMerchantPersistence.CreateRepository(context);

            MerchantAggregate? loaded = await repository.GetByIdAsync(persistedId);

            loaded.Should().NotBeNull();
            loaded!.Id.Should().Be(persistedId);
            loaded.Name.Should().Be(DefaultName);
        }
    }

    [Fact]
    public async Task Repository_GetByIdAsync_ReturnsNullForNonexistentMerchant()
    {
        using SqliteConnection connection = TestMerchantPersistence.CreateOpenConnection();
        await using LastHourDbContext context = await TestMerchantPersistence.CreateContextAsync(connection);

        IMerchantRepository repository = TestMerchantPersistence.CreateRepository(context);

        MerchantAggregate? loaded = await repository.GetByIdAsync(MerchantId.New());

        loaded.Should().BeNull();
    }

    [Fact]
    public async Task Repository_GetByIdTrackedAsync_ReturnsTrackedMerchant()
    {
        using SqliteConnection connection = TestMerchantPersistence.CreateOpenConnection();
        MerchantId persistedId;

        await using (LastHourDbContext context = await TestMerchantPersistence.CreateContextAsync(connection))
        {
            MerchantAggregate merchant = MerchantAggregate.Register(
                DefaultOwnerId,
                DefaultName,
                DefaultAddress,
                DefaultPhone);

            context.Set<MerchantAggregate>().Add(merchant);
            await context.SaveChangesAsync();
            persistedId = merchant.Id;
        }

        await using (LastHourDbContext context = TestMerchantPersistence.CreateSqliteContext(connection))
        {
            IMerchantRepository repository = TestMerchantPersistence.CreateRepository(context);

            MerchantAggregate? loaded = await repository.GetByIdTrackedAsync(persistedId);

            loaded.Should().NotBeNull();
            context.Entry(loaded!).State.Should().Be(EntityState.Unchanged);
        }
    }

    [Fact]
    public async Task Repository_ExistsByIdAsync_ReturnsTrueForExistingMerchant()
    {
        using SqliteConnection connection = TestMerchantPersistence.CreateOpenConnection();
        MerchantId persistedId;

        await using (LastHourDbContext context = await TestMerchantPersistence.CreateContextAsync(connection))
        {
            MerchantAggregate merchant = MerchantAggregate.Register(
                DefaultOwnerId,
                DefaultName,
                DefaultAddress,
                DefaultPhone);

            context.Set<MerchantAggregate>().Add(merchant);
            await context.SaveChangesAsync();
            persistedId = merchant.Id;
        }

        await using (LastHourDbContext context = TestMerchantPersistence.CreateSqliteContext(connection))
        {
            IMerchantRepository repository = TestMerchantPersistence.CreateRepository(context);

            bool exists = await repository.ExistsByIdAsync(persistedId);

            exists.Should().BeTrue();
        }
    }

    [Fact]
    public async Task Repository_ExistsByIdAsync_ReturnsFalseForNonexistentMerchant()
    {
        using SqliteConnection connection = TestMerchantPersistence.CreateOpenConnection();
        await using LastHourDbContext context = await TestMerchantPersistence.CreateContextAsync(connection);

        IMerchantRepository repository = TestMerchantPersistence.CreateRepository(context);

        bool exists = await repository.ExistsByIdAsync(MerchantId.New());

        exists.Should().BeFalse();
    }

    [Fact]
    public async Task Repository_ExistsByNameAsync_ReturnsTrueForExistingMerchantName()
    {
        using SqliteConnection connection = TestMerchantPersistence.CreateOpenConnection();

        await using (LastHourDbContext context = await TestMerchantPersistence.CreateContextAsync(connection))
        {
            MerchantAggregate merchant = MerchantAggregate.Register(
                DefaultOwnerId,
                DefaultName,
                DefaultAddress,
                DefaultPhone);

            context.Set<MerchantAggregate>().Add(merchant);
            await context.SaveChangesAsync();
        }

        await using (LastHourDbContext context = TestMerchantPersistence.CreateSqliteContext(connection))
        {
            IMerchantRepository repository = TestMerchantPersistence.CreateRepository(context);

            bool exists = await repository.ExistsByNameAsync(DefaultName);

            exists.Should().BeTrue();
        }
    }

    [Fact]
    public async Task Repository_ExistsByNameAsync_ReturnsFalseForNonexistentMerchantName()
    {
        using SqliteConnection connection = TestMerchantPersistence.CreateOpenConnection();
        await using LastHourDbContext context = await TestMerchantPersistence.CreateContextAsync(connection);

        IMerchantRepository repository = TestMerchantPersistence.CreateRepository(context);

        bool exists = await repository.ExistsByNameAsync(MerchantName.From("Unknown Merchant"));

        exists.Should().BeFalse();
    }

    [Fact]
    public async Task DuplicateMerchantName_DatabaseRejectsSecondInsert()
    {
        using SqliteConnection connection = TestMerchantPersistence.CreateOpenConnection();
        await using LastHourDbContext context = await TestMerchantPersistence.CreateContextAsync(connection);

        MerchantAggregate first = MerchantAggregate.Register(
            DefaultOwnerId,
            MerchantName.From("Duplicate Store"),
            DefaultAddress,
            DefaultPhone);

        context.Set<MerchantAggregate>().Add(first);
        await context.SaveChangesAsync();

        MerchantAggregate second = MerchantAggregate.Register(
            Guid.NewGuid(),
            MerchantName.From("Duplicate Store"),
            Address.Create("100 Other St", "Other City", null, null, "USA"),
            PhoneNumber.From("+1-212-555-9999"));

        context.Set<MerchantAggregate>().Add(second);

        Func<Task> act = () => context.SaveChangesAsync();

        await act.Should().ThrowAsync<DbUpdateException>()
            .Where(exception => HasUniqueConstraintFailure(exception));
    }

    private static bool HasUniqueConstraintFailure(DbUpdateException exception)
    {
        Exception? current = exception;
        while (current is not null)
        {
            if (current is SqliteException { SqliteErrorCode: 19 })
            {
                return true;
            }

            current = current.InnerException;
        }

        return false;
    }

    [Fact]
    public async Task UpdateProfile_WhenDataChanges_UpdatesAuditAndOutbox()
    {
        using SqliteConnection connection = TestMerchantPersistence.CreateOpenConnection();
        MerchantId persistedId;

        await using (LastHourDbContext context = await TestMerchantPersistence.CreateContextAsync(connection))
        {
            MerchantAggregate merchant = MerchantAggregate.Register(
                DefaultOwnerId,
                DefaultName,
                DefaultAddress,
                DefaultPhone);

            context.Set<MerchantAggregate>().Add(merchant);
            await context.SaveChangesAsync();
            persistedId = merchant.Id;
        }

        var commandInterceptor = new TestMerchantPersistence.TestCommandInterceptor();
        await using (LastHourDbContext context = TestMerchantPersistence.CreateSqliteContext(connection, commandInterceptor))
        {
            IMerchantRepository repository = TestMerchantPersistence.CreateRepository(context);
            MerchantAggregate? loaded = await repository.GetByIdTrackedAsync(persistedId);

            loaded.Should().NotBeNull();
            bool isUpdated = loaded!.UpdateProfile(
                MerchantName.From("Updated Name"),
                Address.Create("123 New St", "Seattle", "WA", "98101", "USA"),
                PhoneNumber.From("+1-206-555-9999"));

            isUpdated.Should().BeTrue();
            if (isUpdated)
            {
                repository.Update(loaded);
            }

            await context.SaveChangesAsync();
        }

        await using (LastHourDbContext verifyContext = TestMerchantPersistence.CreateSqliteContext(connection))
        {
            MerchantAggregate? reloaded = await verifyContext.Set<MerchantAggregate>()
                .SingleOrDefaultAsync(m => m.Id == persistedId);

            reloaded.Should().NotBeNull();
            reloaded!.Name.Value.Should().Be("Updated Name");

            var outboxMessages = await verifyContext.OutboxMessages.ToListAsync();
            outboxMessages.Should().Contain(m => m.Type.Contains("MerchantProfileUpdatedDomainEvent"));

            commandInterceptor.ExecutedCommands.Should().Contain(cmd => cmd.Contains("UPDATE") && cmd.Contains("merchants"));
        }
    }

    [Fact]
    public async Task UpdateProfile_WhenAllDataIsUnchanged_DoesNotUpdateAuditOrOutbox()
    {
        using SqliteConnection connection = TestMerchantPersistence.CreateOpenConnection();
        MerchantId persistedId;

        await using (LastHourDbContext context = await TestMerchantPersistence.CreateContextAsync(connection))
        {
            MerchantAggregate merchant = MerchantAggregate.Register(
                DefaultOwnerId,
                DefaultName,
                DefaultAddress,
                DefaultPhone);

            context.Set<MerchantAggregate>().Add(merchant);
            await context.SaveChangesAsync();
            persistedId = merchant.Id;
        }

        int initialOutboxCount;
        var commandInterceptor = new TestMerchantPersistence.TestCommandInterceptor();
        await using (LastHourDbContext context = TestMerchantPersistence.CreateSqliteContext(connection, commandInterceptor))
        {
            initialOutboxCount = await context.OutboxMessages.CountAsync();

            IMerchantRepository repository = TestMerchantPersistence.CreateRepository(context);
            MerchantAggregate? loaded = await repository.GetByIdTrackedAsync(persistedId);

            loaded.Should().NotBeNull();
            bool isUpdated = loaded!.UpdateProfile(
                DefaultName,
                DefaultAddress,
                DefaultPhone);

            isUpdated.Should().BeFalse();
            loaded.GetDomainEvents().Should().BeEmpty();
            if (isUpdated)
            {
                repository.Update(loaded);
            }

            await context.SaveChangesAsync();
        }

        await using (LastHourDbContext verifyContext = TestMerchantPersistence.CreateSqliteContext(connection))
        {
            MerchantAggregate? reloaded = await verifyContext.Set<MerchantAggregate>()
                .SingleOrDefaultAsync(m => m.Id == persistedId);

            reloaded.Should().NotBeNull();
            reloaded!.Name.Value.Should().Be(DefaultName.Value);
            reloaded.UpdatedAt.Should().BeNull();

            int finalOutboxCount = await verifyContext.OutboxMessages.CountAsync();
            finalOutboxCount.Should().Be(initialOutboxCount);

            commandInterceptor.ExecutedCommands.Should().NotContain(cmd => cmd.Contains("UPDATE") && cmd.Contains("merchants"));
        }
    }

    [Fact]
    public async Task MerchantWrite_WhenPersistenceFails_RollsBackAggregateAndOutbox()
    {
        using SqliteConnection connection = TestMerchantPersistence.CreateOpenConnection();
        await using (LastHourDbContext initContext = await TestMerchantPersistence.CreateContextAsync(connection))
        {
            // Initial schema setup
        }

        var throwingInterceptor = new TestMerchantPersistence.ThrowingSaveChangesInterceptor();
        await using (LastHourDbContext context = TestMerchantPersistence.CreateSqliteContext(connection, throwingInterceptor))
        {
            MerchantAggregate merchant = MerchantAggregate.Register(
                DefaultOwnerId,
                DefaultName,
                DefaultAddress,
                DefaultPhone);

            context.Set<MerchantAggregate>().Add(merchant);

            Func<Task> act = async () => await context.SaveChangesAsync();
            await act.Should().ThrowAsync<InvalidOperationException>();
        }

        await using (LastHourDbContext verifyContext = TestMerchantPersistence.CreateSqliteContext(connection))
        {
            int merchantCount = await verifyContext.Set<MerchantAggregate>().CountAsync();
            merchantCount.Should().Be(0);

            int outboxCount = await verifyContext.OutboxMessages.CountAsync();
            outboxCount.Should().Be(0);
        }
    }

    [Fact]
    public async Task MerchantWrite_WhenUniqueConstraintFails_DoesNotPersistOrphanOutbox()
    {
        using SqliteConnection connection = TestMerchantPersistence.CreateOpenConnection();

        await using (LastHourDbContext context = await TestMerchantPersistence.CreateContextAsync(connection))
        {
            MerchantAggregate merchant1 = MerchantAggregate.Register(
                DefaultOwnerId,
                DefaultName,
                DefaultAddress,
                DefaultPhone);

            context.Set<MerchantAggregate>().Add(merchant1);
            await context.SaveChangesAsync();
        }

        await using (LastHourDbContext duplicateContext = TestMerchantPersistence.CreateSqliteContext(connection))
        {
            MerchantAggregate merchant2 = MerchantAggregate.Register(
                Guid.NewGuid(),
                DefaultName, // Duplicate unique name
                Address.Create("456 Other St", "New York", "NY", "10001", "USA"),
                PhoneNumber.From("+1-212-555-9999"));

            duplicateContext.Set<MerchantAggregate>().Add(merchant2);

            Func<Task> act = async () => await duplicateContext.SaveChangesAsync();
            await act.Should().ThrowAsync<DbUpdateException>();
        }

        await using (LastHourDbContext verifyContext = TestMerchantPersistence.CreateSqliteContext(connection))
        {
            int merchantCount = await verifyContext.Set<MerchantAggregate>().CountAsync();
            merchantCount.Should().Be(1);

            int outboxCount = await verifyContext.OutboxMessages.CountAsync();
            outboxCount.Should().Be(1); // Only merchant1's creation event, no orphan event for merchant2
        }
    }

    [Fact]
    public async Task MerchantUpdate_WhenTransactionFails_AuditChangesAreRolledBack()
    {
        using SqliteConnection connection = TestMerchantPersistence.CreateOpenConnection();
        MerchantId persistedId;

        await using (LastHourDbContext context = await TestMerchantPersistence.CreateContextAsync(connection))
        {
            MerchantAggregate merchant = MerchantAggregate.Register(
                DefaultOwnerId,
                DefaultName,
                DefaultAddress,
                DefaultPhone);

            context.Set<MerchantAggregate>().Add(merchant);
            await context.SaveChangesAsync();
            persistedId = merchant.Id;
        }

        var throwingInterceptor = new TestMerchantPersistence.ThrowingSaveChangesInterceptor();
        await using (LastHourDbContext context = TestMerchantPersistence.CreateSqliteContext(connection, throwingInterceptor))
        {
            IMerchantRepository repository = TestMerchantPersistence.CreateRepository(context);
            MerchantAggregate? loaded = await repository.GetByIdTrackedAsync(persistedId);

            loaded.Should().NotBeNull();
            bool isUpdated = loaded!.UpdateProfile(
                MerchantName.From("Updated Name"),
                Address.Create("123 New St", "Seattle", "WA", "98101", "USA"),
                PhoneNumber.From("+1-206-555-9999"));

            isUpdated.Should().BeTrue();
            repository.Update(loaded);

            Func<Task> act = async () => await context.SaveChangesAsync();
            await act.Should().ThrowAsync<InvalidOperationException>();
        }

        await using (LastHourDbContext verifyContext = TestMerchantPersistence.CreateSqliteContext(connection))
        {
            MerchantAggregate? reloaded = await verifyContext.Set<MerchantAggregate>()
                .SingleOrDefaultAsync(m => m.Id == persistedId);

            reloaded.Should().NotBeNull();
            reloaded!.Name.Value.Should().Be(DefaultName.Value);
            reloaded.UpdatedAt.Should().BeNull();

            int outboxCount = await verifyContext.OutboxMessages.CountAsync();
            outboxCount.Should().Be(1); // Only the initial registration event, no profile update event
        }
    }
}
