using LastHour.Domain.Common;
using LastHour.Domain.Exceptions;

namespace LastHour.Domain.ValueObjects;

public sealed class Money : ValueObject
{
    public decimal Amount { get; }
    public string Currency { get; }

    private Money() { Amount = 0; Currency = "USD"; }

    public Money(decimal amount, string currency = "USD")
    {
        if (amount < 0) throw new DomainException("Amount cannot be negative");
        if (string.IsNullOrWhiteSpace(currency)) throw new DomainException("Currency is required");
        Amount = decimal.Round(amount, 2, MidpointRounding.AwayFromZero);
        Currency = currency.ToUpperInvariant();
    }

    public Money Add(Money other)
    {
        EnsureSameCurrency(other);
        return new Money(Amount + other.Amount, Currency);
    }

    public Money Subtract(Money other)
    {
        EnsureSameCurrency(other);
        if (Amount < other.Amount) throw new DomainException("Insufficient funds");
        return new Money(Amount - other.Amount, Currency);
    }

    public Money MultiplyBy(int quantity)
    {
        return new Money(Amount * quantity, Currency);
    }

    public Money ApplyDiscount(decimal discountPercentage)
    {
        if (discountPercentage < 0 || discountPercentage > 100)
            throw new DomainException("Discount must be between 0 and 100");
        var discounted = Amount * (1 - discountPercentage / 100);
        return new Money(discounted, Currency);
    }

    private void EnsureSameCurrency(Money other)
    {
        if (!Currency.Equals(other.Currency, StringComparison.OrdinalIgnoreCase))
            throw new DomainException($"Currency mismatch: {Currency} vs {other.Currency}");
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Amount;
        yield return Currency;
    }

    public override string ToString() => $"{Currency} {Amount:F2}";
}
