using LastHour.Api.Middleware;
using Microsoft.Extensions.Options;

namespace LastHour.Api.Observability.Auditing;

/// <summary>
/// Records security-relevant HTTP outcomes to the audit trail: rejected access attempts
/// (401, 403, 429) and server errors (5xx). Everything else is covered by the request logger,
/// keeping the audit trail focused on what security operations care about.
/// </summary>
public sealed class AuditMiddleware : IMiddleware
{
    private readonly IAuditLogger _auditLogger;
    private readonly AuditLoggingOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="AuditMiddleware"/> class.
    /// </summary>
    /// <param name="auditLogger">The audit logger used to record events.</param>
    /// <param name="options">The audit logging options.</param>
    /// <exception cref="ArgumentNullException"><paramref name="auditLogger"/> or <paramref name="options"/>
    /// is <see langword="null"/>.</exception>
    public AuditMiddleware(IAuditLogger auditLogger, IOptions<AuditLoggingOptions> options)
    {
        _auditLogger = auditLogger ?? throw new ArgumentNullException(nameof(auditLogger));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
    }

    /// <summary>
    /// Invokes the remainder of the pipeline and records an audit event when the response is
    /// auditable. The event is recorded after the response status is final.
    /// </summary>
    /// <param name="context">The current HTTP context.</param>
    /// <param name="next">The next middleware in the pipeline.</param>
    /// <returns>A task representing the remainder of the request pipeline.</returns>
    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        await next(context);

        if (!_options.Enabled)
        {
            return;
        }

        int statusCode = context.Response.StatusCode;
        if (!IsAuditable(statusCode))
        {
            return;
        }

        bool serverError = statusCode >= 500;
        string? correlationId = context.Items.TryGetValue(CorrelationIdDefaults.ContextKey, out object? storedCorrelationId)
            ? storedCorrelationId?.ToString()
            : null;

        _auditLogger.LogAudit(new AuditEvent(
            eventType: serverError ? "ServerError" : "AccessRejected",
            action: $"{context.Request.Method} {context.Request.Path}",
            outcome: serverError ? "Failed" : "Denied",
            statusCode: statusCode,
            correlationId: correlationId,
            remoteIpAddress: context.Connection.RemoteIpAddress?.ToString()));
    }

    private static bool IsAuditable(int statusCode)
        => statusCode is 401 or 403 or 429 or >= 500;
}
