using LastHour.Api.Middleware;
using LastHour.Api.Observability.Auditing;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace LastHour.Api.Tests.Observability.Auditing;

public class AuditMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_ForbiddenStatus_RecordsAccessRejectedEvent()
    {
        var logger = new CapturingAuditLogger();
        var middleware = new AuditMiddleware(logger, Options.Create(new AuditLoggingOptions()));
        DefaultHttpContext context = CreateContext();
        context.Response.StatusCode = StatusCodes.Status403Forbidden;

        await middleware.InvokeAsync(context, _ => Task.CompletedTask);

        AuditEvent recorded = Assert.Single(logger.Events);
        Assert.Equal("AccessRejected", recorded.EventType);
        Assert.Equal("Denied", recorded.Outcome);
        Assert.Equal(403, recorded.StatusCode);
        Assert.Equal("GET /api/v1/orders", recorded.Action);
        Assert.Equal("corr-123", recorded.CorrelationId);
    }

    [Fact]
    public async Task InvokeAsync_UnauthorizedStatus_RecordsAccessRejectedEvent()
    {
        var logger = new CapturingAuditLogger();
        var middleware = new AuditMiddleware(logger, Options.Create(new AuditLoggingOptions()));
        DefaultHttpContext context = CreateContext();
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;

        await middleware.InvokeAsync(context, _ => Task.CompletedTask);

        AuditEvent recorded = Assert.Single(logger.Events);
        Assert.Equal("AccessRejected", recorded.EventType);
        Assert.Equal(401, recorded.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_TooManyRequests_RecordsAccessRejectedEvent()
    {
        var logger = new CapturingAuditLogger();
        var middleware = new AuditMiddleware(logger, Options.Create(new AuditLoggingOptions()));
        DefaultHttpContext context = CreateContext();
        context.Response.StatusCode = StatusCodes.Status429TooManyRequests;

        await middleware.InvokeAsync(context, _ => Task.CompletedTask);

        AuditEvent recorded = Assert.Single(logger.Events);
        Assert.Equal("AccessRejected", recorded.EventType);
        Assert.Equal(429, recorded.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_ServerError_RecordsServerErrorEvent()
    {
        var logger = new CapturingAuditLogger();
        var middleware = new AuditMiddleware(logger, Options.Create(new AuditLoggingOptions()));
        DefaultHttpContext context = CreateContext();
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;

        await middleware.InvokeAsync(context, _ => Task.CompletedTask);

        AuditEvent recorded = Assert.Single(logger.Events);
        Assert.Equal("ServerError", recorded.EventType);
        Assert.Equal("Failed", recorded.Outcome);
        Assert.Equal(500, recorded.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_SuccessStatus_RecordsNothing()
    {
        var logger = new CapturingAuditLogger();
        var middleware = new AuditMiddleware(logger, Options.Create(new AuditLoggingOptions()));
        DefaultHttpContext context = CreateContext();
        context.Response.StatusCode = StatusCodes.Status200OK;

        await middleware.InvokeAsync(context, _ => Task.CompletedTask);

        Assert.Empty(logger.Events);
    }

    [Fact]
    public async Task InvokeAsync_Disabled_RecordsNothing()
    {
        var logger = new CapturingAuditLogger();
        var middleware = new AuditMiddleware(
            logger,
            Options.Create(new AuditLoggingOptions { Enabled = false }));
        DefaultHttpContext context = CreateContext();
        context.Response.StatusCode = StatusCodes.Status403Forbidden;

        await middleware.InvokeAsync(context, _ => Task.CompletedTask);

        Assert.Empty(logger.Events);
    }

    [Fact]
    public async Task InvokeAsync_WithoutCorrelationId_RecordsNullCorrelationId()
    {
        var logger = new CapturingAuditLogger();
        var middleware = new AuditMiddleware(logger, Options.Create(new AuditLoggingOptions()));
        DefaultHttpContext context = CreateContext();
        context.Items.Clear();
        context.Response.StatusCode = StatusCodes.Status403Forbidden;

        await middleware.InvokeAsync(context, _ => Task.CompletedTask);

        Assert.Null(Assert.Single(logger.Events).CorrelationId);
    }

    [Fact]
    public void Constructor_NullAuditLogger_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(
            () => new AuditMiddleware(null!, Options.Create(new AuditLoggingOptions())));
    }

    [Fact]
    public void Constructor_NullOptions_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(
            () => new AuditMiddleware(new CapturingAuditLogger(), null!));
    }

    private static DefaultHttpContext CreateContext()
    {
        var context = new DefaultHttpContext();
        context.Request.Method = "GET";
        context.Request.Path = "/api/v1/orders";
        context.Items[CorrelationIdDefaults.ContextKey] = "corr-123";
        return context;
    }

    private sealed class CapturingAuditLogger : IAuditLogger
    {
        public List<AuditEvent> Events { get; } = new List<AuditEvent>();

        public void LogAudit(AuditEvent auditEvent) => Events.Add(auditEvent);
    }
}
