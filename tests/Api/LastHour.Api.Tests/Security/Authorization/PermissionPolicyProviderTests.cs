using FluentAssertions;
using LastHour.Api.Security.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.Extensions.Options;

namespace LastHour.Api.Tests.Security.Authorization;

/// <summary>
/// Exercises the permission policy provider: catalog codes map onto permission requirements,
/// unknown codes fail closed, and everything outside the convention is delegated to the
/// default provider.
/// </summary>
public class PermissionPolicyProviderTests
{
    private static PermissionPolicyProvider CreateProvider()
        => new PermissionPolicyProvider(
            Options.Create(new AuthorizationOptions()));

    [Fact]
    public async Task GetPolicyAsync_PermissionCode_ReturnsPolicyWithRequirement()
    {
        PermissionPolicyProvider provider = CreateProvider();

        AuthorizationPolicy? policy = await provider.GetPolicyAsync("Permission.Users.Read");

        policy.Should().NotBeNull();
        policy!.Requirements.Should().ContainSingle(
                requirement => requirement is PermissionRequirement)
            .Which.Should().BeOfType<PermissionRequirement>()
            .Which.Permission.Should().Be("Users.Read");
        policy.AuthenticationSchemes.Should().BeEmpty();
    }

    [Fact]
    public async Task GetPolicyAsync_PermissionCode_RequiresAuthenticatedUser()
    {
        PermissionPolicyProvider provider = CreateProvider();

        AuthorizationPolicy? policy = await provider.GetPolicyAsync("Permission.Roles.Manage");

        policy.Should().NotBeNull();
        // Every permission policy also denies anonymous principals, so unauthenticated
        // requests are challenged before the permission requirement is evaluated.
        policy!.Requirements.Should().Contain(
            requirement => requirement is DenyAnonymousAuthorizationRequirement);
        policy.Requirements.Should().ContainSingle(
            requirement => requirement is PermissionRequirement);
    }

    [Fact]
    public async Task GetPolicyAsync_UnknownPermissionCode_StillFailsClosed()
    {
        PermissionPolicyProvider provider = CreateProvider();

        // A code that is not in the catalog still resolves to a requirement — one that can
        // never match a real user's permission set, so the endpoint is denied rather than
        // accidentally allowed.
        AuthorizationPolicy? policy = await provider.GetPolicyAsync("Permission.Does.Not.Exist");

        policy.Should().NotBeNull();
        policy!.Requirements.OfType<PermissionRequirement>()
            .Single()
            .Permission.Should().Be("Does.Not.Exist");
    }

    [Fact]
    public async Task GetPolicyAsync_NonPermissionPolicy_DelegatesToDefaultProvider()
    {
        PermissionPolicyProvider provider = CreateProvider();

        AuthorizationPolicy? policy = await provider.GetPolicyAsync("UnregisteredPolicy");

        policy.Should().BeNull();
    }

    [Fact]
    public async Task GetDefaultPolicyAsync_RequiresAuthenticatedUser()
    {
        PermissionPolicyProvider provider = CreateProvider();

        AuthorizationPolicy defaultPolicy = await provider.GetDefaultPolicyAsync();

        // The default policy denies anonymous principals, so an endpoint protected without an
        // explicit permission still rejects unauthenticated requests.
        defaultPolicy.Requirements.Should().ContainSingle(
            requirement => requirement is DenyAnonymousAuthorizationRequirement);
    }

    [Fact]
    public async Task GetFallbackPolicyAsync_DelegatesToDefaultProvider()
    {
        PermissionPolicyProvider provider = CreateProvider();

        AuthorizationPolicy? fallback = await provider.GetFallbackPolicyAsync();

        fallback.Should().BeNull();
    }
}
