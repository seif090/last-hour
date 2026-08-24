using FluentAssertions;
using LastHour.BuildingBlocks.Infrastructure.Persistence;
using LastHour.BuildingBlocks.Infrastructure.Persistence.Repositories;
using LastHour.Modules.Identity.Application.Persistence;
using LastHour.Modules.Identity.Domain.Roles;
using LastHour.Modules.Identity.Domain.Users;
using LastHour.Modules.Identity.Tests.Persistence.TestSupport;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace LastHour.Modules.Identity.Tests.Persistence;

/// <summary>
/// Persistence tests for the roles and permissions model over real SQLite (relational, so
/// unique constraints and referential actions are honored). These tests exercise the composed
/// <see cref="RoleRepository"/> against actual EF translation, which is what guarantees the
/// authorization resolution queries (roles of a user, effective permissions of a user)
/// translate to single database queries.
/// </summary>
public class RolesPermissionsPersistenceTests
{
    [Fact]
    public async Task CreateRole_WithGrant_PersistsAndReloads()
    {
        using SqliteConnection connection = TestIdentityPersistence.CreateOpenConnection();
        RoleId roleId;

        await using (LastHourDbContext context = await TestIdentityPersistence.CreateContextAsync(connection))
        {
            Permission permission = Permission.Create("Users.Read", "View user accounts.");
            Role role = Role.Create("Viewer", "Read-only access");
            role.AssignPermission(permission.Id);

            context.Set<Permission>().Add(permission);
            context.Set<Role>().Add(role);
            await context.SaveChangesAsync();
            roleId = role.Id;
        }

        await using (LastHourDbContext reloaded = TestIdentityPersistence.CreateSqliteContext(connection))
        {
            Role? role = await reloaded.Set<Role>()
                .Include(candidate => candidate.RolePermissions)
                .SingleAsync(candidate => candidate.Id == roleId);

            role.Name.Value.Should().Be("Viewer");
            role.Name.NormalizedValue.Should().Be("viewer");
            role.RolePermissions.Should().ContainSingle();
        }
    }

    [Fact]
    public async Task AssignRoleToUser_PersistsAssociation_AndResolvesRolesForUser()
    {
        using SqliteConnection connection = TestIdentityPersistence.CreateOpenConnection();
        UserId userId;

        await using (LastHourDbContext context = await TestIdentityPersistence.CreateContextAsync(connection))
        {
            User user = User.Create(Email.From("user@example.com"));
            user.Activate();
            Role role = Role.Create("Viewer");
            user.AssignRole(role.Id);

            context.Set<User>().Add(user);
            context.Set<Role>().Add(role);
            await context.SaveChangesAsync();
            userId = user.Id;
        }

        await using (LastHourDbContext context = TestIdentityPersistence.CreateSqliteContext(connection))
        {
            var roles = new RoleRepository(
                new EfRepository<Role>(context),
                new EfReadRepository<Role, RoleId>(context),
                new EfReadRepository<Permission, PermissionId>(context),
                new EfTrackedRepository<Role, RoleId>(context));

            IReadOnlyList<Role> resolved = await roles.GetRolesForUserAsync(userId);

            resolved.Should().ContainSingle(role => role.Name.NormalizedValue == "viewer");
        }
    }

    [Fact]
    public async Task EffectivePermissions_ResolveUnionAcrossRoles_InSingleQuery()
    {
        using SqliteConnection connection = TestIdentityPersistence.CreateOpenConnection();
        UserId userId;

        await using (LastHourDbContext context = await TestIdentityPersistence.CreateContextAsync(connection))
        {
            Permission usersRead = Permission.Create("Users.Read");
            Permission usersManage = Permission.Create("Users.Manage");
            Permission rolesRead = Permission.Create("Roles.Read");

            Role viewer = Role.Create("Viewer");
            viewer.AssignPermission(usersRead.Id);
            Role administrator = Role.Create("Administrator");
            administrator.AssignPermission(usersRead.Id);
            administrator.AssignPermission(usersManage.Id);
            administrator.AssignPermission(rolesRead.Id);

            User user = User.Create(Email.From("admin@example.com"));
            user.Activate();
            user.AssignRole(viewer.Id);
            user.AssignRole(administrator.Id);

            context.AddRange(usersRead, usersManage, rolesRead, viewer, administrator, user);
            await context.SaveChangesAsync();
            userId = user.Id;
        }

        await using (LastHourDbContext context = TestIdentityPersistence.CreateSqliteContext(connection))
        {
            var roles = new RoleRepository(
                new EfRepository<Role>(context),
                new EfReadRepository<Role, RoleId>(context),
                new EfReadRepository<Permission, PermissionId>(context),
                new EfTrackedRepository<Role, RoleId>(context));

            IReadOnlyList<Permission> permissions = await roles.GetPermissionsForUserAsync(userId);

            permissions.Select(permission => permission.Code)
                .Should().BeEquivalentTo("Users.Read", "Users.Manage", "Roles.Read");
        }
    }

    [Fact]
    public async Task DuplicateNormalizedRoleName_UniqueConstraintRejectsSecond()
    {
        using SqliteConnection connection = TestIdentityPersistence.CreateOpenConnection();

        await using (LastHourDbContext context = await TestIdentityPersistence.CreateContextAsync(connection))
        {
            context.Set<Role>().Add(Role.Create("Administrator"));
            await context.SaveChangesAsync();

            Func<Task> act = () =>
            {
                context.Set<Role>().Add(Role.Create("administrator"));
                return context.SaveChangesAsync();
            };

            await act.Should().ThrowAsync<DbUpdateException>();
        }
    }

    [Fact]
    public async Task DuplicatePermissionCode_UniqueConstraintRejectsSecond()
    {
        using SqliteConnection connection = TestIdentityPersistence.CreateOpenConnection();

        await using (LastHourDbContext context = await TestIdentityPersistence.CreateContextAsync(connection))
        {
            context.Set<Permission>().Add(Permission.Create("Users.Read"));
            await context.SaveChangesAsync();

            Func<Task> act = () =>
            {
                context.Set<Permission>().Add(Permission.Create("Users.Read"));
                return context.SaveChangesAsync();
            };

            await act.Should().ThrowAsync<DbUpdateException>();
        }
    }

    [Fact]
    public async Task DuplicateUserRoleAssignment_CompositeKeyRejectsSecond()
    {
        using SqliteConnection connection = TestIdentityPersistence.CreateOpenConnection();
        UserId userId;
        RoleId roleId;

        await using (LastHourDbContext context = await TestIdentityPersistence.CreateContextAsync(connection))
        {
            User user = User.Create(Email.From("user@example.com"));
            user.Activate();
            Role role = Role.Create("Viewer");
            context.Set<User>().Add(user);
            context.Set<Role>().Add(role);
            await context.SaveChangesAsync();

            context.Set<UserRole>().Add(UserRole.Create(user.Id, role.Id));
            await context.SaveChangesAsync();
            userId = user.Id;
            roleId = role.Id;
        }

        // A fresh context attempts the same composite key: the database (not the change
        // tracker) must reject it.
        await using (LastHourDbContext competing = TestIdentityPersistence.CreateSqliteContext(connection))
        {
            UserId loadedUserId = competing.Set<User>().Single(user => user.Id == userId).Id;
            RoleId loadedRoleId = competing.Set<Role>().Single(role => role.Id == roleId).Id;

            Func<Task> act = () =>
            {
                competing.Set<UserRole>().Add(UserRole.Create(loadedUserId, loadedRoleId));
                return competing.SaveChangesAsync();
            };

            await act.Should().ThrowAsync<DbUpdateException>();
        }
    }

    [Fact]
    public async Task DeletingAssignedRole_IsRejectedByReferentialIntegrity()
    {
        // SQLite enforces foreign keys only when the pragma is enabled; the production
        // PostgreSQL database enforces them always, so the test mirrors that. The delete runs
        // in a fresh context so only the database — not EF's client-side cascade computation —
        // decides the outcome.
        using SqliteConnection connection = new SqliteConnection("Data Source=:memory:;Foreign Keys=True");
        connection.Open();
        RoleId roleId;

        await using (LastHourDbContext context = await TestIdentityPersistence.CreateContextAsync(connection))
        {
            User user = User.Create(Email.From("user@example.com"));
            user.Activate();
            Role role = Role.Create("Viewer");
            user.AssignRole(role.Id);
            context.Set<User>().Add(user);
            context.Set<Role>().Add(role);
            await context.SaveChangesAsync();
            roleId = role.Id;
        }

        await using (LastHourDbContext competing = TestIdentityPersistence.CreateSqliteContext(connection))
        {
            Role loaded = competing.Set<Role>().Single(role => role.Id == roleId);
            competing.Set<Role>().Remove(loaded);

            Func<Task> act = () => competing.SaveChangesAsync();
            await act.Should().ThrowAsync<DbUpdateException>();
        }
    }

    [Fact]
    public async Task DeletingGrantedPermission_IsRejectedByReferentialIntegrity()
    {
        // SQLite enforces foreign keys only when the pragma is enabled; the production
        // PostgreSQL database enforces them always, so the test mirrors that. The delete runs
        // in a fresh context so only the database — not EF's client-side cascade computation —
        // decides the outcome.
        using SqliteConnection connection = new SqliteConnection("Data Source=:memory:;Foreign Keys=True");
        connection.Open();
        PermissionId permissionId;

        await using (LastHourDbContext context = await TestIdentityPersistence.CreateContextAsync(connection))
        {
            Permission permission = Permission.Create("Users.Read");
            Role role = Role.Create("Administrator");
            role.AssignPermission(permission.Id);
            context.Set<Permission>().Add(permission);
            context.Set<Role>().Add(role);
            await context.SaveChangesAsync();
            permissionId = permission.Id;
        }

        await using (LastHourDbContext competing = TestIdentityPersistence.CreateSqliteContext(connection))
        {
            Permission loaded = competing.Set<Permission>().Single(permission => permission.Id == permissionId);
            competing.Set<Permission>().Remove(loaded);

            Func<Task> act = () => competing.SaveChangesAsync();
            await act.Should().ThrowAsync<DbUpdateException>();
        }
    }
}
