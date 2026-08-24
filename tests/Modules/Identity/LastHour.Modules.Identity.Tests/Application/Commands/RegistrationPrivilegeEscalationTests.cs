using FluentAssertions;
using LastHour.Modules.Identity.Application.Commands;

namespace LastHour.Modules.Identity.Tests.Application.Commands;

/// <summary>
/// Guards the public registration surface against privilege escalation: the registration
/// command accepts only the email and password, so a caller can never supply a role, a
/// permission or any privileged input through public registration.
/// </summary>
public class RegistrationPrivilegeEscalationTests
{
    [Fact]
    public void RegisterUserCommand_ExposesOnlyEmailAndPassword()
    {
        string?[] parameterNames = typeof(RegisterUserCommand)
            .GetConstructors()
            .Single()
            .GetParameters()
            .Select(parameter => parameter.Name)
            .ToArray();

        parameterNames.Should().BeEquivalentTo("email", "password");
    }

    [Fact]
    public void RegisterUserCommand_DeclaresNoRoleOrPermissionMembers()
    {
        typeof(RegisterUserCommand).GetProperties()
            .Select(property => property.Name)
            .Should().BeEquivalentTo("Email", "Password");
    }
}
