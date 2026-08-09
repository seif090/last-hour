using LastHour.BuildingBlocks.SharedKernel.Domain;

namespace LastHour.BuildingBlocks.SharedKernel.Tests.Domain;

public class ValueObjectTests
{
    [Fact]
    public void Equality_TwoObjectsWithSameComponents_AreEqual()
    {
        var left = new TestValueObject("street", "NYC");
        var right = new TestValueObject("street", "NYC");

        Assert.True(left == right);
        Assert.False(left != right);
        Assert.True(left.Equals(right));
        Assert.True(left.Equals((object)right));
        Assert.Equal(left.GetHashCode(), right.GetHashCode());
    }

    [Fact]
    public void Equality_DifferentComponents_AreNotEqual()
    {
        var left = new TestValueObject("street", "NYC");
        var right = new TestValueObject("avenue", "NYC");

        Assert.NotEqual(left, right);
        Assert.True(left != right);
    }

    [Fact]
    public void Equality_NullComponentVersusValue_AreNotEqual()
    {
        var withNull = new TestValueObject("street", null);
        var withValue = new TestValueObject("street", "NYC");

        Assert.NotEqual(withNull, withValue);
    }

    [Fact]
    public void Equality_NullComponentVersusNullComponent_AreEqual()
    {
        var left = new TestValueObject("street", null);
        var right = new TestValueObject("street", null);

        Assert.Equal(left, right);
        Assert.Equal(left.GetHashCode(), right.GetHashCode());
    }

    [Fact]
    public void Equality_ComparedToNull_AreNotEqual()
    {
        var valueObject = new TestValueObject("street", "NYC");

        Assert.False(valueObject == null);
        Assert.True(valueObject != null);
        Assert.False(valueObject.Equals(null));
    }

    [Fact]
    public void Equality_ComparedToNonValueObject_AreNotEqual()
    {
        var valueObject = new TestValueObject("street", "NYC");

        Assert.False(valueObject.Equals((object)"street"));
    }

    [Fact]
    public void Equality_StaticNullToNull_IsEqual()
    {
        TestValueObject? left = null;
        TestValueObject? right = null;

        Assert.True(left == right);
        Assert.False(left != right);
    }

    private sealed class TestValueObject : ValueObject
    {
        public TestValueObject(string street, string? city)
        {
            Street = street;
            City = city;
        }

        public string Street { get; }

        public string? City { get; }

        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return Street;
            yield return City!;
        }
    }
}
