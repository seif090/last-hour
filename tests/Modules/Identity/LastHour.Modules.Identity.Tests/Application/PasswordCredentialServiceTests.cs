using FluentAssertions;
using LastHour.BuildingBlocks.Application.Contracts;
using LastHour.BuildingBlocks.SharedKernel.Results;
using LastHour.Modules.Identity.Application;
using LastHour.Modules.Identity.Domain.Users;
using LastHour.Modules.Identity.Tests.Application.TestSupport;
using Microsoft.Extensions.Options;

namespace LastHour.Modules.Identity.Tests.Application;

public class PasswordCredentialServiceTests
{
    private const string Password = "correct horse battery staple";
    private const string OtherPassword = "a different valid phrase";

    private static PasswordCredentialService CreateService(PasswordPolicy? policy = null)
        => new(new StubPasswordHasher(), Options.Create(policy ?? new PasswordPolicy()));

    [Fact]
    public void HashPassword_ProducesCredential_ThatVerifies()
    {
        PasswordCredentialService service = CreateService();

        PasswordCredential credential = service.HashPassword(Password);

        credential.Hash.Should().NotBe(Password);
        service.VerifyPassword(Password, credential).Should().Be(PasswordVerificationResult.Success);
    }

    [Fact]
    public void VerifyPassword_WrongPassword_ReturnsFailed()
    {
        PasswordCredentialService service = CreateService();
        PasswordCredential credential = service.HashPassword(Password);

        service.VerifyPassword(OtherPassword, credential).Should().Be(PasswordVerificationResult.Failed);
    }

    [Fact]
    public void ChangePassword_WrongCurrentPassword_Fails_AndLeavesCredentialUnchanged()
    {
        PasswordCredentialService service = CreateService();
        User user = User.Create(Email.From("user@example.com"), service.HashPassword(Password));
        string originalHash = user.PasswordCredential!.Hash;
        long originalVersion = user.Version;

        Result result = service.ChangePassword(user, "wrong-current", OtherPassword);

        result.IsFailure.Should().BeTrue();
        result.FirstError!.Code.Should().Be("Identity.InvalidCurrentPassword");
        user.PasswordCredential!.Hash.Should().Be(originalHash);
        user.Version.Should().Be(originalVersion);
    }

    [Fact]
    public void ChangePassword_WeakNewPassword_Fails_AndLeavesCredentialUnchanged()
    {
        PasswordCredentialService service = CreateService();
        User user = User.Create(Email.From("user@example.com"), service.HashPassword(Password));
        string originalHash = user.PasswordCredential!.Hash;
        long originalVersion = user.Version;

        Result result = service.ChangePassword(user, Password, "short");

        result.IsFailure.Should().BeTrue();
        result.FirstError!.Code.Should().Be("Identity.PasswordTooShort");
        user.PasswordCredential!.Hash.Should().Be(originalHash);
        user.Version.Should().Be(originalVersion);
    }

    [Fact]
    public void ChangePassword_NoExistingCredential_Fails()
    {
        PasswordCredentialService service = CreateService();
        User user = User.Create(Email.From("user@example.com"));

        Result result = service.ChangePassword(user, Password, OtherPassword);

        result.IsFailure.Should().BeTrue();
        result.FirstError!.Code.Should().Be("Identity.NoPasswordCredential");
    }

    [Fact]
    public void ChangePassword_CorrectCurrentPassword_Succeeds_ReplacesCredential_AndBumpsVersion()
    {
        PasswordCredentialService service = CreateService();
        User user = User.Create(Email.From("user@example.com"), service.HashPassword(Password));
        long originalVersion = user.Version;

        Result result = service.ChangePassword(user, Password, OtherPassword);

        result.IsSuccess.Should().BeTrue();
        service.VerifyPassword(OtherPassword, user.PasswordCredential!).Should().Be(PasswordVerificationResult.Success);
        service.VerifyPassword(Password, user.PasswordCredential!).Should().Be(PasswordVerificationResult.Failed);
        user.Version.Should().Be(originalVersion + 1);
    }
}
