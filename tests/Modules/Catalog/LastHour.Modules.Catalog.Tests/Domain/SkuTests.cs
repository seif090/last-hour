using FluentAssertions;
using LastHour.Modules.Catalog.Domain.Products;

namespace LastHour.Modules.Catalog.Tests.Domain;

public class SkuTests
{
    [Fact]
    public void From_ValidSku_TrimsAndNormalizes()
    {
        Sku sku = Sku.From("  sku-abc_123  ");

        sku.Value.Should().Be("sku-abc_123");
        sku.NormalizedValue.Should().Be("SKU-ABC_123");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void From_NullOrWhiteSpace_ThrowsInvalidSkuException(string? value)
    {
        Action act = () => Sku.From(value);

        act.Should().Throw<InvalidSkuException>()
            .WithMessage("A SKU is required.");
    }

    [Fact]
    public void From_OverlongSku_ThrowsInvalidSkuException()
    {
        string overlong = new string('A', Sku.MaxLength + 1);

        Action act = () => Sku.From(overlong);

        act.Should().Throw<InvalidSkuException>()
            .WithMessage($"A SKU cannot exceed {Sku.MaxLength} characters.");
    }

    [Theory]
    [InlineData("SKU 123")]
    [InlineData("SKU@123")]
    [InlineData("SKU.123")]
    [InlineData("SKU#123")]
    public void From_InvalidCharacters_ThrowsInvalidSkuException(string invalidSku)
    {
        Action act = () => Sku.From(invalidSku);

        act.Should().Throw<InvalidSkuException>()
            .WithMessage("A SKU may only contain letters, numbers, hyphens, and underscores.");
    }

    [Fact]
    public void Equality_SameSkuDifferentCase_AreEqual()
    {
        Sku sku1 = Sku.From("PROD-001");
        Sku sku2 = Sku.From("prod-001");

        sku1.Should().Be(sku2);
        (sku1 == sku2).Should().BeTrue();
    }

    [Fact]
    public void Equality_DifferentSku_AreNotEqual()
    {
        Sku sku1 = Sku.From("PROD-001");
        Sku sku2 = Sku.From("PROD-002");

        sku1.Should().NotBe(sku2);
        (sku1 != sku2).Should().BeTrue();
    }

    [Fact]
    public void ToString_ReturnsSuppliedValue()
    {
        Sku sku = Sku.From("prod-123");

        sku.ToString().Should().Be("prod-123");
    }
}
