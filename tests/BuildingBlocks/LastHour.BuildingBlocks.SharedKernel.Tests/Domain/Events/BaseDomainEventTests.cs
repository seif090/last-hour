using LastHour.BuildingBlocks.SharedKernel.Domain.Events;

namespace LastHour.BuildingBlocks.SharedKernel.Tests.Domain.Events;

public class BaseDomainEventTests
{
    [Fact]
    public void Constructor_AssignsNonEmptyEventId()
    {
        TestDomainEvent domainEvent = new TestDomainEvent();

        Assert.NotEqual(Guid.Empty, domainEvent.EventId);
    }

    [Fact]
    public void Constructor_AssignsUtcOccurredOnCloseToNow()
    {
        DateTime before = DateTime.UtcNow;
        TestDomainEvent domainEvent = new TestDomainEvent();
        DateTime after = DateTime.UtcNow;

        Assert.Equal(DateTimeKind.Utc, domainEvent.OccurredOn.Kind);
        Assert.InRange(domainEvent.OccurredOn, before, after);
    }

    [Fact]
    public void TwoInstances_HaveDistinctEventIds()
    {
        TestDomainEvent first = new TestDomainEvent();
        TestDomainEvent second = new TestDomainEvent();

        Assert.NotEqual(first.EventId, second.EventId);
    }

    private sealed class TestDomainEvent : BaseDomainEvent
    {
    }
}
