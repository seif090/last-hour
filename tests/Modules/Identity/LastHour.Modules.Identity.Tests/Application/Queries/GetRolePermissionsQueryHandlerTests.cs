using FluentAssertions;
using LastHour.BuildingBlocks.SharedKernel.Results;
using LastHour.Modules.Identity.Application.Queries;
using LastHour.Modules.Identity.Domain.Roles;
using LastHour.Modules.Identity.Tests.Application.TestSupport;

namespace LastHour.Modules.Identity.Tests.Application.Queries;

public class GetRolePermissionsQueryHandlerTests
{
    [Fact]
    public async Task Handle_RoleWithPermissions_ReturnsSafeProjections()
    {
        var roles = new FakeRoleRepository();
        Permission read = roles.AddPermission("Users.Read", "View user accounts.");
        Permission manage = roles.AddPermission("Users.Manage");
        Role role = roles.AddRole("Administrator", read, manage);

        Result<IReadOnlyList<PermissionInfo>> result = await new GetRolePermissionsQueryHandler(roles)
            .Handle(new GetRolePermissionsQuery(role.Id.Value), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Select(permission => permission.Code)
            .Should().BeEquivalentTo("Users.Read", "Users.Manage");
        result.Value.Single(permission => permission.Code == "Users.Read").Description
            .Should().Be("View user accounts.");
    }

    [Fact]
    public async Task Handle_RoleWithoutGrants_ReturnsEmptyList()
    {
        var roles = new FakeRoleRepository();
        Role role = roles.AddRole("Viewer");

        Result<IReadOnlyList<PermissionInfo>> result = await new GetRolePermissionsQueryHandler(roles)
            .Handle(new GetRolePermissionsQuery(role.Id.Value), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_UnknownRole_ReturnsNotFound()
    {
        Result<IReadOnlyList<PermissionInfo>> result = await new GetRolePermissionsQueryHandler(new FakeRoleRepository())
            .Handle(new GetRolePermissionsQuery(Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.FirstError!.Type.Should().Be(ErrorType.NotFound);
        result.FirstError!.Code.Should().Be("Roles.NotFound");
    }
}
