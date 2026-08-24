using System.Text.RegularExpressions;
using LastHour.BuildingBlocks.SharedKernel.Domain;

namespace LastHour.Modules.Catalog.Domain.Products;

/// <summary>
/// Represents a monetary value consisting of a decimal amount and an ISO 4217 currency code.
/// Modeled as an immutable value object.
/// </summary>
public sealed class Money : ValueObject
{
    private static readonly Regex CurrencyPattern = new Regex(
        @"^[A-Z]{3}$",
        RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private readonly decimal _amount;
    private readonly string _currency;

    private Money(decimal amount, string currency)
    {
        _amount = amount;
        _currency = currency;
    }

    /// <summary>
    /// Gets the monetary amount.
    /// </summary>
    public decimal Amount => _amount;

    /// <summary>
    /// Gets the ISO 4217 3-letter currency code (upper-case).
    /// </summary>
    public string Currency => _currency;

    /// <summary>
    /// Creates a new <see cref="Money"/> value object.
    /// </summary>
    /// <param name="amount">The non-negative price amount.</param>
    /// <param name="currency">The 3-letter ISO currency code.</param>
    /// <returns>A new <see cref="Money"/> instance.</returns>
    /// <exception cref="InvalidPriceException">The amount is negative or the currency code is invalid.</exception>
    public static Money Create(decimal amount, string currency)
    {
        if (amount < 0)
        {
            throw new InvalidPriceException("Price amount cannot be negative.");
        }

        if (string.IsNullOrWhiteSpace(currency))
        {
            throw new InvalidPriceException("Currency code is required.");
        }

        string normalizedCurrency = currency.Trim().ToUpperInvariant();
        if (!CurrencyPattern.IsMatch(normalizedCurrency))
        {
            throw new InvalidPriceException($"Currency code '{currency}' must be a valid 3-letter ISO 4217 code.");
        }

        return new Money(amount, normalizedCurrency);
    }

    /// <summary>
    /// Creates a <see cref="Money"/> instance with zero amount.
    /// </summary>
    /// <param name="currency">The 3-letter ISO currency code.</param>
    /// <returns>A zero <see cref="Money"/> instance.</returns>
    public static Money Zero(string currency) => Create(0m, currency);

    /// <inheritdoc/>
    public override string ToString() => $"{_amount:0.00} {_currency}";

    /// <inheritdoc/>
    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return _amount;
        yield return _currency;
    }
}
