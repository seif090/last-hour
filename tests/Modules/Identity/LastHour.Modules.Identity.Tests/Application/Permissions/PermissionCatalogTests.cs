using FluentAssertions;
using LastHour.Modules.Identity.Application.Permissions;

namespace LastHour.Modules.Identity.Tests.Application.Permissions;

public class PermissionCatalogTests
{
    [Fact]
    public void Catalog_IsNotEmpty()
    {
        PermissionCatalog.All.Should().NotBeEmpty();
    }

    [Fact]
    public void Catalog_AllCodesAreUnique()
    {
        IEnumerable<string> codes = PermissionCatalog.All.Select(permission => permission.Code);

        codes.Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public void Catalog_ContainsOutboxPermissions()
    {
        PermissionCatalog.All.Should().Contain(PermissionCatalog.OutboxRead);
        PermissionCatalog.All.Should().Contain(PermissionCatalog.OutboxManage);
        PermissionCatalog.OutboxRead.Code.Should().Be("Outbox.Read");
        PermissionCatalog.OutboxManage.Code.Should().Be("Outbox.Manage");
    }

    [Fact]
    public void Catalog_AllCodesFollowResourceActionConvention()
    {
        foreach (PermissionDefinition permission in PermissionCatalog.All)
        {
            permission.HasValidCode().Should().BeTrue(
                $"'{permission.Code}' must follow the Resource.Action convention");
        }
    }

    [Fact]
    public void Catalog_EachPermissionHasADescription()
    {
        foreach (PermissionDefinition permission in PermissionCatalog.All)
        {
            permission.Description.Should().NotBeNullOrWhiteSpace(
                $"permission '{permission.Code}' should describe its purpose");
        }
    }

    [Theory]
    [InlineData("Users.Read", true)]
    [InlineData("Merchant.Offers.Create", true)]
    [InlineData("users.read", true)]
    [InlineData("Read", false)]
    [InlineData("Users", false)]
    [InlineData("Users.", false)]
    [InlineData(".Read", false)]
    [InlineData("Users.Read.Admin", true)]
    [InlineData("User s.Read", false)]
    [InlineData("Users..Read", false)]
    public void HasValidCode_ConventionCases(string code, bool expected)
    {
        var definition = new PermissionDefinition(code, null);

        definition.HasValidCode().Should().Be(expected);
    }
}
