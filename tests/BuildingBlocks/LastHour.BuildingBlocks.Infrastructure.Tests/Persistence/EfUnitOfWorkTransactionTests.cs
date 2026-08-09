using LastHour.BuildingBlocks.Infrastructure.Persistence;
using LastHour.BuildingBlocks.Infrastructure.Persistence.UnitOfWork;
using LastHour.BuildingBlocks.Infrastructure.Tests.Persistence.TestSupport;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace LastHour.BuildingBlocks.Infrastructure.Tests.Persistence;

public class EfUnitOfWorkTransactionTests
{
    [Fact]
    public async Task BeginThenCommitTransaction_PersistsChanges()
    {
        using SqliteConnection connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        using LastHourDbContext context = TestPersistence.CreateSqliteContext(connection);
        await context.Database.EnsureCreatedAsync();
        var unitOfWork = new EfUnitOfWork(context);

        await unitOfWork.BeginTransactionAsync();
        context.Set<TestAggregate>().Add(new TestAggregate("merchant"));
        await unitOfWork.SaveChangesAsync();
        await unitOfWork.CommitTransactionAsync();

        Assert.Equal(1, await context.Set<TestAggregate>().CountAsync());
    }

    [Fact]
    public async Task BeginThenRollbackTransaction_DiscardsChanges()
    {
        using SqliteConnection connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        using LastHourDbContext context = TestPersistence.CreateSqliteContext(connection);
        await context.Database.EnsureCreatedAsync();
        var unitOfWork = new EfUnitOfWork(context);

        await unitOfWork.BeginTransactionAsync();
        context.Set<TestAggregate>().Add(new TestAggregate("merchant"));
        await unitOfWork.SaveChangesAsync();
        await unitOfWork.RollbackTransactionAsync();

        Assert.Equal(0, await context.Set<TestAggregate>().CountAsync());
    }

    [Fact]
    public async Task BeginTransactionAsync_WhileAlreadyInProgress_Throws()
    {
        using SqliteConnection connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        using LastHourDbContext context = TestPersistence.CreateSqliteContext(connection);
        await context.Database.EnsureCreatedAsync();
        var unitOfWork = new EfUnitOfWork(context);

        await unitOfWork.BeginTransactionAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(() => unitOfWork.BeginTransactionAsync());
    }

    [Fact]
    public async Task CommitTransactionAsync_AfterBegin_AllowsSubsequentBegin()
    {
        using SqliteConnection connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        using LastHourDbContext context = TestPersistence.CreateSqliteContext(connection);
        await context.Database.EnsureCreatedAsync();
        var unitOfWork = new EfUnitOfWork(context);

        await unitOfWork.BeginTransactionAsync();
        await unitOfWork.CommitTransactionAsync();

        await unitOfWork.BeginTransactionAsync();
        await unitOfWork.CommitTransactionAsync();
    }
}
