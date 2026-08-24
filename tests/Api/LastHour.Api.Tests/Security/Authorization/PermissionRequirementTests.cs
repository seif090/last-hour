using LastHour.Api.Security.Authorization;
using Microsoft.AspNetCore.Authorization;

namespace LastHour.Api.Tests.Security.Authorization;

/// <summary>
/// Exercises the permission requirement: it carries the catalog code and is immutable.
/// </summary>
public class PermissionRequirementTests
{
    [Fact]
    public void Ctor_StoresPermissionCode()
    {
        var requirement = new PermissionRequirement("Users.Read");

        Assert.Equal("Users.Read", requirement.Permission);
    }

    [Fact]
    public void Ctor_NullPermission_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new PermissionRequirement(null!));
    }

    [Fact]
    public void Requirement_IsImmutable()
    {
        // The requirement is a record with a get-only property: there is no mutation surface.
        var requirement = new PermissionRequirement("Roles.Manage");

        System.Reflection.PropertyInfo? property = typeof(PermissionRequirement)
            .GetProperty(nameof(PermissionRequirement.Permission));

        Assert.NotNull(property);
        Assert.Null(property!.SetMethod);
        Assert.Equal("Roles.Manage", requirement.Permission);
    }

    [Fact]
    public void Requirement_IsRegisteredAsAuthorizationRequirement()
    {
        Assert.IsAssignableFrom<IAuthorizationRequirement>(new PermissionRequirement("Users.Read"));
    }
}
