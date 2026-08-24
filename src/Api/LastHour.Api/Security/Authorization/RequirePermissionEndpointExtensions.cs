namespace LastHour.Api.Security.Authorization;

/// <summary>
/// The minimal-API counterpart of <see cref="RequirePermissionAttribute"/>: a fluent way to
/// protect an endpoint with a catalog permission. Both surface the same policy-name convention
/// (<c>Permission.&lt;code&gt;</c>), so the policy provider resolves them identically.
/// </summary>
public static class RequirePermissionEndpointExtensions
{
    /// <summary>
    /// Adds the authorization policy for the given catalog permission to the endpoint.
    /// </summary>
    /// <param name="builder">The route handler builder to extend.</param>
    /// <param name="permission">The required permission code from the centralized catalog.</param>
    /// <returns>The same builder, so calls can be chained.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> or <paramref name="permission"/>
    /// is <see langword="null"/>.</exception>
    public static RouteHandlerBuilder RequirePermission(this RouteHandlerBuilder builder, string permission)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(permission);

        return builder.RequireAuthorization($"{RequirePermissionAttribute.PolicyPrefix}{permission}");
    }
}
