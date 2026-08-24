using LastHour.BuildingBlocks.Infrastructure.Events;
using LastHour.BuildingBlocks.Infrastructure.Tests.Persistence.TestSupport;
using LastHour.BuildingBlocks.SharedKernel.Domain.StronglyTypedIds;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace LastHour.BuildingBlocks.Infrastructure.Tests.Events;

[Collection("Events")]
public class EventDispatcherTests
{
    [Fact]
    public async Task DispatchAsync_DomainEvent_DeliversEnvelopeToHandler()
    {
        TestSink.Reset();
        using ServiceProvider provider = CreateMediatorProvider();
        var dispatcher = new EventDispatcher(provider.GetRequiredService<IMediator>());
        GuidId aggregateId = GuidId.New();

        await dispatcher.DispatchAsync(new TestAggregateRenamed(aggregateId, "new-name"));

        TestAggregateRenamed received = Assert.IsType<TestAggregateRenamed>(Assert.Single(TestSink.Messages));
        Assert.Equal(aggregateId, received.AggregateId);
        Assert.Equal("new-name", received.NewName);
    }

    [Fact]
    public async Task DispatchAsync_Collection_DeliversInOrder()
    {
        TestSink.Reset();
        using ServiceProvider provider = CreateMediatorProvider();
        var dispatcher = new EventDispatcher(provider.GetRequiredService<IMediator>());
        var first = new TestAggregateRenamed(GuidId.New(), "first");
        var second = new TestAggregateRenamed(GuidId.New(), "second");

        await dispatcher.DispatchAsync(new[] { first, second });

        Assert.Equal(2, TestSink.Messages.Count);
        TestAggregateRenamed firstReceived = Assert.IsType<TestAggregateRenamed>(TestSink.Messages.ElementAt(0));
        TestAggregateRenamed secondReceived = Assert.IsType<TestAggregateRenamed>(TestSink.Messages.ElementAt(1));
        Assert.Equal("first", firstReceived.NewName);
        Assert.Equal("second", secondReceived.NewName);
    }

    private static ServiceProvider CreateMediatorProvider()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMediatR(config => config.RegisterServicesFromAssembly(typeof(TestAggregateRenamedNotificationHandler).Assembly));
        return services.BuildServiceProvider();
    }
}
