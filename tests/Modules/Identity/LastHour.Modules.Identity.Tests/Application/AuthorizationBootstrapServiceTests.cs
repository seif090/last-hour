using FluentAssertions;
using LastHour.BuildingBlocks.Infrastructure.Persistence;
using LastHour.BuildingBlocks.Infrastructure.Persistence.Repositories;
using LastHour.BuildingBlocks.Infrastructure.Persistence.UnitOfWork;
using LastHour.Modules.Identity.Application;
using LastHour.Modules.Identity.Application.Permissions;
using LastHour.Modules.Identity.Application.Persistence;
using LastHour.Modules.Identity.Domain.Roles;
using LastHour.Modules.Identity.Tests.Application.TestSupport;
using LastHour.Modules.Identity.Tests.Persistence.TestSupport;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace LastHour.Modules.Identity.Tests.Application;

public class AuthorizationBootstrapServiceTests
{
    [Fact]
    public async Task Seed_FirstRun_CreatesCatalogPermissionsBootstrapRoleAndFullGrants()
    {
        var permissions = new FakePermissionRepository();
        var roles = new FakeRoleRepository(permissionStore: permissions);
        var unitOfWork = new FakeUnitOfWork();

        await new AuthorizationBootstrapService(permissions, permissions, roles, unitOfWork)
            .SeedAsync(CancellationToken.None);

        permissions.All.Select(permission => permission.Code)
            .Should().BeEquivalentTo(PermissionCatalog.All.Select(definition => definition.Code));

        Role administrator = roles.All.Should().ContainSingle().Subject;
        administrator.Name.Value.Should().Be(BootstrapRoles.Administrator);
        administrator.Name.NormalizedValue.Should().Be(BootstrapRoles.Administrator.ToLowerInvariant());

        var grantedIds = administrator.RolePermissions.Select(grant => grant.PermissionId);
        grantedIds.Should().HaveCount(PermissionCatalog.All.Count);
        unitOfWork.SaveChangesCount.Should().Be(1, "the whole bootstrap commits atomically once");
    }

    [Fact]
    public async Task Seed_SecondRun_IsIdempotent()
    {
        var permissions = new FakePermissionRepository();
        var roles = new FakeRoleRepository(permissionStore: permissions);
        var unitOfWork = new FakeUnitOfWork();
        AuthorizationBootstrapService service = new AuthorizationBootstrapService(permissions, permissions, roles, unitOfWork);

        await service.SeedAsync(CancellationToken.None);
        await service.SeedAsync(CancellationToken.None);

        permissions.All.Should().HaveCount(PermissionCatalog.All.Count);
        roles.All.Should().ContainSingle();
        roles.All.Single().RolePermissions.Should().HaveCount(PermissionCatalog.All.Count);
        unitOfWork.SaveChangesCount.Should().Be(2, "each run is one atomic commit");
    }

    [Fact]
    public async Task Seed_ExistingPermissions_AreNotDuplicated()
    {
        var permissions = new FakePermissionRepository();
        permissions.AddPermission("Users.Read", "Pre-existing.");
        var roles = new FakeRoleRepository(permissionStore: permissions);
        var unitOfWork = new FakeUnitOfWork();

        await new AuthorizationBootstrapService(permissions, permissions, roles, unitOfWork)
            .SeedAsync(CancellationToken.None);

        permissions.All.Should().HaveCount(PermissionCatalog.All.Count);
        permissions.All.Should().ContainSingle(permission => permission.Code == "Users.Read");
    }

    [Fact]
    public async Task Seed_RunningAfterManualGrantRemoval_ReestablishesMissingGrants()
    {
        var permissions = new FakePermissionRepository();
        var roles = new FakeRoleRepository(permissionStore: permissions);
        var unitOfWork = new FakeUnitOfWork();
        AuthorizationBootstrapService service = new AuthorizationBootstrapService(permissions, permissions, roles, unitOfWork);

        await service.SeedAsync(CancellationToken.None);

        Role administrator = roles.All.Single();
        PermissionId removed = administrator.RolePermissions.First().PermissionId;
        administrator.RevokePermission(removed);

        await service.SeedAsync(CancellationToken.None);

        administrator.RolePermissions.Should().HaveCount(PermissionCatalog.All.Count);
        administrator.HasPermission(removed).Should().BeTrue();
    }

    [Fact]
    public async Task Seed_PersistsAtomicallyThroughRealEf()
    {
        using SqliteConnection connection = TestIdentityPersistence.CreateOpenConnection();

        await using (LastHourDbContext context = await TestIdentityPersistence.CreateContextAsync(connection))
        {
            var service = new AuthorizationBootstrapService(
                new EfRepository<Permission>(context),
                new EfReadRepository<Permission, PermissionId>(context),
                new RoleRepository(
                    new EfRepository<Role>(context),
                    new EfReadRepository<Role, RoleId>(context),
                    new EfReadRepository<Permission, PermissionId>(context),
                    new EfTrackedRepository<Role, RoleId>(context)),
                new EfUnitOfWork(context));

            await service.SeedAsync(CancellationToken.None);
            await service.SeedAsync(CancellationToken.None);
        }

        await using (LastHourDbContext reloaded = TestIdentityPersistence.CreateSqliteContext(connection))
        {
            (await reloaded.Set<Permission>().CountAsync()).Should().Be(PermissionCatalog.All.Count);
            (await reloaded.Set<Role>().CountAsync()).Should().Be(1);
            (await reloaded.Set<RolePermissionGrant>().CountAsync()).Should().Be(PermissionCatalog.All.Count);
        }
    }
}
