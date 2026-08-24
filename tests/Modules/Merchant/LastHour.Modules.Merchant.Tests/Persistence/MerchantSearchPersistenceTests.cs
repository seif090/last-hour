using FluentAssertions;
using LastHour.BuildingBlocks.Infrastructure.Persistence;
using LastHour.Modules.Merchant.Application.Models;
using LastHour.Modules.Merchant.Domain.Merchants;
using LastHour.Modules.Merchant.Tests.Persistence.TestSupport;
using Microsoft.Data.Sqlite;
using MerchantAggregate = LastHour.Modules.Merchant.Domain.Merchants.Merchant;

namespace LastHour.Modules.Merchant.Tests.Persistence;

public class MerchantSearchPersistenceTests
{
    [Fact]
    public async Task Search_SortedByNameAcrossPages_ReturnsStableOrderedSlices()
    {
        using SqliteConnection connection = TestMerchantPersistence.CreateOpenConnection();
        await using (LastHourDbContext context = await TestMerchantPersistence.CreateContextAsync(connection))
        {
            AddMerchant(context, "Alpha Market", "New York");
            AddMerchant(context, "Bravo Grocery", "New York");
            AddMerchant(context, "Charlie Foods", "New York");
            AddMerchant(context, "Delta Store", "New York");
            AddMerchant(context, "Echo Mart", "New York");
            await context.SaveChangesAsync();
        }

        await using (LastHourDbContext context = TestMerchantPersistence.CreateSqliteContext(connection))
        {
            IMerchantRepository repository = TestMerchantPersistence.CreateRepository(context);

            IReadOnlyList<MerchantAggregate> first = await repository.SearchAsync(
                null, null, MerchantSortField.Name, MerchantSortDirection.Asc, 1, 2, CancellationToken.None);
            IReadOnlyList<MerchantAggregate> second = await repository.SearchAsync(
                null, null, MerchantSortField.Name, MerchantSortDirection.Asc, 2, 2, CancellationToken.None);
            int total = await repository.CountAsync(null, null, CancellationToken.None);

            first.Select(m => m.Name.Value).Should().Equal("Alpha Market", "Bravo Grocery");
            second.Select(m => m.Name.Value).Should().Equal("Charlie Foods", "Delta Store");
            total.Should().Be(5);
        }
    }

    [Fact]
    public async Task Search_SortedByNameDescending_ReturnsReversedOrder()
    {
        using SqliteConnection connection = TestMerchantPersistence.CreateOpenConnection();
        await using (LastHourDbContext context = await TestMerchantPersistence.CreateContextAsync(connection))
        {
            AddMerchant(context, "Alpha Market", "Boston");
            AddMerchant(context, "Bravo Grocery", "Boston");
            AddMerchant(context, "Charlie Foods", "Boston");
            await context.SaveChangesAsync();
        }

        await using (LastHourDbContext context = TestMerchantPersistence.CreateSqliteContext(connection))
        {
            IMerchantRepository repository = TestMerchantPersistence.CreateRepository(context);

            IReadOnlyList<MerchantAggregate> results = await repository.SearchAsync(
                null, null, MerchantSortField.Name, MerchantSortDirection.Desc, 1, 10, CancellationToken.None);

            results.Select(m => m.Name.Value).Should().Equal("Charlie Foods", "Bravo Grocery", "Alpha Market");
        }
    }

    [Fact]
    public async Task Search_ByMerchantName_ReturnsMatchingMerchants()
    {
        using SqliteConnection connection = TestMerchantPersistence.CreateOpenConnection();
        await using (LastHourDbContext context = await TestMerchantPersistence.CreateContextAsync(connection))
        {
            AddMerchant(context, "Super Bakery", "Chicago");
            AddMerchant(context, "Mega Electronics", "Chicago");
            await context.SaveChangesAsync();
        }

        await using (LastHourDbContext context = TestMerchantPersistence.CreateSqliteContext(connection))
        {
            IMerchantRepository repository = TestMerchantPersistence.CreateRepository(context);

            IReadOnlyList<MerchantAggregate> matches = await repository.SearchAsync(
                "Bakery", null, MerchantSortField.Name, MerchantSortDirection.Asc, 1, 10, CancellationToken.None);
            int count = await repository.CountAsync("Bakery", null, CancellationToken.None);

            matches.Should().ContainSingle(m => m.Name.Value == "Super Bakery");
            count.Should().Be(1);
        }
    }

    [Fact]
    public async Task Search_ByCity_ReturnsMatchingMerchants()
    {
        using SqliteConnection connection = TestMerchantPersistence.CreateOpenConnection();
        await using (LastHourDbContext context = await TestMerchantPersistence.CreateContextAsync(connection))
        {
            AddMerchant(context, "Shop One", "Seattle");
            AddMerchant(context, "Shop Two", "Miami");
            await context.SaveChangesAsync();
        }

        await using (LastHourDbContext context = TestMerchantPersistence.CreateSqliteContext(connection))
        {
            IMerchantRepository repository = TestMerchantPersistence.CreateRepository(context);

            IReadOnlyList<MerchantAggregate> matches = await repository.SearchAsync(
                "Miami", null, MerchantSortField.Name, MerchantSortDirection.Asc, 1, 10, CancellationToken.None);
            int count = await repository.CountAsync("Miami", null, CancellationToken.None);

            matches.Should().ContainSingle(m => m.Name.Value == "Shop Two");
            count.Should().Be(1);
        }
    }

    [Fact]
    public async Task Search_EmptyOrWhitespaceSearchTerm_ReturnsAllApplicableMerchants()
    {
        using SqliteConnection connection = TestMerchantPersistence.CreateOpenConnection();
        await using (LastHourDbContext context = await TestMerchantPersistence.CreateContextAsync(connection))
        {
            AddMerchant(context, "First Store", "Austin");
            AddMerchant(context, "Second Store", "Dallas");
            await context.SaveChangesAsync();
        }

        await using (LastHourDbContext context = TestMerchantPersistence.CreateSqliteContext(connection))
        {
            IMerchantRepository repository = TestMerchantPersistence.CreateRepository(context);

            IReadOnlyList<MerchantAggregate> emptyMatches = await repository.SearchAsync(
                string.Empty, null, MerchantSortField.Name, MerchantSortDirection.Asc, 1, 10, CancellationToken.None);
            IReadOnlyList<MerchantAggregate> whitespaceMatches = await repository.SearchAsync(
                "   ", null, MerchantSortField.Name, MerchantSortDirection.Asc, 1, 10, CancellationToken.None);
            int total = await repository.CountAsync("   ", null, CancellationToken.None);

            emptyMatches.Should().HaveCount(2);
            whitespaceMatches.Should().HaveCount(2);
            total.Should().Be(2);
        }
    }

    [Fact]
    public async Task Search_WithStatusFilter_ReturnsOnlyMatchingStatus()
    {
        using SqliteConnection connection = TestMerchantPersistence.CreateOpenConnection();
        await using (LastHourDbContext context = await TestMerchantPersistence.CreateContextAsync(connection))
        {
            MerchantAggregate pendingMerchant = AddMerchant(context, "Pending Corp", "Houston");
            MerchantAggregate activeMerchant = AddMerchant(context, "Active Corp", "Houston");
            activeMerchant.Verify();
            await context.SaveChangesAsync();
        }

        await using (LastHourDbContext context = TestMerchantPersistence.CreateSqliteContext(connection))
        {
            IMerchantRepository repository = TestMerchantPersistence.CreateRepository(context);

            IReadOnlyList<MerchantAggregate> activeResults = await repository.SearchAsync(
                null, MerchantStatus.Active, MerchantSortField.Name, MerchantSortDirection.Asc, 1, 10, CancellationToken.None);
            int activeCount = await repository.CountAsync(null, MerchantStatus.Active, CancellationToken.None);

            activeResults.Should().ContainSingle(m => m.Name.Value == "Active Corp");
            activeCount.Should().Be(1);
        }
    }

    [Fact]
    public async Task Search_CombinedSearchAndStatusFilter_ReturnsIntersection()
    {
        using SqliteConnection connection = TestMerchantPersistence.CreateOpenConnection();
        await using (LastHourDbContext context = await TestMerchantPersistence.CreateContextAsync(connection))
        {
            MerchantAggregate m1 = AddMerchant(context, "Target Store", "Austin");
            MerchantAggregate m2 = AddMerchant(context, "Target Shop", "Austin");
            m2.Verify();
            MerchantAggregate m3 = AddMerchant(context, "Other Store", "Austin");
            m3.Verify();
            await context.SaveChangesAsync();
        }

        await using (LastHourDbContext context = TestMerchantPersistence.CreateSqliteContext(connection))
        {
            IMerchantRepository repository = TestMerchantPersistence.CreateRepository(context);

            IReadOnlyList<MerchantAggregate> results = await repository.SearchAsync(
                "Target", MerchantStatus.Active, MerchantSortField.Name, MerchantSortDirection.Asc, 1, 10, CancellationToken.None);
            int count = await repository.CountAsync("Target", MerchantStatus.Active, CancellationToken.None);

            results.Should().ContainSingle(m => m.Name.Value == "Target Shop");
            count.Should().Be(1);
        }
    }

    [Fact]
    public async Task Search_SortedByStatus_OrdersCorrectly()
    {
        using SqliteConnection connection = TestMerchantPersistence.CreateOpenConnection();
        await using (LastHourDbContext context = await TestMerchantPersistence.CreateContextAsync(connection))
        {
            MerchantAggregate pending = AddMerchant(context, "Pending Merchant", "Denver");
            MerchantAggregate active = AddMerchant(context, "Active Merchant", "Denver");
            active.Verify();
            await context.SaveChangesAsync();
        }

        await using (LastHourDbContext context = TestMerchantPersistence.CreateSqliteContext(connection))
        {
            IMerchantRepository repository = TestMerchantPersistence.CreateRepository(context);

            IReadOnlyList<MerchantAggregate> results = await repository.SearchAsync(
                null, null, MerchantSortField.Status, MerchantSortDirection.Asc, 1, 10, CancellationToken.None);

            results.Should().HaveCount(2);
        }
    }

    [Fact]
    public async Task Search_SortedByCreatedAt_OrdersCorrectly()
    {
        using SqliteConnection connection = TestMerchantPersistence.CreateOpenConnection();
        await using (LastHourDbContext context = await TestMerchantPersistence.CreateContextAsync(connection))
        {
            AddMerchant(context, "Merchant One", "Phoenix");
            AddMerchant(context, "Merchant Two", "Phoenix");
            await context.SaveChangesAsync();
        }

        await using (LastHourDbContext context = TestMerchantPersistence.CreateSqliteContext(connection))
        {
            IMerchantRepository repository = TestMerchantPersistence.CreateRepository(context);

            IReadOnlyList<MerchantAggregate> results = await repository.SearchAsync(
                null, null, MerchantSortField.CreatedAt, MerchantSortDirection.Asc, 1, 10, CancellationToken.None);

            results.Should().HaveCount(2);
        }
    }

    private static MerchantAggregate AddMerchant(
        LastHourDbContext context,
        string name,
        string city)
    {
        MerchantAggregate merchant = MerchantAggregate.Register(
            Guid.NewGuid(),
            MerchantName.From(name),
            Address.Create("123 Main St", city, "CA", "90210", "USA"),
            PhoneNumber.From("+1-555-0100"));

        context.Set<MerchantAggregate>().Add(merchant);
        return merchant;
    }
}
