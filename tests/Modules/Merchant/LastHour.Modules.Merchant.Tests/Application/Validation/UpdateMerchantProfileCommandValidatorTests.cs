using FluentAssertions;
using FluentValidation.Results;
using LastHour.Modules.Merchant.Application.Commands;
using LastHour.Modules.Merchant.Domain.Merchants;

namespace LastHour.Modules.Merchant.Tests.Application.Validation;

public class UpdateMerchantProfileCommandValidatorTests
{
    private readonly UpdateMerchantProfileCommandValidator _validator = new();

    private static UpdateMerchantProfileCommand CreateCommand(
        Guid? merchantId = null,
        Guid? callerUserId = null,
        string name = "Valid Merchant Name",
        string street = "123 Main Street",
        string city = "Seattle",
        string? stateOrProvince = "WA",
        string? postalCode = "98101",
        string country = "USA",
        string phone = "+1-206-555-0100")
        => new(
            merchantId ?? Guid.NewGuid(),
            callerUserId ?? Guid.NewGuid(),
            name,
            street,
            city,
            stateOrProvince,
            postalCode,
            country,
            phone);

    [Fact]
    public void Validate_ValidCommand_PassesValidation()
    {
        var command = CreateCommand();

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void Validate_EmptyMerchantId_FailsWithMerchantIdRequired()
    {
        var command = CreateCommand(merchantId: Guid.Empty);

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorCode == "MerchantIdRequired");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_EmptyOrWhitespaceName_FailsWithMerchantNameRequired(string name)
    {
        var command = CreateCommand(name: name);

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorCode == "MerchantNameRequired");
    }

    [Fact]
    public void Validate_NameMaxLength_PassesValidation()
    {
        string name = new('a', MerchantName.MaxLength);
        var command = CreateCommand(name: name);

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_OverlongName_FailsWithMerchantNameTooLong()
    {
        string name = new('a', MerchantName.MaxLength + 1);
        var command = CreateCommand(name: name);

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorCode == "MerchantNameTooLong");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_EmptyOrWhitespaceStreet_FailsWithStreetRequired(string street)
    {
        var command = CreateCommand(street: street);

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorCode == "StreetRequired");
    }

    [Fact]
    public void Validate_StreetMaxLength_PassesValidation()
    {
        string street = new('a', 200);
        var command = CreateCommand(street: street);

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_OverlongStreet_FailsWithStreetTooLong()
    {
        string street = new('a', 201);
        var command = CreateCommand(street: street);

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorCode == "StreetTooLong");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_EmptyOrWhitespaceCity_FailsWithCityRequired(string city)
    {
        var command = CreateCommand(city: city);

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorCode == "CityRequired");
    }

    [Fact]
    public void Validate_CityMaxLength_PassesValidation()
    {
        string city = new('a', 100);
        var command = CreateCommand(city: city);

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_OverlongCity_FailsWithCityTooLong()
    {
        string city = new('a', 101);
        var command = CreateCommand(city: city);

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorCode == "CityTooLong");
    }

    [Fact]
    public void Validate_NullStateOrProvince_PassesValidation()
    {
        var command = CreateCommand(stateOrProvince: null);

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_StateOrProvinceMaxLength_PassesValidation()
    {
        string state = new('a', 100);
        var command = CreateCommand(stateOrProvince: state);

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_OverlongStateOrProvince_FailsWithStateOrProvinceTooLong()
    {
        string state = new('a', 101);
        var command = CreateCommand(stateOrProvince: state);

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorCode == "StateOrProvinceTooLong");
    }

    [Fact]
    public void Validate_NullPostalCode_PassesValidation()
    {
        var command = CreateCommand(postalCode: null);

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_PostalCodeMaxLength_PassesValidation()
    {
        string postalCode = new('a', 20);
        var command = CreateCommand(postalCode: postalCode);

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_OverlongPostalCode_FailsWithPostalCodeTooLong()
    {
        string postalCode = new('a', 21);
        var command = CreateCommand(postalCode: postalCode);

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorCode == "PostalCodeTooLong");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_EmptyOrWhitespaceCountry_FailsWithCountryRequired(string country)
    {
        var command = CreateCommand(country: country);

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorCode == "CountryRequired");
    }

    [Fact]
    public void Validate_CountryMaxLength_PassesValidation()
    {
        string country = new('a', 100);
        var command = CreateCommand(country: country);

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_OverlongCountry_FailsWithCountryTooLong()
    {
        string country = new('a', 101);
        var command = CreateCommand(country: country);

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorCode == "CountryTooLong");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_EmptyOrWhitespacePhone_FailsWithPhoneNumberRequired(string phone)
    {
        var command = CreateCommand(phone: phone);

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorCode == "PhoneNumberRequired");
    }

    [Fact]
    public void Validate_PhoneMaxLength_PassesValidation()
    {
        string phone = new('1', PhoneNumber.MaxLength);
        var command = CreateCommand(phone: phone);

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_OverlongPhone_FailsWithPhoneNumberTooLong()
    {
        string phone = new('1', PhoneNumber.MaxLength + 1);
        var command = CreateCommand(phone: phone);

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorCode == "PhoneNumberTooLong");
    }
}
