using FluentAssertions;
using FluentValidation.Results;
using LastHour.Modules.Merchant.Application.Commands;

namespace LastHour.Modules.Merchant.Tests.Application.Validation;

public class MerchantLifecycleCommandValidatorTests
{
    [Fact]
    public void VerifyMerchantValidator_ValidCommand_PassesValidation()
    {
        var validator = new VerifyMerchantCommandValidator();
        var command = new VerifyMerchantCommand(Guid.NewGuid());

        ValidationResult result = validator.Validate(command);

        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void VerifyMerchantValidator_EmptyMerchantId_FailsWithMerchantIdRequired()
    {
        var validator = new VerifyMerchantCommandValidator();
        var command = new VerifyMerchantCommand(Guid.Empty);

        ValidationResult result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorCode == "MerchantIdRequired");
    }

    [Fact]
    public void SuspendMerchantValidator_ValidCommand_PassesValidation()
    {
        var validator = new SuspendMerchantCommandValidator();
        var command = new SuspendMerchantCommand(Guid.NewGuid());

        ValidationResult result = validator.Validate(command);

        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void SuspendMerchantValidator_EmptyMerchantId_FailsWithMerchantIdRequired()
    {
        var validator = new SuspendMerchantCommandValidator();
        var command = new SuspendMerchantCommand(Guid.Empty);

        ValidationResult result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorCode == "MerchantIdRequired");
    }

    [Fact]
    public void ActivateMerchantValidator_ValidCommand_PassesValidation()
    {
        var validator = new ActivateMerchantCommandValidator();
        var command = new ActivateMerchantCommand(Guid.NewGuid());

        ValidationResult result = validator.Validate(command);

        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void ActivateMerchantValidator_EmptyMerchantId_FailsWithMerchantIdRequired()
    {
        var validator = new ActivateMerchantCommandValidator();
        var command = new ActivateMerchantCommand(Guid.Empty);

        ValidationResult result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorCode == "MerchantIdRequired");
    }

    [Fact]
    public void CloseMerchantValidator_ValidCommand_PassesValidation()
    {
        var validator = new CloseMerchantCommandValidator();
        var command = new CloseMerchantCommand(Guid.NewGuid());

        ValidationResult result = validator.Validate(command);

        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void CloseMerchantValidator_EmptyMerchantId_FailsWithMerchantIdRequired()
    {
        var validator = new CloseMerchantCommandValidator();
        var command = new CloseMerchantCommand(Guid.Empty);

        ValidationResult result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorCode == "MerchantIdRequired");
    }
}
