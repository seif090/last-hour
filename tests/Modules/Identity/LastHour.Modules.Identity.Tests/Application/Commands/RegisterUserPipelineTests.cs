using FluentAssertions;
using LastHour.BuildingBlocks.SharedKernel.Results;
using LastHour.Modules.Identity.Domain.Users;
using LastHour.Modules.Identity.Tests.Application.Commands.TestSupport;
using LastHour.Modules.Identity.Tests.Application.TestSupport;

namespace LastHour.Modules.Identity.Tests.Application.Commands;

public class RegisterUserPipelineTests
{
    private const string Password = "correct horse battery staple";

    [Fact]
    public async Task Register_ValidUser_PersistsNormalizedEmailHashAndPendingStatus()
    {
        await using TestAuthPipeline pipeline = await TestAuthPipeline.CreateAsync();

        Result<UserId> result = await pipeline.RegisterAsync("User@Example.com", Password);

        result.IsSuccess.Should().BeTrue();
        User? persisted = await pipeline.FindByNormalizedEmailAsync("user@example.com");
        persisted.Should().NotBeNull();
        persisted!.Status.Should().Be(AccountStatus.Pending);
        persisted.PasswordCredential.Should().NotBeNull();
        persisted.PasswordCredential!.Hash.Should().Be(new StubPasswordHasher().Hash(Password));
        persisted.PasswordCredential.Hash.Should().NotBe(Password, "the plaintext password is never stored");
    }

    [Fact]
    public async Task Register_DuplicateEmail_DifferentCasing_ReturnsConflict()
    {
        await using TestAuthPipeline pipeline = await TestAuthPipeline.CreateAsync();
        Result<UserId> first = await pipeline.RegisterAsync("User@Example.com", Password);
        first.IsSuccess.Should().BeTrue();

        Result<UserId> second = await pipeline.RegisterAsync("USER@example.com", Password);

        second.IsSuccess.Should().BeFalse();
        second.FirstError!.Type.Should().Be(ErrorType.Conflict);
        second.FirstError!.Code.Should().Be("Identity.EmailAlreadyRegistered");
        (await pipeline.FindByNormalizedEmailAsync("user@example.com")).Should().NotBeNull();
    }

    [Fact]
    public async Task Register_InvalidEmail_ReturnsValidationFailure()
    {
        await using TestAuthPipeline pipeline = await TestAuthPipeline.CreateAsync();

        Result<UserId> result = await pipeline.RegisterAsync("not-an-email", Password);

        result.IsFailure.Should().BeTrue();
        result.FirstError!.Type.Should().Be(ErrorType.Validation);
        result.FirstError!.Code.Should().Be("InvalidEmail");
    }

    [Fact]
    public async Task Register_WeakPassword_ReturnsPolicyFailure()
    {
        await using TestAuthPipeline pipeline = await TestAuthPipeline.CreateAsync();

        Result<UserId> result = await pipeline.RegisterAsync("user@example.com", "short");

        result.IsFailure.Should().BeTrue();
        result.FirstError!.Type.Should().Be(ErrorType.Validation);
        result.FirstError!.Code.Should().Be("Identity.PasswordTooShort");
    }

    [Fact]
    public async Task Register_MissingFields_ValidationBehaviorShortCircuits()
    {
        await using TestAuthPipeline pipeline = await TestAuthPipeline.CreateAsync();

        Result<UserId> missingEmail = await pipeline.RegisterAsync(string.Empty, Password);
        missingEmail.IsFailure.Should().BeTrue();
        ValidationError emailError = missingEmail.FirstError.Should().BeOfType<ValidationError>().Subject;
        emailError.Validations.Should().Contain(error => error.Code == "EmailRequired");

        Result<UserId> missingPassword = await pipeline.RegisterAsync("user@example.com", string.Empty);
        missingPassword.IsFailure.Should().BeTrue();
        ValidationError passwordError = missingPassword.FirstError.Should().BeOfType<ValidationError>().Subject;
        passwordError.Validations.Should().Contain(error => error.Code == "PasswordRequired");
    }

    [Fact]
    public async Task Register_ConcurrentSameNormalizedEmail_ExactlyOneSucceedsAndDatabaseStaysConsistent()
    {
        await using TestAuthPipeline pipeline = await TestAuthPipeline.CreateAsync();

        Task<Result<UserId>>[] attempts = Enumerable
            .Range(0, 4)
            .Select(_ => pipeline.RegisterAsync("race@example.com", Password))
            .ToArray();

        Result<UserId>[] results = await Task.WhenAll(attempts);

        results.Count(result => result.IsSuccess).Should().Be(1, "exactly one registration may win the race");
        results.Where(result => result.IsFailure).Should().OnlyContain(
            result => result.FirstError!.Type == ErrorType.Conflict,
            "every loser fails safely as a conflict, never with internal details");
        (await pipeline.FindByNormalizedEmailAsync("race@example.com")).Should().NotBeNull();
    }
}
