using System.Security.Claims;
using LastHour.BuildingBlocks.Application.Contracts;

namespace LastHour.Api.Security.Authentication;

/// <summary>
/// The HTTP-aware <see cref="ICurrentUser"/>: the identity of the authenticated caller,
/// resolved from the validated access-token claims of the current request. This replaces the
/// infrastructure default (which represents an unauthenticated principal for background
/// workers) inside the API host, so application-layer code sees the real caller during an
/// HTTP request. Identity always comes from claims — never from request bodies — and the
/// access token carries no roles by design, so <see cref="Roles"/> and
/// <see cref="IsInRole"/> report an empty set until role claims are deliberately added.
/// </summary>
public sealed class HttpContextCurrentUser : ICurrentUser
{
    private readonly ClaimsPrincipal _principal;

    /// <summary>
    /// Initializes a new instance of the <see cref="HttpContextCurrentUser"/> class.
    /// </summary>
    /// <param name="httpContextAccessor">The accessor used to reach the current request context.</param>
    /// <exception cref="ArgumentNullException"><paramref name="httpContextAccessor"/> is <see langword="null"/>.</exception>
    public HttpContextCurrentUser(IHttpContextAccessor httpContextAccessor)
    {
        ArgumentNullException.ThrowIfNull(httpContextAccessor);
        _principal = httpContextAccessor.HttpContext?.User ?? new ClaimsPrincipal();
    }

    /// <inheritdoc/>
    public bool IsAuthenticated => _principal.Identity?.IsAuthenticated == true;

    /// <inheritdoc/>
    public string? UserId
    {
        get
        {
            Guid? userId = _principal.GetUserId();
            return userId?.ToString("D");
        }
    }

    /// <inheritdoc/>
    public string? Name
    {
        get
        {
            // The access token carries no name claim today; the subject is the stable
            // identity. Kept for future optional display-name claims.
            string? name = _principal.FindFirst("name")?.Value;
            return string.IsNullOrWhiteSpace(name) ? null : name;
        }
    }

    /// <inheritdoc/>
    public IReadOnlyCollection<string> Roles => Array.Empty<string>();

    /// <inheritdoc/>
    public bool IsInRole(string role) => false;
}
