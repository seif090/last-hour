using LastHour.Api.Security.Authorization;

namespace LastHour.Api.Tests.Security.Authorization;

/// <summary>
/// Exercises the <see cref="RequirePermissionAttribute"/>: it is pure metadata mapping a
/// catalog code onto the permission policy convention.
/// </summary>
public class RequirePermissionAttributeTests
{
    [Fact]
    public void Ctor_MapsPermissionCodeToPolicyName()
    {
        var attribute = new RequirePermissionAttribute("Users.Read");

        Assert.Equal("Permission.Users.Read", attribute.Policy);
    }

    [Fact]
    public void Ctor_KeepsCatalogCodeVerbatim()
    {
        var attribute = new RequirePermissionAttribute("Roles.Manage");

        Assert.Equal(
            RequirePermissionAttribute.PolicyPrefix + "Roles.Manage",
            attribute.Policy);
    }

    [Fact]
    public void Ctor_NullPermission_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new RequirePermissionAttribute(null!));
    }

    [Fact]
    public void Attribute_AllowsMultiplePermissionsPerTarget()
    {
        AttributeUsageAttribute? usage = typeof(RequirePermissionAttribute)
            .GetCustomAttributes(typeof(AttributeUsageAttribute), inherit: false)
            .Cast<AttributeUsageAttribute>()
            .Single();

        Assert.True(usage.AllowMultiple);
        Assert.True(usage.Inherited);
    }

    [Fact]
    public void Attribute_IsAuthorizationAttribute()
    {
        Assert.IsAssignableFrom<Microsoft.AspNetCore.Authorization.AuthorizeAttribute>(
            new RequirePermissionAttribute("Users.Read"));
    }
}
