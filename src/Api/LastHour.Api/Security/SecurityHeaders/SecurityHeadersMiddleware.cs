using System.Text;
using Microsoft.Extensions.Options;

namespace LastHour.Api.Security.SecurityHeaders;

/// <summary>
/// Applies the configured security headers to every response. Headers are attached before the
/// response is sent (via <c>HttpResponse.OnStarting</c>) so they are applied after the response
/// is produced; HSTS is only emitted over HTTPS, where it is meaningful.
/// </summary>
public sealed class SecurityHeadersMiddleware : IMiddleware
{
    private readonly IOptions<SecurityHeadersOptions> _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="SecurityHeadersMiddleware"/> class.
    /// </summary>
    /// <param name="options">The security headers options.</param>
    public SecurityHeadersMiddleware(IOptions<SecurityHeadersOptions> options)
    {
        _options = options;
    }

    /// <summary>
    /// Invokes the remainder of the pipeline with the security headers attached.
    /// </summary>
    /// <param name="context">The current HTTP context.</param>
    /// <param name="next">The next middleware in the pipeline.</param>
    /// <returns>A task representing the remainder of the request pipeline.</returns>
    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        SecurityHeadersOptions options = _options.Value;
        if (options.Enabled)
        {
            context.Response.OnStarting(() =>
            {
                ApplyHeaders(context.Response, options);
                return Task.CompletedTask;
            });
        }

        await next(context);
    }

    private static void ApplyHeaders(HttpResponse response, SecurityHeadersOptions options)
    {
        if (options.XContentTypeOptionsEnabled)
        {
            response.Headers["X-Content-Type-Options"] = "nosniff";
        }

        if (!string.IsNullOrWhiteSpace(options.ReferrerPolicy))
        {
            response.Headers["Referrer-Policy"] = options.ReferrerPolicy;
        }

        if (!string.IsNullOrWhiteSpace(options.FrameOptions))
        {
            response.Headers["X-Frame-Options"] = options.FrameOptions;
        }

        if (!string.IsNullOrWhiteSpace(options.PermissionsPolicy))
        {
            response.Headers["Permissions-Policy"] = options.PermissionsPolicy;
        }

        if (!string.IsNullOrWhiteSpace(options.ContentSecurityPolicy))
        {
            response.Headers["Content-Security-Policy"] = options.ContentSecurityPolicy;
        }

        if (options.Hsts.Enabled && IsHttps(response.HttpContext.Request))
        {
            response.Headers["Strict-Transport-Security"] = BuildHstsValue(options.Hsts);
        }
    }

    private static bool IsHttps(HttpRequest request)
        => string.Equals(request.Scheme, "https", StringComparison.OrdinalIgnoreCase);

    private static string BuildHstsValue(HstsOptions hsts)
    {
        // HSTS max-age is expressed in seconds; the option is configured in days for readability.
        var builder = new StringBuilder("max-age=")
            .Append(hsts.MaxAgeDays * 86_400);

        if (hsts.IncludeSubDomains)
        {
            builder.Append("; includeSubDomains");
        }

        if (hsts.Preload)
        {
            builder.Append("; preload");
        }

        return builder.ToString();
    }
}
