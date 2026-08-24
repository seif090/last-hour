using System.Collections.Concurrent;
using LastHour.BuildingBlocks.Application.Events;
using LastHour.BuildingBlocks.SharedKernel.Domain;
using LastHour.BuildingBlocks.SharedKernel.Domain.Events;
using LastHour.BuildingBlocks.SharedKernel.Domain.StronglyTypedIds;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LastHour.BuildingBlocks.Infrastructure.Tests.Persistence.TestSupport;

public sealed class TestAggregate : AggregateRoot<GuidId>, IAuditableEntity, ISoftDelete
{
    private TestAggregate()
    {
    }

    public TestAggregate(string name)
    {
        Id = GuidId.New();
        Name = name;
    }

    public string Name { get; private set; } = string.Empty;

    public bool IsDeleted { get; set; }

    public DateTime? DeletedAt { get; set; }

    public string? DeletedBy { get; set; }

    public DateTime CreatedAt { get; set; }

    public string? CreatedBy { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public string? UpdatedBy { get; set; }

    public void Rename(string name)
    {
        Name = name;
        RaiseDomainEvent(new TestAggregateRenamed(Id, name));
    }
}

public sealed class TestAggregateRenamed : BaseDomainEvent
{
    public TestAggregateRenamed(GuidId aggregateId, string newName)
    {
        AggregateId = aggregateId;
        NewName = newName;
    }

    public GuidId AggregateId { get; }

    public string NewName { get; }
}

public sealed class TestEntity : Entity<GuidId>, ISoftDelete
{
    private TestEntity()
    {
    }

    public TestEntity(string value)
    {
        Id = GuidId.New();
        Value = value;
    }

    public string Value { get; private set; } = string.Empty;

    public bool IsDeleted { get; set; }

    public DateTime? DeletedAt { get; set; }

    public string? DeletedBy { get; set; }
}

public sealed class TestAggregateConfiguration : IEntityTypeConfiguration<TestAggregate>
{
    public void Configure(EntityTypeBuilder<TestAggregate> builder)
    {
        builder.ToTable("test_aggregates");
        builder.Property(aggregate => aggregate.Name).HasMaxLength(200).IsRequired();
    }
}

public sealed class TestEntityConfiguration : IEntityTypeConfiguration<TestEntity>
{
    public void Configure(EntityTypeBuilder<TestEntity> builder)
    {
        builder.ToTable("test_entities");
        builder.Property(entity => entity.Value).HasMaxLength(200).IsRequired();
    }
}

public sealed record TestMessage(string Value);

public sealed record ThrowingTestMessage(string Value);

public static class TestSink
{
    public static ConcurrentQueue<object> Messages { get; } = new ConcurrentQueue<object>();

    public static void Reset() => Messages.Clear();
}

public sealed class TestAggregateRenamedNotificationHandler : INotificationHandler<NotificationMessage<TestAggregateRenamed>>
{
    public Task Handle(NotificationMessage<TestAggregateRenamed> notification, CancellationToken cancellationToken)
    {
        TestSink.Messages.Enqueue(notification.Message);
        return Task.CompletedTask;
    }
}

public sealed class TestMessageNotificationHandler : INotificationHandler<NotificationMessage<TestMessage>>
{
    public Task Handle(NotificationMessage<TestMessage> notification, CancellationToken cancellationToken)
    {
        TestSink.Messages.Enqueue(notification.Message);
        return Task.CompletedTask;
    }
}

public sealed class ThrowingTestMessageNotificationHandler : INotificationHandler<NotificationMessage<ThrowingTestMessage>>
{
    public Task Handle(NotificationMessage<ThrowingTestMessage> notification, CancellationToken cancellationToken)
        => throw new InvalidOperationException("handler failed");
}

public sealed record CancellingTestMessage(string Value);

public sealed class CancellingTestMessageNotificationHandler : INotificationHandler<NotificationMessage<CancellingTestMessage>>
{
    public static Action? OnHandling { get; set; }

    public async Task Handle(NotificationMessage<CancellingTestMessage> notification, CancellationToken cancellationToken)
    {
        OnHandling?.Invoke();
        await Task.Delay(Timeout.Infinite, cancellationToken);
    }
}

public sealed record InternalCancelledTestMessage(string Value);

public sealed class InternalCancelledTestMessageNotificationHandler : INotificationHandler<NotificationMessage<InternalCancelledTestMessage>>
{
    public Task Handle(NotificationMessage<InternalCancelledTestMessage> notification, CancellationToken cancellationToken)
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        throw new OperationCanceledException("Internal handler timeout", cts.Token);
    }
}

public sealed class TestDomainEvent : BaseDomainEvent
{
    public TestDomainEvent(string data)
    {
        Data = data;
    }

    public string Data { get; }
}

public sealed class TestDomainEventNotificationHandler : INotificationHandler<NotificationMessage<TestDomainEvent>>
{
    public Task Handle(NotificationMessage<TestDomainEvent> notification, CancellationToken cancellationToken)
    {
        TestSink.Messages.Enqueue(notification.Message);
        return Task.CompletedTask;
    }
}
