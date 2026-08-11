using FluentAssertions;
using LastHour.BuildingBlocks.SharedKernel.Domain.Exceptions;
using LastHour.Modules.Identity.Domain.Users;

namespace LastHour.Modules.Identity.Tests.Domain;

public class UserTests
{
    private static readonly Email DefaultEmail = Email.From("user@example.com");

    [Fact]
    public void Create_NewUser_IsPendingWithUniqueId()
    {
        User user = User.Create(DefaultEmail);

        user.Id.Value.Should().NotBe(Guid.Empty);
        user.Status.Should().Be(AccountStatus.Pending);
        user.Email.Should().Be(DefaultEmail);
        user.Version.Should().Be(0);
        user.PasswordCredential.Should().BeNull();
    }

    [Fact]
    public void Create_EachUser_HasUniqueId()
    {
        User first = User.Create(DefaultEmail);
        User second = User.Create(Email.From("other@example.com"));

        first.Id.Should().NotBe(second.Id);
    }

    [Fact]
    public void Create_NullEmail_Throws()
    {
        Action act = () => User.Create(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Create_WithPasswordCredential_StoresCredential()
    {
        User user = User.Create(DefaultEmail, PasswordCredential.FromHash("hash-value"));

        user.PasswordCredential.Should().NotBeNull();
        user.PasswordCredential!.Hash.Should().Be("hash-value");
    }

    [Fact]
    public void Activate_FromPending_TransitionsToActive()
    {
        User user = User.Create(DefaultEmail);

        user.Activate();

        user.Status.Should().Be(AccountStatus.Active);
        user.Version.Should().Be(1);
    }

    [Fact]
    public void Activate_FromSuspended_Reinstates()
    {
        User user = CreateActiveUser();
        user.Suspend();

        user.Activate();

        user.Status.Should().Be(AccountStatus.Active);
    }

    [Fact]
    public void Activate_FromActive_Throws()
    {
        User user = CreateActiveUser();

        Action act = () => user.Activate();

        act.Should().Throw<BusinessRuleValidationException>();
    }

    [Fact]
    public void Activate_FromDisabled_Throws()
    {
        User user = CreateActiveUser();
        user.Disable();

        Action act = () => user.Activate();

        act.Should().Throw<BusinessRuleValidationException>();
    }

    [Fact]
    public void Suspend_FromActive_TransitionsToSuspended()
    {
        User user = CreateActiveUser();

        user.Suspend();

        user.Status.Should().Be(AccountStatus.Suspended);
    }

    [Fact]
    public void Suspend_FromPending_Throws()
    {
        User user = User.Create(DefaultEmail);

        Action act = () => user.Suspend();

        act.Should().Throw<BusinessRuleValidationException>();
    }

    [Fact]
    public void Suspend_FromSuspended_Throws()
    {
        User user = CreateActiveUser();
        user.Suspend();

        Action act = () => user.Suspend();

        act.Should().Throw<BusinessRuleValidationException>();
    }

    [Theory]
    [InlineData(AccountStatus.Pending)]
    [InlineData(AccountStatus.Active)]
    [InlineData(AccountStatus.Suspended)]
    public void Disable_FromActiveLifecycleState_TransitionsToDisabled(AccountStatus from)
    {
        User user = from switch
        {
            AccountStatus.Active => CreateActiveUser(),
            AccountStatus.Suspended => Suspend(CreateActiveUser()),
            _ => User.Create(DefaultEmail),
        };

        user.Disable();

        user.Status.Should().Be(AccountStatus.Disabled);
    }

    [Fact]
    public void Disable_FromDisabled_Throws()
    {
        User user = CreateActiveUser();
        user.Disable();

        Action act = () => user.Disable();

        act.Should().Throw<BusinessRuleValidationException>();
    }

    [Fact]
    public void ChangeEmail_UpdatesAddressAndNormalizedValue()
    {
        User user = User.Create(DefaultEmail);

        user.ChangeEmail(Email.From("New.User@Example.com"));

        user.Email.Value.Should().Be("New.User@Example.com");
        user.Email.NormalizedValue.Should().Be("new.user@example.com");
        user.Version.Should().Be(1);
    }

    [Fact]
    public void ChangeEmail_SameNormalizedAddress_Throws()
    {
        User user = User.Create(DefaultEmail);

        Action act = () => user.ChangeEmail(Email.From("USER@example.com"));

        act.Should().Throw<BusinessRuleValidationException>();
    }

    [Fact]
    public void ChangeEmail_OnDisabledUser_Throws()
    {
        User user = CreateActiveUser();
        user.Disable();

        Action act = () => user.ChangeEmail(Email.From("new@example.com"));

        act.Should().Throw<BusinessRuleValidationException>();
    }

    [Fact]
    public void SetPasswordCredential_StoresHashAndBumpsVersion()
    {
        User user = User.Create(DefaultEmail);

        user.SetPasswordCredential(PasswordCredential.FromHash("new-hash"));

        user.PasswordCredential!.Hash.Should().Be("new-hash");
        user.Version.Should().Be(1);
    }

    [Fact]
    public void SetPasswordCredential_Null_Throws()
    {
        User user = User.Create(DefaultEmail);

        Action act = () => user.SetPasswordCredential(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Mutations_IncrementVersion_ForOptimisticConcurrency()
    {
        User user = User.Create(DefaultEmail);
        user.Activate();

        long versionAfterActivate = user.Version;
        user.Suspend();

        user.Version.Should().BeGreaterThan(versionAfterActivate);
    }

    private static User CreateActiveUser()
    {
        User user = User.Create(DefaultEmail);
        user.Activate();
        return user;
    }

    private static User Suspend(User user)
    {
        user.Suspend();
        return user;
    }
}
