using LastHour.BuildingBlocks.Infrastructure.StronglyTypedIds;
using LastHour.BuildingBlocks.SharedKernel.Domain.StronglyTypedIds;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace LastHour.BuildingBlocks.Infrastructure.Tests.StronglyTypedIds;

public class StronglyTypedIdValueConverterTests
{
    private static readonly Guid SampleGuid = Guid.Parse("6b29fc40-ca47-1067-b31d-00dd010662da");

    [Fact]
    public void Create_ReturnsValueConverter_OfExpectedTypes()
    {
        ValueConverter<GuidId, Guid> converter = StronglyTypedIdValueConverter.Create<Guid, GuidId>();

        Assert.IsType<ValueConverter<GuidId, Guid>>(converter);
    }

    [Fact]
    public void ConvertToProvider_ReturnsPrimitiveValue()
    {
        ValueConverter<GuidId, Guid> converter = StronglyTypedIdValueConverter.Create<Guid, GuidId>();
        GuidId id = GuidId.Create(SampleGuid);

        object? providerValue = converter.ConvertToProvider(id);

        Assert.Equal(SampleGuid, providerValue);
    }

    [Fact]
    public void ConvertFromProvider_ReturnsId()
    {
        ValueConverter<GuidId, Guid> converter = StronglyTypedIdValueConverter.Create<Guid, GuidId>();

        object? id = converter.ConvertFromProvider(SampleGuid);

        Assert.IsType<GuidId>(id);
        Assert.Equal(SampleGuid, Assert.IsType<GuidId>(id).Value);
    }

    [Fact]
    public void RoundTrip_PreservesId()
    {
        ValueConverter<GuidId, Guid> converter = StronglyTypedIdValueConverter.Create<Guid, GuidId>();
        GuidId id = GuidId.Create(SampleGuid);

        object? providerValue = converter.ConvertToProvider(id);
        object? restored = converter.ConvertFromProvider(providerValue);

        Assert.Equal(id, restored);
    }

    [Fact]
    public void RoundTrip_WithExplicitFactory_PreservesId()
    {
        ValueConverter<IntId, int> converter = StronglyTypedIdValueConverter.Create<int, IntId>(IntId.Create);
        IntId id = IntId.Create(42);

        object? providerValue = converter.ConvertToProvider(id);
        object? restored = converter.ConvertFromProvider(providerValue);

        Assert.Equal(id, restored);
    }
}
