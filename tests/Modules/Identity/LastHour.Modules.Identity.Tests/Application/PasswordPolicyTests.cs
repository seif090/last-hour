using FluentAssertions;
using LastHour.BuildingBlocks.SharedKernel.Results;
using LastHour.Modules.Identity.Application;
using LastHour.Modules.Identity.Tests.Application.TestSupport;
using Microsoft.Extensions.Options;

namespace LastHour.Modules.Identity.Tests.Application;

public class PasswordPolicyTests
{
    private static PasswordCredentialService CreateService(PasswordPolicy? policy = null)
        => new(new StubPasswordHasher(), Options.Create(policy ?? new PasswordPolicy()));

    [Fact]
    public void ValidatePassword_DefaultPolicy_AcceptsTwelveCharacterPassword()
    {
        PasswordCredentialService service = CreateService();

        Result result = service.ValidatePassword("correct horse battery staple");

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void ValidatePassword_DefaultPolicy_RejectsTooShortPassword()
    {
        PasswordCredentialService service = CreateService();

        Result result = service.ValidatePassword("too-short");

        result.IsFailure.Should().BeTrue();
        result.FirstError!.Code.Should().Be("Identity.PasswordTooShort");
    }

    [Fact]
    public void ValidatePassword_RejectsNullEmptyAndWhitespace()
    {
        PasswordCredentialService service = CreateService();

        service.ValidatePassword(null).FirstError!.Code.Should().Be("Identity.PasswordRequired");
        service.ValidatePassword(string.Empty).FirstError!.Code.Should().Be("Identity.PasswordRequired");
        service.ValidatePassword("           ").FirstError!.Code.Should().Be("Identity.PasswordRequired");
    }

    [Fact]
    public void ValidatePassword_CustomPolicy_EnforcesConfiguredBoundaries()
    {
        var policy = new PasswordPolicy { MinimumLength = 8, MaximumLength = 16 };
        PasswordCredentialService service = CreateService(policy);

        service.ValidatePassword("seven77").FirstError!.Code.Should().Be("Identity.PasswordTooShort");
        service.ValidatePassword("12345678").IsSuccess.Should().BeTrue();
        service.ValidatePassword("12345678901234567890").FirstError!.Code.Should().Be("Identity.PasswordTooLong");
    }

    [Fact]
    public void ValidatePassword_UnicodePassword_LengthCountsUtf16CodeUnits()
    {
        // Six emoji are 12 UTF-16 code units, which satisfies a minimum length of 12. This
        // documents the policy semantics: length is measured in string code units, exactly as
        // the hasher receives them.
        var policy = new PasswordPolicy { MinimumLength = 12, MaximumLength = 128 };
        PasswordCredentialService service = CreateService(policy);

        service.ValidatePassword("🔐🔐🔐🔐🔐🔐").IsSuccess.Should().BeTrue();
        service.ValidatePassword("🔐🔐🔐🔐🔐").FirstError!.Code.Should().Be("Identity.PasswordTooShort");
    }

    [Fact]
    public void PasswordPolicyValidator_RejectsInvalidPolicies()
    {
        var validator = new PasswordPolicyValidator();

        ValidateOptionsResult result = validator.Validate(null, new PasswordPolicy { MinimumLength = 6 });
        result.Failed.Should().BeTrue();
        result.FailureMessage.Should().Contain("MinimumLength");

        result = validator.Validate(null, new PasswordPolicy { MaximumLength = 2048 });
        result.Failed.Should().BeTrue();
        result.FailureMessage.Should().Contain("MaximumLength");

        result = validator.Validate(null, new PasswordPolicy { MinimumLength = 20, MaximumLength = 12 });
        result.Failed.Should().BeTrue();
        result.FailureMessage.Should().Contain("MinimumLength");
    }

    [Fact]
    public void PasswordPolicyValidator_AcceptsValidPolicy()
    {
        var validator = new PasswordPolicyValidator();

        ValidateOptionsResult result = validator.Validate(null, new PasswordPolicy());

        result.Succeeded.Should().BeTrue();
    }
}
