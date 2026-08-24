using FluentAssertions;
using LastHour.BuildingBlocks.SharedKernel.Domain.Exceptions;
using LastHour.Modules.Identity.Domain.Roles;

namespace LastHour.Modules.Identity.Tests.Domain;

public class RoleTests
{
    [Fact]
    public void Create_NewRole_NormalizesNameAndKeepsOriginalCase()
    {
        Role role = Role.Create("Administrator", "Manages the platform");

        role.Id.Value.Should().NotBe(Guid.Empty);
        role.Name.Value.Should().Be("Administrator");
        role.Name.NormalizedValue.Should().Be("administrator");
        role.Description.Should().Be("Manages the platform");
        role.UserRoles.Should().BeEmpty();
        role.RolePermissions.Should().BeEmpty();
    }

    [Fact]
    public void Create_WhitespacePaddedName_IsTrimmedAndNormalized()
    {
        Role role = Role.Create("  Viewer  ");

        role.Name.Value.Should().Be("Viewer");
        role.Name.NormalizedValue.Should().Be("viewer");
    }

    [Fact]
    public void Create_EmptyName_Throws()
    {
        Action act = () => Role.Create("   ");

        act.Should().Throw<InvalidRoleNameException>();
    }

    [Fact]
    public void Create_OverlongName_Throws()
    {
        Action act = () => Role.Create(new string('a', RoleName.MaxLength + 1));

        act.Should().Throw<InvalidRoleNameException>();
    }

    [Fact]
    public void Create_OverlongDescription_Throws()
    {
        Action act = () => Role.Create("Viewer", new string('a', Role.MaxDescriptionLength + 1));

        act.Should().Throw<InvalidRoleNameException>();
    }

    [Fact]
    public void Create_DifferentCaseNames_NormalizeToSameValue()
    {
        RoleName.From("Administrator").NormalizedValue.Should()
            .Be(RoleName.From("administrator").NormalizedValue);
    }

    [Fact]
    public void AssignPermission_GrantsPermission()
    {
        Role role = Role.Create("Viewer");
        PermissionId permissionId = PermissionId.New();

        role.AssignPermission(permissionId);

        role.HasPermission(permissionId).Should().BeTrue();
        role.RolePermissions.Should().ContainSingle(grant => grant.PermissionId == permissionId);
        role.RolePermissions.Single().RoleId.Should().Be(role.Id);
    }

    [Fact]
    public void AssignPermission_SamePermissionTwice_Throws()
    {
        Role role = Role.Create("Viewer");
        PermissionId permissionId = PermissionId.New();
        role.AssignPermission(permissionId);

        Action act = () => role.AssignPermission(permissionId);

        act.Should().Throw<BusinessRuleValidationException>();
        role.RolePermissions.Should().ContainSingle();
    }

    [Fact]
    public void AssignPermission_MultiplePermissions_AllHeld()
    {
        Role role = Role.Create("Viewer");

        role.AssignPermission(PermissionId.New());
        role.AssignPermission(PermissionId.New());

        role.RolePermissions.Should().HaveCount(2);
    }

    [Fact]
    public void RevokePermission_RemovesOnlyTheTargetGrant()
    {
        Role role = Role.Create("Viewer");
        PermissionId keep = PermissionId.New();
        PermissionId remove = PermissionId.New();
        role.AssignPermission(keep);
        role.AssignPermission(remove);

        role.RevokePermission(remove);

        role.HasPermission(remove).Should().BeFalse();
        role.HasPermission(keep).Should().BeTrue();
        role.RolePermissions.Should().ContainSingle();
    }

    [Fact]
    public void RevokePermission_NotGranted_Throws()
    {
        Role role = Role.Create("Viewer");

        Action act = () => role.RevokePermission(PermissionId.New());

        act.Should().Throw<BusinessRuleValidationException>();
    }
}
