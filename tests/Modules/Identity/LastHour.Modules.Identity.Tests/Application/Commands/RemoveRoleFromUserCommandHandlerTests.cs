using FluentAssertions;
using LastHour.BuildingBlocks.SharedKernel.Results;
using LastHour.Modules.Identity.Application.Commands;
using LastHour.Modules.Identity.Domain.Roles;
using LastHour.Modules.Identity.Domain.Users;
using LastHour.Modules.Identity.Tests.Application.TestSupport;
using Microsoft.Extensions.Logging.Abstractions;

namespace LastHour.Modules.Identity.Tests.Application.Commands;

public class RemoveRoleFromUserCommandHandlerTests
{
    private static RemoveRoleFromUserCommandHandler CreateHandler(FakeUserRepository users, FakeRoleRepository roles)
        => new RemoveRoleFromUserCommandHandler(users, roles, NullLogger<RemoveRoleFromUserCommandHandler>.Instance);

    [Fact]
    public async Task Handle_ValidRemoval_RemovesOnlyTheTargetRole()
    {
        var users = new FakeUserRepository();
        User user = users.AddActiveUser("user@example.com", "correct horse battery staple");
        var roles = new FakeRoleRepository();
        Role keep = roles.AddRole("Viewer");
        Role remove = roles.AddRole("Editor");
        user.AssignRole(keep.Id);
        user.AssignRole(remove.Id);

        Result result = await CreateHandler(users, roles).Handle(
            new RemoveRoleFromUserCommand(user.Id.Value, remove.Id.Value),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        user.HasRole(remove.Id).Should().BeFalse();
        user.HasRole(keep.Id).Should().BeTrue();
        user.UserRoles.Should().ContainSingle();
    }

    [Fact]
    public async Task Handle_RoleNotAssigned_ReturnsConflict()
    {
        var users = new FakeUserRepository();
        User user = users.AddActiveUser("user@example.com", "correct horse battery staple");
        var roles = new FakeRoleRepository();
        Role role = roles.AddRole("Viewer");

        Result result = await CreateHandler(users, roles).Handle(
            new RemoveRoleFromUserCommand(user.Id.Value, role.Id.Value),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.FirstError!.Type.Should().Be(ErrorType.Conflict);
        result.FirstError!.Code.Should().Be("Users.RoleNotAssigned");
    }

    [Fact]
    public async Task Handle_UnknownUser_ReturnsNotFound()
    {
        var roles = new FakeRoleRepository();
        Role role = roles.AddRole("Viewer");

        Result result = await CreateHandler(new FakeUserRepository(), roles).Handle(
            new RemoveRoleFromUserCommand(Guid.NewGuid(), role.Id.Value),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.FirstError!.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task Handle_UnknownRole_ReturnsNotFound()
    {
        var users = new FakeUserRepository();
        User user = users.AddActiveUser("user@example.com", "correct horse battery staple");

        Result result = await CreateHandler(users, new FakeRoleRepository()).Handle(
            new RemoveRoleFromUserCommand(user.Id.Value, Guid.NewGuid()),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.FirstError!.Type.Should().Be(ErrorType.NotFound);
    }
}
