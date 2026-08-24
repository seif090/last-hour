using FluentAssertions;
using LastHour.BuildingBlocks.SharedKernel.Results;
using LastHour.Modules.Identity.Application.Commands;
using LastHour.Modules.Identity.Domain.Roles;
using LastHour.Modules.Identity.Domain.Users;
using LastHour.Modules.Identity.Tests.Application.TestSupport;
using Microsoft.Extensions.Logging.Abstractions;

namespace LastHour.Modules.Identity.Tests.Application.Commands;

public class AssignRoleToUserCommandHandlerTests
{
    private static AssignRoleToUserCommandHandler CreateHandler(FakeUserRepository users, FakeRoleRepository roles)
        => new AssignRoleToUserCommandHandler(users, roles, NullLogger<AssignRoleToUserCommandHandler>.Instance);

    [Fact]
    public async Task Handle_ValidAssignment_AddsRoleToUser()
    {
        var users = new FakeUserRepository();
        User user = users.AddActiveUser("user@example.com", "correct horse battery staple");
        var roles = new FakeRoleRepository();
        Role role = roles.AddRole("Viewer");

        Result result = await CreateHandler(users, roles).Handle(
            new AssignRoleToUserCommand(user.Id.Value, role.Id.Value),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        user.HasRole(role.Id).Should().BeTrue();
    }

    [Fact]
    public async Task Handle_DuplicateAssignment_ReturnsConflictAndKeepsSingleAssignment()
    {
        var users = new FakeUserRepository();
        User user = users.AddActiveUser("user@example.com", "correct horse battery staple");
        var roles = new FakeRoleRepository();
        Role role = roles.AddRole("Viewer");
        AssignRoleToUserCommandHandler handler = CreateHandler(users, roles);

        await handler.Handle(new AssignRoleToUserCommand(user.Id.Value, role.Id.Value), CancellationToken.None);
        Result result = await handler.Handle(
            new AssignRoleToUserCommand(user.Id.Value, role.Id.Value),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.FirstError!.Type.Should().Be(ErrorType.Conflict);
        result.FirstError!.Code.Should().Be("Users.RoleAlreadyAssigned");
        user.UserRoles.Should().ContainSingle();
    }

    [Fact]
    public async Task Handle_MultipleRoles_AllAssignable()
    {
        var users = new FakeUserRepository();
        User user = users.AddActiveUser("user@example.com", "correct horse battery staple");
        var roles = new FakeRoleRepository();
        Role first = roles.AddRole("Viewer");
        Role second = roles.AddRole("Editor");
        AssignRoleToUserCommandHandler handler = CreateHandler(users, roles);

        await handler.Handle(new AssignRoleToUserCommand(user.Id.Value, first.Id.Value), CancellationToken.None);
        Result result = await handler.Handle(
            new AssignRoleToUserCommand(user.Id.Value, second.Id.Value),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        user.HasRole(first.Id).Should().BeTrue();
        user.HasRole(second.Id).Should().BeTrue();
        user.UserRoles.Should().HaveCount(2);
    }

    [Fact]
    public async Task Handle_UnknownUser_ReturnsNotFound()
    {
        var roles = new FakeRoleRepository();
        Role role = roles.AddRole("Viewer");

        Result result = await CreateHandler(new FakeUserRepository(), roles).Handle(
            new AssignRoleToUserCommand(Guid.NewGuid(), role.Id.Value),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.FirstError!.Type.Should().Be(ErrorType.NotFound);
        result.FirstError!.Code.Should().Be("Users.NotFound");
    }

    [Fact]
    public async Task Handle_UnknownRole_ReturnsNotFoundAndDoesNotAssign()
    {
        var users = new FakeUserRepository();
        User user = users.AddActiveUser("user@example.com", "correct horse battery staple");

        Result result = await CreateHandler(users, new FakeRoleRepository()).Handle(
            new AssignRoleToUserCommand(user.Id.Value, Guid.NewGuid()),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.FirstError!.Type.Should().Be(ErrorType.NotFound);
        result.FirstError!.Code.Should().Be("Roles.NotFound");
        user.UserRoles.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_ValidAssignment_BumpsUserConcurrencyVersion()
    {
        var users = new FakeUserRepository();
        User user = users.AddActiveUser("user@example.com", "correct horse battery staple");
        var roles = new FakeRoleRepository();
        Role role = roles.AddRole("Viewer");
        long versionBefore = user.Version;

        await CreateHandler(users, roles).Handle(
            new AssignRoleToUserCommand(user.Id.Value, role.Id.Value),
            CancellationToken.None);

        user.Version.Should().BeGreaterThan(versionBefore);
    }
}
