using FluentAssertions;
using LastHour.BuildingBlocks.SharedKernel.Results;
using LastHour.Modules.Identity.Application.Queries;
using LastHour.Modules.Identity.Domain.Roles;
using LastHour.Modules.Identity.Domain.Users;
using LastHour.Modules.Identity.Tests.Application.TestSupport;

namespace LastHour.Modules.Identity.Tests.Application.Queries;

public class GetUserPermissionsQueryHandlerTests
{
    [Fact]
    public async Task Handle_UserWithRoles_ResolvesEffectivePermissionsAcrossRoles()
    {
        var users = new FakeUserRepository();
        User user = users.AddActiveUser("user@example.com", "correct horse battery staple");
        var roles = new FakeRoleRepository(users);
        Permission usersRead = roles.AddPermission("Users.Read");
        Permission usersManage = roles.AddPermission("Users.Manage");
        Permission rolesRead = roles.AddPermission("Roles.Read");
        roles.AddRole("Viewer", usersRead);
        roles.AddRole("Administrator", usersRead, usersManage, rolesRead);
        foreach (RoleId roleId in roles.All.Select(role => role.Id))
        {
            user.AssignRole(roleId);
        }

        Result<IReadOnlyList<PermissionInfo>> result = await new GetUserPermissionsQueryHandler(users, roles)
            .Handle(new GetUserPermissionsQuery(user.Id.Value), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Select(permission => permission.Code)
            .Should().BeEquivalentTo("Users.Read", "Users.Manage", "Roles.Read");
    }

    [Fact]
    public async Task Handle_UserWithoutRoles_ReturnsEmptySet()
    {
        var users = new FakeUserRepository();
        User user = users.AddActiveUser("user@example.com", "correct horse battery staple");

        Result<IReadOnlyList<PermissionInfo>> result = await new GetUserPermissionsQueryHandler(users, new FakeRoleRepository())
            .Handle(new GetUserPermissionsQuery(user.Id.Value), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_UnknownUser_ReturnsNotFound()
    {
        var users = new FakeUserRepository();

        Result<IReadOnlyList<PermissionInfo>> result = await new GetUserPermissionsQueryHandler(users, new FakeRoleRepository())
            .Handle(new GetUserPermissionsQuery(Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.FirstError!.Type.Should().Be(ErrorType.NotFound);
        result.FirstError!.Code.Should().Be("Users.NotFound");
    }
}
