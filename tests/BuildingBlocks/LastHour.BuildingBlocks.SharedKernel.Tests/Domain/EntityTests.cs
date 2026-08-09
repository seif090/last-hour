using LastHour.BuildingBlocks.SharedKernel.Domain;
using LastHour.BuildingBlocks.SharedKernel.Domain.Events;

namespace LastHour.BuildingBlocks.SharedKernel.Tests.Domain;

public class EntityTests
{
    [Fact]
    public void RaiseDomainEvent_RegistersEvent_ExposedAsReadOnly()
    {
        TestEntity entity = new TestEntity();
        IDomainEvent domainEvent = new TestDomainEvent();

        entity.Raise(domainEvent);

        Assert.Single(entity.GetDomainEvents());
        Assert.Same(domainEvent, entity.GetDomainEvents().Single());
    }

    [Fact]
    public void RaiseDomainEvent_RegistersMultipleEventsInOrder()
    {
        TestEntity entity = new TestEntity();
        IDomainEvent first = new TestDomainEvent();
        IDomainEvent second = new TestDomainEvent();

        entity.Raise(first);
        entity.Raise(second);

        Assert.Equal(new[] { first, second }, entity.GetDomainEvents());
    }

    [Fact]
    public void ClearDomainEvents_RemovesPendingEvents()
    {
        TestEntity entity = new TestEntity();
        entity.Raise(new TestDomainEvent());

        entity.ClearDomainEvents();

        Assert.Empty(entity.GetDomainEvents());
    }

    [Fact]
    public void RaiseDomainEvent_Null_ThrowsArgumentNullException()
    {
        TestEntity entity = new TestEntity();

        Assert.Throws<ArgumentNullException>(() => entity.Raise(null!));
    }

    [Fact]
    public void GetDomainEvents_ReturnsReadOnlyCollection()
    {
        TestEntity entity = new TestEntity();
        entity.Raise(new TestDomainEvent());

        IReadOnlyCollection<IDomainEvent> events = entity.GetDomainEvents();

        Assert.Throws<NotSupportedException>(
            () => ((ICollection<IDomainEvent>)events).Add(new TestDomainEvent()));
    }

    [Fact]
    public void EntityOfT_ExposesIdAssignedByDerivedType()
    {
        Guid id = Guid.NewGuid();
        TestEntityWithId entity = new TestEntityWithId(id);

        Assert.Equal(id, entity.Id);
    }

    [Fact]
    public void AggregateRootOfT_RaisesDomainEvents_AndExposesId()
    {
        Guid id = Guid.NewGuid();
        TestAggregateRoot aggregateRoot = new TestAggregateRoot(id);
        IDomainEvent domainEvent = new TestDomainEvent();

        aggregateRoot.Raise(domainEvent);

        Assert.Equal(id, aggregateRoot.Id);
        Assert.Same(domainEvent, aggregateRoot.GetDomainEvents().Single());
    }

    private sealed class TestDomainEvent : BaseDomainEvent
    {
    }

    private sealed class TestEntity : Entity
    {
        public void Raise(IDomainEvent domainEvent) => RaiseDomainEvent(domainEvent);
    }

    private sealed class TestEntityWithId : Entity<Guid>
    {
        public TestEntityWithId(Guid id) => Id = id;

        public void Raise(IDomainEvent domainEvent) => RaiseDomainEvent(domainEvent);
    }

    private sealed class TestAggregateRoot : AggregateRoot<Guid>
    {
        public TestAggregateRoot(Guid id) => Id = id;

        public void Raise(IDomainEvent domainEvent) => RaiseDomainEvent(domainEvent);
    }
}
