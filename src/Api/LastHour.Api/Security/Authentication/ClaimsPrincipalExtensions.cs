using System.Security.Claims;
using Microsoft.IdentityModel.JsonWebTokens;

namespace LastHour.Api.Security.Authentication;

/// <summary>
/// Centralizes claim-to-user-identifier resolution so every authenticated endpoint reads the
/// subject the same way. The user id always comes from the validated access-token claims —
/// never from request bodies or query parameters — which is what makes the authenticated
/// surface immune to client-supplied identity.
/// </summary>
public static class ClaimsPrincipalExtensions
{
    /// <summary>
    /// Resolves the authenticated user's identifier from the <c>sub</c> claim, or
    /// <see langword="null"/> when the claim is absent or malformed.
    /// </summary>
    /// <param name="principal">The authenticated principal.</param>
    /// <returns>The user identifier, or <see langword="null"/> when the token carries none.</returns>
    public static Guid? GetUserId(this ClaimsPrincipal principal)
    {
        ArgumentNullException.ThrowIfNull(principal);

        string? subject = principal.FindFirstValue(JwtRegisteredClaimNames.Sub);
        return subject is not null && Guid.TryParse(subject, out Guid userId) ? userId : null;
    }
}
