using System.Text.Json;
using LastHour.BuildingBlocks.Infrastructure.Persistence.Outbox;
using LastHour.BuildingBlocks.Infrastructure.Tests.Persistence.TestSupport;
using LastHour.BuildingBlocks.SharedKernel.Domain.StronglyTypedIds;

namespace LastHour.BuildingBlocks.Infrastructure.Tests.Persistence.Outbox;

public class OutboxJsonTests
{
    [Fact]
    public void SerializerOptions_RoundTripsEventWithStronglyTypedId()
    {
        GuidId aggregateId = GuidId.New();
        var domainEvent = new TestAggregateRenamed(aggregateId, "renamed");

        string json = JsonSerializer.Serialize(domainEvent, OutboxJson.SerializerOptions);
        TestAggregateRenamed? rehydrated = JsonSerializer.Deserialize<TestAggregateRenamed>(json, OutboxJson.SerializerOptions);

        Assert.NotNull(rehydrated);
        Assert.Equal(aggregateId, rehydrated!.AggregateId);
        Assert.Equal("renamed", rehydrated.NewName);
    }

    [Fact]
    public void SerializerOptions_WritesStronglyTypedIdAsPrimitiveString()
    {
        var domainEvent = new TestAggregateRenamed(GuidId.New(), "renamed");

        string json = JsonSerializer.Serialize(domainEvent, OutboxJson.SerializerOptions);

        using JsonDocument document = JsonDocument.Parse(json);
        Assert.Equal(JsonValueKind.String, document.RootElement.GetProperty("aggregateId").ValueKind);
    }
}
