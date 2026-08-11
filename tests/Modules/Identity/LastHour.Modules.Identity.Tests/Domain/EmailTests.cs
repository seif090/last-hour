using FluentAssertions;
using LastHour.Modules.Identity.Domain.Users;

namespace LastHour.Modules.Identity.Tests.Domain;

public class EmailTests
{
    [Theory]
    [InlineData("user@example.com")]
    [InlineData("User@Example.com")]
    [InlineData("  user@example.com  ")]
    [InlineData("first.last+tag@sub.example.co.uk")]
    public void From_ValidAddress_CreatesEmail(string value)
    {
        Email email = Email.From(value);

        email.Value.Should().Be(value.Trim());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void From_NullOrWhitespace_ThrowsInvalidEmail(string? value)
    {
        Action act = () => Email.From(value);

        act.Should().Throw<InvalidEmailException>();
    }

    [Theory]
    [InlineData("not-an-email")]
    [InlineData("missing-at-sign")]
    [InlineData("user@")]
    [InlineData("user name@example.com")]
    public void From_InvalidFormat_ThrowsInvalidEmail(string value)
    {
        Action act = () => Email.From(value);

        act.Should().Throw<InvalidEmailException>();
    }

    [Fact]
    public void From_AddressExceedingMaxLength_ThrowsInvalidEmail()
    {
        string longAddress = $"{new string('a', Email.MaxLength)}@example.com";

        Action act = () => Email.From(longAddress);

        act.Should().Throw<InvalidEmailException>();
    }

    [Fact]
    public void NormalizedValue_IsTrimmedAndLowerCased()
    {
        Email email = Email.From("  User@Example.COM  ");

        email.NormalizedValue.Should().Be("user@example.com");
    }

    [Fact]
    public void Normalize_CentralizedRule_TrimsAndLowerCases()
    {
        Email.Normalize("  User@Example.COM  ").Should().Be("user@example.com");
    }

    [Fact]
    public void Equality_DifferentCase_IsNotEqualByValueButNormalizesEqually()
    {
        Email upper = Email.From("User@Example.com");
        Email lower = Email.From("user@example.com");

        upper.Should().NotBe(lower);
        upper.NormalizedValue.Should().Be(lower.NormalizedValue);
    }

    [Fact]
    public void Equality_SameValue_IsEqual()
    {
        Email first = Email.From("user@example.com");
        Email second = Email.From("user@example.com");

        first.Should().Be(second);
        first.GetHashCode().Should().Be(second.GetHashCode());
    }
}
