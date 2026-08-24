using FluentAssertions;
using FluentValidation.Results;
using LastHour.Modules.Merchant.Application.Commands;
using LastHour.Modules.Merchant.Domain.Merchants;

namespace LastHour.Modules.Merchant.Tests.Application.Validation;

public class RegisterMerchantCommandValidatorTests
{
    private readonly RegisterMerchantCommandValidator _validator = new();

    [Fact]
    public void Validate_ValidCommand_PassesValidation()
    {
        var command = new RegisterMerchantCommand(
            Guid.NewGuid(),
            "Valid Merchant Name",
            "123 Main Street",
            "Seattle",
            "WA",
            "98101",
            "USA",
            "+1-206-555-0100");

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void Validate_EmptyOwnerId_FailsWithOwnerIdRequired()
    {
        var command = new RegisterMerchantCommand(
            Guid.Empty,
            "Valid Merchant Name",
            "123 Main Street",
            "Seattle",
            "WA",
            "98101",
            "USA",
            "+1-206-555-0100");

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorCode == "OwnerIdRequired");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_EmptyOrWhitespaceName_FailsWithMerchantNameRequired(string name)
    {
        var command = new RegisterMerchantCommand(
            Guid.NewGuid(),
            name,
            "123 Main Street",
            "Seattle",
            "WA",
            "98101",
            "USA",
            "+1-206-555-0100");

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorCode == "MerchantNameRequired");
    }

    [Fact]
    public void Validate_NameMaxLength_PassesValidation()
    {
        string name = new('a', MerchantName.MaxLength);
        var command = new RegisterMerchantCommand(
            Guid.NewGuid(),
            name,
            "123 Main Street",
            "Seattle",
            "WA",
            "98101",
            "USA",
            "+1-206-555-0100");

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_OverlongName_FailsWithMerchantNameTooLong()
    {
        string name = new('a', MerchantName.MaxLength + 1);
        var command = new RegisterMerchantCommand(
            Guid.NewGuid(),
            name,
            "123 Main Street",
            "Seattle",
            "WA",
            "98101",
            "USA",
            "+1-206-555-0100");

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorCode == "MerchantNameTooLong");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_EmptyOrWhitespaceStreet_FailsWithStreetRequired(string street)
    {
        var command = new RegisterMerchantCommand(
            Guid.NewGuid(),
            "Valid Merchant Name",
            street,
            "Seattle",
            "WA",
            "98101",
            "USA",
            "+1-206-555-0100");

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorCode == "StreetRequired");
    }

    [Fact]
    public void Validate_StreetMaxLength_PassesValidation()
    {
        string street = new('a', 200);
        var command = new RegisterMerchantCommand(
            Guid.NewGuid(),
            "Valid Merchant Name",
            street,
            "Seattle",
            "WA",
            "98101",
            "USA",
            "+1-206-555-0100");

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_OverlongStreet_FailsWithStreetTooLong()
    {
        string street = new('a', 201);
        var command = new RegisterMerchantCommand(
            Guid.NewGuid(),
            "Valid Merchant Name",
            street,
            "Seattle",
            "WA",
            "98101",
            "USA",
            "+1-206-555-0100");

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorCode == "StreetTooLong");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_EmptyOrWhitespaceCity_FailsWithCityRequired(string city)
    {
        var command = new RegisterMerchantCommand(
            Guid.NewGuid(),
            "Valid Merchant Name",
            "123 Main Street",
            city,
            "WA",
            "98101",
            "USA",
            "+1-206-555-0100");

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorCode == "CityRequired");
    }

    [Fact]
    public void Validate_CityMaxLength_PassesValidation()
    {
        string city = new('a', 100);
        var command = new RegisterMerchantCommand(
            Guid.NewGuid(),
            "Valid Merchant Name",
            "123 Main Street",
            city,
            "WA",
            "98101",
            "USA",
            "+1-206-555-0100");

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_OverlongCity_FailsWithCityTooLong()
    {
        string city = new('a', 101);
        var command = new RegisterMerchantCommand(
            Guid.NewGuid(),
            "Valid Merchant Name",
            "123 Main Street",
            city,
            "WA",
            "98101",
            "USA",
            "+1-206-555-0100");

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorCode == "CityTooLong");
    }

    [Fact]
    public void Validate_NullStateOrProvince_PassesValidation()
    {
        var command = new RegisterMerchantCommand(
            Guid.NewGuid(),
            "Valid Merchant Name",
            "123 Main Street",
            "Seattle",
            null,
            "98101",
            "USA",
            "+1-206-555-0100");

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_StateOrProvinceMaxLength_PassesValidation()
    {
        string state = new('a', 100);
        var command = new RegisterMerchantCommand(
            Guid.NewGuid(),
            "Valid Merchant Name",
            "123 Main Street",
            "Seattle",
            state,
            "98101",
            "USA",
            "+1-206-555-0100");

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_OverlongStateOrProvince_FailsWithStateOrProvinceTooLong()
    {
        string state = new('a', 101);
        var command = new RegisterMerchantCommand(
            Guid.NewGuid(),
            "Valid Merchant Name",
            "123 Main Street",
            "Seattle",
            state,
            "98101",
            "USA",
            "+1-206-555-0100");

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorCode == "StateOrProvinceTooLong");
    }

    [Fact]
    public void Validate_NullPostalCode_PassesValidation()
    {
        var command = new RegisterMerchantCommand(
            Guid.NewGuid(),
            "Valid Merchant Name",
            "123 Main Street",
            "Seattle",
            "WA",
            null,
            "USA",
            "+1-206-555-0100");

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_PostalCodeMaxLength_PassesValidation()
    {
        string postalCode = new('a', 20);
        var command = new RegisterMerchantCommand(
            Guid.NewGuid(),
            "Valid Merchant Name",
            "123 Main Street",
            "Seattle",
            "WA",
            postalCode,
            "USA",
            "+1-206-555-0100");

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_OverlongPostalCode_FailsWithPostalCodeTooLong()
    {
        string postalCode = new('a', 21);
        var command = new RegisterMerchantCommand(
            Guid.NewGuid(),
            "Valid Merchant Name",
            "123 Main Street",
            "Seattle",
            "WA",
            postalCode,
            "USA",
            "+1-206-555-0100");

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorCode == "PostalCodeTooLong");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_EmptyOrWhitespaceCountry_FailsWithCountryRequired(string country)
    {
        var command = new RegisterMerchantCommand(
            Guid.NewGuid(),
            "Valid Merchant Name",
            "123 Main Street",
            "Seattle",
            "WA",
            "98101",
            country,
            "+1-206-555-0100");

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorCode == "CountryRequired");
    }

    [Fact]
    public void Validate_CountryMaxLength_PassesValidation()
    {
        string country = new('a', 100);
        var command = new RegisterMerchantCommand(
            Guid.NewGuid(),
            "Valid Merchant Name",
            "123 Main Street",
            "Seattle",
            "WA",
            "98101",
            country,
            "+1-206-555-0100");

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_OverlongCountry_FailsWithCountryTooLong()
    {
        string country = new('a', 101);
        var command = new RegisterMerchantCommand(
            Guid.NewGuid(),
            "Valid Merchant Name",
            "123 Main Street",
            "Seattle",
            "WA",
            "98101",
            country,
            "+1-206-555-0100");

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorCode == "CountryTooLong");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_EmptyOrWhitespacePhone_FailsWithPhoneNumberRequired(string phone)
    {
        var command = new RegisterMerchantCommand(
            Guid.NewGuid(),
            "Valid Merchant Name",
            "123 Main Street",
            "Seattle",
            "WA",
            "98101",
            "USA",
            phone);

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorCode == "PhoneNumberRequired");
    }

    [Fact]
    public void Validate_PhoneMaxLength_PassesValidation()
    {
        string phone = new('1', PhoneNumber.MaxLength);
        var command = new RegisterMerchantCommand(
            Guid.NewGuid(),
            "Valid Merchant Name",
            "123 Main Street",
            "Seattle",
            "WA",
            "98101",
            "USA",
            phone);

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_OverlongPhone_FailsWithPhoneNumberTooLong()
    {
        string phone = new('1', PhoneNumber.MaxLength + 1);
        var command = new RegisterMerchantCommand(
            Guid.NewGuid(),
            "Valid Merchant Name",
            "123 Main Street",
            "Seattle",
            "WA",
            "98101",
            "USA",
            phone);

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorCode == "PhoneNumberTooLong");
    }
}
