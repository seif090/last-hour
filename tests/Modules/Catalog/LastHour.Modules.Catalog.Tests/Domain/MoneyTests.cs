using FluentAssertions;
using LastHour.Modules.Catalog.Domain.Products;

namespace LastHour.Modules.Catalog.Tests.Domain;

public class MoneyTests
{
    [Fact]
    public void Create_ValidAmountAndCurrency_SetsProperties()
    {
        Money money = Money.Create(99.99m, "usd");

        money.Amount.Should().Be(99.99m);
        money.Currency.Should().Be("USD");
    }

    [Fact]
    public void Create_NegativeAmount_ThrowsInvalidPriceException()
    {
        Action act = () => Money.Create(-1m, "EUR");

        act.Should().Throw<InvalidPriceException>()
            .WithMessage("Price amount cannot be negative.");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_NullOrWhiteSpaceCurrency_ThrowsInvalidPriceException(string? currency)
    {
        Action act = () => Money.Create(10m, currency!);

        act.Should().Throw<InvalidPriceException>()
            .WithMessage("Currency code is required.");
    }

    [Theory]
    [InlineData("US")]
    [InlineData("USDD")]
    [InlineData("12A")]
    [InlineData("U$D")]
    public void Create_InvalidCurrencyFormat_ThrowsInvalidPriceException(string currency)
    {
        Action act = () => Money.Create(10m, currency);

        act.Should().Throw<InvalidPriceException>()
            .WithMessage($"Currency code '{currency}' must be a valid 3-letter ISO 4217 code.");
    }

    [Fact]
    public void Zero_ReturnsZeroAmountWithSpecifiedCurrency()
    {
        Money zero = Money.Zero("GBP");

        zero.Amount.Should().Be(0m);
        zero.Currency.Should().Be("GBP");
    }

    [Fact]
    public void Equality_SameAmountAndCurrency_AreEqual()
    {
        Money money1 = Money.Create(19.99m, "EUR");
        Money money2 = Money.Create(19.99m, "eur");

        money1.Should().Be(money2);
        (money1 == money2).Should().BeTrue();
    }

    [Fact]
    public void Equality_DifferentAmountOrCurrency_AreNotEqual()
    {
        Money money1 = Money.Create(19.99m, "EUR");
        Money money2 = Money.Create(29.99m, "EUR");
        Money money3 = Money.Create(19.99m, "USD");

        money1.Should().NotBe(money2);
        money1.Should().NotBe(money3);
    }

    [Fact]
    public void ToString_ReturnsFormattedAmountAndCurrency()
    {
        Money money = Money.Create(19.5m, "EUR");

        money.ToString().Should().Be("19.50 EUR");
    }
}
