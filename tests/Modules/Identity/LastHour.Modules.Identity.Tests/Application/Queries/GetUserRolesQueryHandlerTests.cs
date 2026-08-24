using FluentAssertions;
using LastHour.BuildingBlocks.SharedKernel.Results;
using LastHour.Modules.Identity.Application.Queries;
using LastHour.Modules.Identity.Domain.Roles;
using LastHour.Modules.Identity.Domain.Users;
using LastHour.Modules.Identity.Tests.Application.TestSupport;

namespace LastHour.Modules.Identity.Tests.Application.Queries;

public class GetUserRolesQueryHandlerTests
{
    [Fact]
    public async Task Handle_UserWithRoles_ReturnsSafeProjections()
    {
        var users = new FakeUserRepository();
        User user = users.AddActiveUser("user@example.com", "correct horse battery staple");
        var roles = new FakeRoleRepository(users);
        roles.AddRole("Viewer");
        roles.AddRole("Editor");
        foreach (RoleId roleId in roles.All.Select(role => role.Id))
        {
            user.AssignRole(roleId);
        }

        Result<IReadOnlyList<RoleInfo>> result = await new GetUserRolesQueryHandler(users, roles)
            .Handle(new GetUserRolesQuery(user.Id.Value), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Select(role => role.Name).Should().BeEquivalentTo("Viewer", "Editor");
        result.Value.Select(role => role.RoleId).Should().NotContain(Guid.Empty);
    }

    [Fact]
    public async Task Handle_UserWithoutRoles_ReturnsEmptyList()
    {
        var users = new FakeUserRepository();
        User user = users.AddActiveUser("user@example.com", "correct horse battery staple");

        Result<IReadOnlyList<RoleInfo>> result = await new GetUserRolesQueryHandler(users, new FakeRoleRepository())
            .Handle(new GetUserRolesQuery(user.Id.Value), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_UnknownUser_ReturnsNotFound()
    {
        var users = new FakeUserRepository();

        Result<IReadOnlyList<RoleInfo>> result = await new GetUserRolesQueryHandler(users, new FakeRoleRepository())
            .Handle(new GetUserRolesQuery(Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.FirstError!.Type.Should().Be(ErrorType.NotFound);
        result.FirstError!.Code.Should().Be("Users.NotFound");
    }
}
