using FluentAssertions;
using LastHour.BuildingBlocks.SharedKernel.Results;
using LastHour.Modules.Identity.Application;
using LastHour.Modules.Identity.Application.Commands;
using LastHour.Modules.Identity.Domain.Users;
using LastHour.Modules.Identity.Tests.Application.TestSupport;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace LastHour.Modules.Identity.Tests.Application.Commands;

public class RegisterUserCommandHandlerTests
{
    private const string Password = "correct horse battery staple";

    private static RegisterUserCommandHandler CreateHandler(FakeUserRepository users)
    {
        var credentials = new PasswordCredentialService(
            new StubPasswordHasher(),
            Options.Create(new PasswordPolicy()));

        return new RegisterUserCommandHandler(
            users,
            credentials,
            NullLogger<RegisterUserCommandHandler>.Instance);
    }

    [Fact]
    public async Task Handle_ValidCommand_ReturnsNewUserIdAndAddsPendingUser()
    {
        var users = new FakeUserRepository();
        RegisterUserCommandHandler handler = CreateHandler(users);

        Result<UserId> result = await handler.Handle(
            new RegisterUserCommand("User@Example.com", Password),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        User added = users.Added.Should().ContainSingle().Subject;
        added.Id.Should().Be(result.Value);
        added.Email.NormalizedValue.Should().Be("user@example.com");
        added.Status.Should().Be(AccountStatus.Pending);
        added.PasswordCredential.Should().NotBeNull();
        added.PasswordCredential!.Hash.Should().Be(new StubPasswordHasher().Hash(Password));
        added.PasswordCredential!.Hash.Should().NotBe(Password);
    }

    [Fact]
    public async Task Handle_DuplicateEmail_ReturnsConflictAndDoesNotAdd()
    {
        var users = new FakeUserRepository { Duplicate = true };
        RegisterUserCommandHandler handler = CreateHandler(users);

        Result<UserId> result = await handler.Handle(
            new RegisterUserCommand("user@example.com", Password),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.FirstError!.Type.Should().Be(ErrorType.Conflict);
        result.FirstError!.Code.Should().Be("Identity.EmailAlreadyRegistered");
        users.Added.Should().BeEmpty();
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-an-email")]
    [InlineData("user name@example.com")]
    public async Task Handle_InvalidEmail_ReturnsValidationFailure(string email)
    {
        RegisterUserCommandHandler handler = CreateHandler(new FakeUserRepository());

        Result<UserId> result = await handler.Handle(
            new RegisterUserCommand(email, Password),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.FirstError!.Type.Should().Be(ErrorType.Validation);
        result.FirstError!.Code.Should().Be("InvalidEmail");
    }

    [Fact]
    public async Task Handle_EmailExceedingMaxLength_ReturnsValidationFailure()
    {
        RegisterUserCommandHandler handler = CreateHandler(new FakeUserRepository());
        string oversizedEmail = new string('a', Email.MaxLength) + "@example.com";

        Result<UserId> result = await handler.Handle(
            new RegisterUserCommand(oversizedEmail, Password),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.FirstError!.Code.Should().Be("InvalidEmail");
    }

    [Fact]
    public async Task Handle_WeakPassword_ReturnsPolicyFailureBeforeAnyLookup()
    {
        var users = new FakeUserRepository();
        RegisterUserCommandHandler handler = CreateHandler(users);

        Result<UserId> result = await handler.Handle(
            new RegisterUserCommand("user@example.com", "short"),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.FirstError!.Code.Should().Be("Identity.PasswordTooShort");
        users.ExistsByEmailCallCount.Should().Be(0, "policy failures short-circuit before the lookup");
        users.Added.Should().BeEmpty();
    }

    /// <summary>
    /// Deterministic stand-in for the user repository. Registration only exercises the
    /// duplicate pre-check and the add operation, which this fake records.
    /// </summary>
    private sealed class FakeUserRepository : IUserRepository
    {
        public bool Duplicate { get; init; }

        public int ExistsByEmailCallCount { get; private set; }

        public List<User> Added { get; } = new();

        public void Add(User user) => Added.Add(user);

        public void Update(User user)
        {
        }

        public void Remove(User user)
        {
        }

        public void RemoveRange(IEnumerable<User> aggregateRoots)
        {
        }

        public Task<User?> GetByEmailAsync(Email email, CancellationToken cancellationToken = default)
            => Task.FromResult<User?>(null);

        public Task<User?> GetByIdAsync(UserId id, CancellationToken cancellationToken = default)
            => Task.FromResult<User?>(null);

        public Task<User?> GetByIdTrackedAsync(UserId id, CancellationToken cancellationToken = default)
            => Task.FromResult<User?>(null);

        public Task ReloadTrackedAsync(User user, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task<bool> ExistsByEmailAsync(Email email, CancellationToken cancellationToken = default)
        {
            ExistsByEmailCallCount++;
            return Task.FromResult(Duplicate);
        }
    }
}
