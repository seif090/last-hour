using LastHour.BuildingBlocks.Infrastructure.Persistence;
using LastHour.BuildingBlocks.Infrastructure.Persistence.Repositories;
using LastHour.BuildingBlocks.Infrastructure.Tests.Persistence.TestSupport;
using LastHour.BuildingBlocks.SharedKernel.Domain.StronglyTypedIds;

namespace LastHour.BuildingBlocks.Infrastructure.Tests.Persistence;

public class EfReadRepositoryTests
{
    [Fact]
    public async Task GetByIdAsync_ExistingEntity_ReturnsEntity()
    {
        using LastHourDbContext context = TestPersistence.CreateContext();
        var entity = new TestEntity("value");
        context.Set<TestEntity>().Add(entity);
        await context.SaveChangesAsync();
        var repository = new EfReadRepository<TestEntity, GuidId>(context);

        TestEntity? found = await repository.GetByIdAsync(entity.Id);

        Assert.NotNull(found);
        Assert.Equal("value", found!.Value);
    }

    [Fact]
    public async Task GetByIdAsync_MissingEntity_ReturnsNull()
    {
        using LastHourDbContext context = TestPersistence.CreateContext();
        var repository = new EfReadRepository<TestEntity, GuidId>(context);

        TestEntity? found = await repository.GetByIdAsync(GuidId.New());

        Assert.Null(found);
    }

    [Fact]
    public async Task ListAsync_ReturnsAllEntities()
    {
        using LastHourDbContext context = TestPersistence.CreateContext();
        context.Set<TestEntity>().Add(new TestEntity("first"));
        context.Set<TestEntity>().Add(new TestEntity("second"));
        await context.SaveChangesAsync();
        var repository = new EfReadRepository<TestEntity, GuidId>(context);

        IReadOnlyList<TestEntity> result = await repository.ListAsync();

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task AnyExistsCount_ReflectStoreState()
    {
        using LastHourDbContext context = TestPersistence.CreateContext();
        var entity = new TestEntity("value");
        context.Set<TestEntity>().Add(entity);
        await context.SaveChangesAsync();
        var repository = new EfReadRepository<TestEntity, GuidId>(context);

        Assert.True(await repository.AnyAsync());
        Assert.True(await repository.ExistsAsync(entity.Id));
        Assert.False(await repository.ExistsAsync(GuidId.New()));
        Assert.Equal(1, await repository.CountAsync());
    }

    [Fact]
    public async Task GetByIdAsync_SoftDeletedEntity_ReturnsNull()
    {
        using LastHourDbContext context = TestPersistence.CreateContext();
        var entity = new TestEntity("value");
        context.Set<TestEntity>().Add(entity);
        await context.SaveChangesAsync();
        context.Set<TestEntity>().Remove(entity);
        await context.SaveChangesAsync();
        var repository = new EfReadRepository<TestEntity, GuidId>(context);

        TestEntity? found = await repository.GetByIdAsync(entity.Id);

        Assert.Null(found);
    }

    [Fact]
    public async Task ListAsync_ExcludesSoftDeletedEntities()
    {
        using LastHourDbContext context = TestPersistence.CreateContext();
        var retained = new TestEntity("retained");
        var removed = new TestEntity("removed");
        context.Set<TestEntity>().AddRange(retained, removed);
        await context.SaveChangesAsync();
        context.Set<TestEntity>().Remove(removed);
        await context.SaveChangesAsync();
        var repository = new EfReadRepository<TestEntity, GuidId>(context);

        IReadOnlyList<TestEntity> result = await repository.ListAsync();

        TestEntity only = Assert.Single(result);
        Assert.Equal("retained", only.Value);
    }
}
