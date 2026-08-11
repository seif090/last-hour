using System.Text.Json;
using LastHour.BuildingBlocks.Infrastructure.StronglyTypedIds;
using LastHour.BuildingBlocks.SharedKernel.Domain.StronglyTypedIds;

namespace LastHour.BuildingBlocks.Infrastructure.Tests.StronglyTypedIds;

public class StronglyTypedIdJsonTests
{
    private static readonly Guid SampleGuid = Guid.Parse("6b29fc40-ca47-1067-b31d-00dd010662da");

    private static JsonSerializerOptions CreateOptions()
    {
        var options = new JsonSerializerOptions();
        options.Converters.Add(new StronglyTypedIdJsonConverterFactory());
        return options;
    }

    [Fact]
    public void Serialize_GuidId_WritesStringValue()
    {
        GuidId id = GuidId.Create(SampleGuid);

        string json = JsonSerializer.Serialize(id, CreateOptions());

        Assert.Equal($"\"{SampleGuid}\"", json);
    }

    [Fact]
    public void Serialize_IntId_WritesStringValue()
    {
        IntId id = IntId.Create(42);

        string json = JsonSerializer.Serialize(id, CreateOptions());

        Assert.Equal("\"42\"", json);
    }

    [Fact]
    public void Deserialize_StringValue_ReturnsId()
    {
        GuidId? id = JsonSerializer.Deserialize<GuidId>($"\"{SampleGuid}\"", CreateOptions());

        Assert.NotNull(id);
        Assert.Equal(SampleGuid, id!.Value);
    }

    [Fact]
    public void Deserialize_IntStringValue_ReturnsId()
    {
        IntId? id = JsonSerializer.Deserialize<IntId>("\"42\"", CreateOptions());

        Assert.NotNull(id);
        Assert.Equal(42, id!.Value);
    }

    [Fact]
    public void Deserialize_Null_ReturnsNull()
    {
        GuidId? id = JsonSerializer.Deserialize<GuidId>("null", CreateOptions());

        Assert.Null(id);
    }

    [Fact]
    public void Deserialize_InvalidValue_Throws()
    {
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<GuidId>("\"not-a-guid\"", CreateOptions()));
    }

    [Fact]
    public void RoundTrip_PreservesValue()
    {
        GuidId id = GuidId.Create(SampleGuid);
        JsonSerializerOptions options = CreateOptions();

        string json = JsonSerializer.Serialize(id, options);
        GuidId? deserialized = JsonSerializer.Deserialize<GuidId>(json, options);

        Assert.Equal(id, deserialized);
    }
}
