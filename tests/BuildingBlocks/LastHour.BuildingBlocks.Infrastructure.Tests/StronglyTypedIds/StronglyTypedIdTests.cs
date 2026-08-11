using LastHour.BuildingBlocks.SharedKernel.Domain.StronglyTypedIds;

namespace LastHour.BuildingBlocks.Infrastructure.Tests.StronglyTypedIds;

public class StronglyTypedIdTests
{
    private static readonly Guid SampleGuid = Guid.Parse("6b29fc40-ca47-1067-b31d-00dd010662da");

    [Fact]
    public void Create_ReturnsIdWithValue()
    {
        GuidId id = GuidId.Create(SampleGuid);

        Assert.Equal(SampleGuid, id.Value);
    }

    [Fact]
    public void Create_WithEmptyGuid_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => GuidId.Create(Guid.Empty));
    }

    [Fact]
    public void Create_WithZeroInt_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => IntId.Create(0));
    }

    [Fact]
    public void Create_WithPositiveInt_ReturnsIdWithValue()
    {
        IntId id = IntId.Create(42);

        Assert.Equal(42, id.Value);
    }

    [Fact]
    public void New_ReturnsNonEmptyId()
    {
        GuidId id = GuidId.New();

        Assert.NotEqual(Guid.Empty, id.Value);
    }

    [Fact]
    public void TwoIds_WithSameValue_AreEqual()
    {
        GuidId first = GuidId.Create(SampleGuid);
        GuidId second = GuidId.Create(SampleGuid);

        Assert.Equal(first, second);
        Assert.True(first == second);
        Assert.False(first != second);
    }

    [Fact]
    public void TwoIds_WithDifferentValue_AreNotEqual()
    {
        GuidId first = GuidId.Create(SampleGuid);
        GuidId second = GuidId.New();

        Assert.NotEqual(first, second);
        Assert.True(first != second);
        Assert.False(first == second);
    }

    [Fact]
    public void Ids_OfDifferentTypes_WithSameValue_AreNotEqual()
    {
        TestId testId = TestId.Create(SampleGuid);
        GuidId guidId = GuidId.Create(SampleGuid);

        Assert.False(guidId.Equals(testId));
    }

    [Fact]
    public void GetHashCode_IsConsistentWithEquality()
    {
        GuidId first = GuidId.Create(SampleGuid);
        GuidId second = GuidId.Create(SampleGuid);

        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Fact]
    public void ImplicitConversion_ToGuid_ReturnsValue()
    {
        GuidId id = GuidId.Create(SampleGuid);

        Guid value = id;

        Assert.Equal(SampleGuid, value);
    }

    [Fact]
    public void ImplicitConversion_ToInt_ReturnsValue()
    {
        IntId id = IntId.Create(42);

        int value = id;

        Assert.Equal(42, value);
    }

    [Fact]
    public void ToString_ReturnsValueString()
    {
        GuidId id = GuidId.Create(SampleGuid);

        Assert.Equal(SampleGuid.ToString(), id.ToString());
    }

    [Fact]
    public void Parse_ReturnsId()
    {
        GuidId id = GuidId.Parse(SampleGuid.ToString());

        Assert.Equal(SampleGuid, id.Value);
    }

    [Fact]
    public void Parse_IntId_ReturnsId()
    {
        IntId id = IntId.Parse("42");

        Assert.Equal(42, id.Value);
    }

    [Fact]
    public void Parse_InvalidValue_Throws()
    {
        Assert.Throws<FormatException>(() => GuidId.Parse("not-a-guid"));
    }

    [Fact]
    public void TryParse_InvalidValue_ReturnsFalse()
    {
        bool parsed = GuidId.TryParse("not-a-guid", out GuidId? result);

        Assert.False(parsed);
        Assert.Null(result);
    }

    [Fact]
    public void TryParse_EmptyString_ReturnsFalse()
    {
        bool parsed = GuidId.TryParse(string.Empty, out _);

        Assert.False(parsed);
    }

    [Fact]
    public void TryParse_EmptyGuid_ReturnsFalse()
    {
        bool parsed = GuidId.TryParse(Guid.Empty.ToString(), out _);

        Assert.False(parsed);
    }

    [Fact]
    public void TryParse_ValidValue_ReturnsTrue()
    {
        bool parsed = GuidId.TryParse(SampleGuid.ToString(), out GuidId? result);

        Assert.True(parsed);
        Assert.Equal(SampleGuid, result!.Value);
    }

    private sealed class TestId : StronglyTypedId<Guid, TestId>, IStronglyTypedId<Guid, TestId>
    {
        private TestId(Guid value)
            : base(value)
        {
        }

        public static TestId Create(Guid value) => new(value);
    }
}
