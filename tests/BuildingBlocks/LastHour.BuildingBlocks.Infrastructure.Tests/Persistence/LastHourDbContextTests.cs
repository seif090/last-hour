using System.Text.Json;
using LastHour.BuildingBlocks.Infrastructure.Persistence;
using LastHour.BuildingBlocks.Infrastructure.Persistence.Outbox;
using LastHour.BuildingBlocks.Infrastructure.Persistence.Seeding;
using LastHour.BuildingBlocks.Infrastructure.Tests.Persistence.TestSupport;
using LastHour.BuildingBlocks.SharedKernel.Domain.StronglyTypedIds;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace LastHour.BuildingBlocks.Infrastructure.Tests.Persistence;

public class LastHourDbContextTests
{
    [Fact]
    public async Task SaveChanges_AddedAuditableEntity_StampsAuditFields()
    {
        using LastHourDbContext context = TestPersistence.CreateContext();
        var aggregate = new TestAggregate("merchant");

        context.Set<TestAggregate>().Add(aggregate);
        await context.SaveChangesAsync();

        Assert.Equal(TestPersistence.FixedUtcNow, aggregate.CreatedAt);
        Assert.Equal(TestPersistence.UserId, aggregate.CreatedBy);
        Assert.Null(aggregate.UpdatedAt);
    }

    [Fact]
    public async Task SaveChanges_ModifiedAuditableEntity_StampsUpdatedFields()
    {
        using LastHourDbContext context = TestPersistence.CreateContext();
        var aggregate = new TestAggregate("merchant");
        context.Set<TestAggregate>().Add(aggregate);
        await context.SaveChangesAsync();

        aggregate.Rename("renamed");
        await context.SaveChangesAsync();

        Assert.Equal(TestPersistence.FixedUtcNow, aggregate.UpdatedAt);
        Assert.Equal(TestPersistence.UserId, aggregate.UpdatedBy);
    }

    [Fact]
    public async Task SaveChanges_AggregateWithDomainEvents_WritesOutboxMessages()
    {
        using LastHourDbContext context = TestPersistence.CreateContext();
        var aggregate = new TestAggregate("merchant");
        aggregate.Rename("renamed");
        context.Set<TestAggregate>().Add(aggregate);

        await context.SaveChangesAsync();

        OutboxMessage stored = await context.OutboxMessages.SingleAsync();
        Assert.Contains(nameof(TestAggregateRenamed), stored.Type);
        Assert.Empty(aggregate.GetDomainEvents());

        TestAggregateRenamed? payload = JsonSerializer.Deserialize<TestAggregateRenamed>(
            stored.Content,
            OutboxJson.SerializerOptions);
        Assert.NotNull(payload);
        Assert.Equal(aggregate.Id, payload!.AggregateId);
        Assert.Equal("renamed", payload.NewName);
    }

    [Fact]
    public async Task Remove_SoftDeleteEntity_BecomesLogicalDelete()
    {
        using LastHourDbContext context = TestPersistence.CreateContext();
        var entity = new TestEntity("value");
        context.Set<TestEntity>().Add(entity);
        await context.SaveChangesAsync();

        context.Set<TestEntity>().Remove(entity);
        await context.SaveChangesAsync();

        TestEntity stored = await context.Set<TestEntity>().IgnoreQueryFilters().SingleAsync();
        Assert.True(stored.IsDeleted);
        Assert.Equal(TestPersistence.FixedUtcNow, stored.DeletedAt);
        Assert.Equal(TestPersistence.UserId, stored.DeletedBy);
    }

    [Fact]
    public async Task Queries_ExcludeSoftDeletedEntities()
    {
        using LastHourDbContext context = TestPersistence.CreateContext();
        var entity = new TestEntity("value");
        context.Set<TestEntity>().Add(entity);
        await context.SaveChangesAsync();

        context.Set<TestEntity>().Remove(entity);
        await context.SaveChangesAsync();

        Assert.False(await context.Set<TestEntity>().AnyAsync());
        Assert.Equal(1, await context.Set<TestEntity>().IgnoreQueryFilters().CountAsync());
    }

    [Fact]
    public void Model_StronglyTypedIdProperty_HasAutomaticValueConverter()
    {
        using LastHourDbContext context = TestPersistence.CreateContext();
        IEntityType? entityType = context.Model.FindEntityType(typeof(TestAggregate));
        Assert.NotNull(entityType);
        IProperty? idProperty = entityType!.FindProperty(nameof(TestAggregate.Id));
        Assert.NotNull(idProperty);

        ValueConverter? converter = idProperty!.GetValueConverter();

        Assert.NotNull(converter);
        Assert.Equal(typeof(GuidId), converter!.ModelClrType);
        Assert.Equal(typeof(Guid), converter.ProviderClrType);
    }

    [Fact]
    public void Model_SeedHistory_HasUniqueSeederTypeIndex()
    {
        using LastHourDbContext context = TestPersistence.CreateContext();
        IEntityType? entityType = context.Model.FindEntityType(typeof(SeedHistory));
        Assert.NotNull(entityType);
        Assert.Equal("seeding_history", entityType!.GetTableName());

        IIndex index = entityType.GetIndexes().Single(
            candidate => candidate.Properties.Any(property => property.Name == nameof(SeedHistory.SeederType)));

        Assert.True(index.IsUnique);
    }
}
