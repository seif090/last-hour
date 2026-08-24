using FluentAssertions;
using FluentValidation.Results;
using LastHour.Modules.Catalog.Application.Commands;
using LastHour.Modules.Catalog.Domain.Products;

namespace LastHour.Modules.Catalog.Tests.Application.Validation;

public class ProductCommandValidatorTests
{
    [Fact]
    public void CreateProductValidator_ValidCommand_PassesValidation()
    {
        var validator = new CreateProductCommandValidator();
        var command = new CreateProductCommand(
            "SKU-001",
            "Valid Product",
            99.99m,
            "USD",
            Guid.NewGuid(),
            "Optional description");

        ValidationResult result = validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void CreateProductValidator_EmptySku_FailsValidation(string? sku)
    {
        var validator = new CreateProductCommandValidator();
        var command = new CreateProductCommand(
            sku!,
            "Valid Product",
            99.99m,
            "USD",
            Guid.NewGuid());

        ValidationResult result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorCode == "SkuRequired");
    }

    [Fact]
    public void CreateProductValidator_OverlongSku_FailsValidation()
    {
        var validator = new CreateProductCommandValidator();
        var command = new CreateProductCommand(
            new string('a', Sku.MaxLength + 1),
            "Valid Product",
            99.99m,
            "USD",
            Guid.NewGuid());

        ValidationResult result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorCode == "SkuTooLong");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void CreateProductValidator_EmptyName_FailsValidation(string? name)
    {
        var validator = new CreateProductCommandValidator();
        var command = new CreateProductCommand(
            "SKU-001",
            name!,
            99.99m,
            "USD",
            Guid.NewGuid());

        ValidationResult result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorCode == "ProductNameRequired");
    }

    [Fact]
    public void CreateProductValidator_OverlongName_FailsValidation()
    {
        var validator = new CreateProductCommandValidator();
        var command = new CreateProductCommand(
            "SKU-001",
            new string('a', Product.MaxNameLength + 1),
            99.99m,
            "USD",
            Guid.NewGuid());

        ValidationResult result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorCode == "ProductNameTooLong");
    }

    [Fact]
    public void CreateProductValidator_NegativePrice_FailsValidation()
    {
        var validator = new CreateProductCommandValidator();
        var command = new CreateProductCommand(
            "SKU-001",
            "Valid Product",
            -1m,
            "USD",
            Guid.NewGuid());

        ValidationResult result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorCode == "PriceAmountNegative");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void CreateProductValidator_EmptyCurrency_FailsValidation(string? currency)
    {
        var validator = new CreateProductCommandValidator();
        var command = new CreateProductCommand(
            "SKU-001",
            "Valid Product",
            99.99m,
            currency!,
            Guid.NewGuid());

        ValidationResult result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorCode == "PriceCurrencyRequired");
    }

    [Theory]
    [InlineData("US")]
    [InlineData("USDD")]
    public void CreateProductValidator_InvalidCurrencyLength_FailsValidation(string currency)
    {
        var validator = new CreateProductCommandValidator();
        var command = new CreateProductCommand(
            "SKU-001",
            "Valid Product",
            99.99m,
            currency,
            Guid.NewGuid());

        ValidationResult result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorCode == "PriceCurrencyInvalidLength");
    }

    [Fact]
    public void CreateProductValidator_EmptyCategoryId_FailsValidation()
    {
        var validator = new CreateProductCommandValidator();
        var command = new CreateProductCommand(
            "SKU-001",
            "Valid Product",
            99.99m,
            "USD",
            Guid.Empty);

        ValidationResult result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorCode == "CategoryIdRequired");
    }

    [Fact]
    public void CreateProductValidator_OverlongDescription_FailsValidation()
    {
        var validator = new CreateProductCommandValidator();
        var command = new CreateProductCommand(
            "SKU-001",
            "Valid Product",
            99.99m,
            "USD",
            Guid.NewGuid(),
            new string('a', Product.MaxDescriptionLength + 1));

        ValidationResult result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorCode == "ProductDescriptionTooLong");
    }

    [Fact]
    public void UpdateProductValidator_ValidCommand_PassesValidation()
    {
        var validator = new UpdateProductCommandValidator();
        var command = new UpdateProductCommand(Guid.NewGuid(), "Updated Product", "Description");

        ValidationResult result = validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void UpdateProductValidator_EmptyId_FailsValidation()
    {
        var validator = new UpdateProductCommandValidator();
        var command = new UpdateProductCommand(Guid.Empty, "Updated Product");

        ValidationResult result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorCode == "ProductIdRequired");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void UpdateProductValidator_EmptyName_FailsValidation(string? name)
    {
        var validator = new UpdateProductCommandValidator();
        var command = new UpdateProductCommand(Guid.NewGuid(), name!);

        ValidationResult result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorCode == "ProductNameRequired");
    }

    [Fact]
    public void UpdateProductValidator_OverlongName_FailsValidation()
    {
        var validator = new UpdateProductCommandValidator();
        var command = new UpdateProductCommand(
            Guid.NewGuid(),
            new string('a', Product.MaxNameLength + 1));

        ValidationResult result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorCode == "ProductNameTooLong");
    }

    [Fact]
    public void UpdateProductValidator_OverlongDescription_FailsValidation()
    {
        var validator = new UpdateProductCommandValidator();
        var command = new UpdateProductCommand(
            Guid.NewGuid(),
            "Valid Name",
            new string('a', Product.MaxDescriptionLength + 1));

        ValidationResult result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorCode == "ProductDescriptionTooLong");
    }

    [Fact]
    public void ChangeProductPriceValidator_ValidCommand_PassesValidation()
    {
        var validator = new ChangeProductPriceCommandValidator();
        var command = new ChangeProductPriceCommand(Guid.NewGuid(), 149.99m, "EUR");

        ValidationResult result = validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void ChangeProductPriceValidator_ValidZeroAmount_PassesValidation()
    {
        var validator = new ChangeProductPriceCommandValidator();
        var command = new ChangeProductPriceCommand(Guid.NewGuid(), 0m, "USD");

        ValidationResult result = validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void ChangeProductPriceValidator_EmptyProductId_FailsValidation()
    {
        var validator = new ChangeProductPriceCommandValidator();
        var command = new ChangeProductPriceCommand(Guid.Empty, 100m, "USD");

        ValidationResult result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorCode == "ProductIdRequired");
    }

    [Fact]
    public void ChangeProductPriceValidator_NegativeAmount_FailsValidation()
    {
        var validator = new ChangeProductPriceCommandValidator();
        var command = new ChangeProductPriceCommand(Guid.NewGuid(), -10m, "USD");

        ValidationResult result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorCode == "PriceAmountNegative");
    }

    [Theory]
    [InlineData("US")]
    [InlineData("USDD")]
    public void ChangeProductPriceValidator_InvalidCurrencyLength_FailsValidation(string currency)
    {
        var validator = new ChangeProductPriceCommandValidator();
        var command = new ChangeProductPriceCommand(Guid.NewGuid(), 100m, currency);

        ValidationResult result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorCode == "PriceCurrencyInvalidLength");
    }

    [Fact]
    public void ChangeProductSkuValidator_ValidCommand_PassesValidation()
    {
        var validator = new ChangeProductSkuCommandValidator();
        var command = new ChangeProductSkuCommand(Guid.NewGuid(), "VALID-SKU-001");

        ValidationResult result = validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void ChangeProductSkuValidator_EmptyProductId_FailsValidation()
    {
        var validator = new ChangeProductSkuCommandValidator();
        var command = new ChangeProductSkuCommand(Guid.Empty, "VALID-SKU-001");

        ValidationResult result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorCode == "ProductIdRequired");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ChangeProductSkuValidator_EmptySku_FailsValidation(string? sku)
    {
        var validator = new ChangeProductSkuCommandValidator();
        var command = new ChangeProductSkuCommand(Guid.NewGuid(), sku!);

        ValidationResult result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorCode == "SkuRequired");
    }

    [Fact]
    public void ChangeProductSkuValidator_OverlongSku_FailsValidation()
    {
        var validator = new ChangeProductSkuCommandValidator();
        var command = new ChangeProductSkuCommand(Guid.NewGuid(), new string('a', Sku.MaxLength + 1));

        ValidationResult result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorCode == "SkuTooLong");
    }

    [Fact]
    public void ChangeProductCategoryValidator_ValidCommand_PassesValidation()
    {
        var validator = new ChangeProductCategoryCommandValidator();
        var command = new ChangeProductCategoryCommand(Guid.NewGuid(), Guid.NewGuid());

        ValidationResult result = validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void ChangeProductCategoryValidator_EmptyProductId_FailsValidation()
    {
        var validator = new ChangeProductCategoryCommandValidator();
        var command = new ChangeProductCategoryCommand(Guid.Empty, Guid.NewGuid());

        ValidationResult result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorCode == "ProductIdRequired");
    }

    [Fact]
    public void ChangeProductCategoryValidator_EmptyCategoryId_FailsValidation()
    {
        var validator = new ChangeProductCategoryCommandValidator();
        var command = new ChangeProductCategoryCommand(Guid.NewGuid(), Guid.Empty);

        ValidationResult result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorCode == "CategoryIdRequired");
    }

    [Fact]
    public void DeleteProductValidator_ValidCommand_PassesValidation()
    {
        var validator = new DeleteProductCommandValidator();
        var command = new DeleteProductCommand(Guid.NewGuid());

        ValidationResult result = validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void DeleteProductValidator_EmptyProductId_FailsValidation()
    {
        var validator = new DeleteProductCommandValidator();
        var command = new DeleteProductCommand(Guid.Empty);

        ValidationResult result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorCode == "ProductIdRequired");
    }
}
