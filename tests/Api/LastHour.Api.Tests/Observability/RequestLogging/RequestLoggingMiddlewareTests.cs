using System.Text;
using LastHour.Api.Middleware;
using LastHour.Api.Observability.RequestLogging;
using LastHour.BuildingBlocks.Application.Contracts;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace LastHour.Api.Tests.Observability.RequestLogging;

/// <summary>
/// Exercises the request logger's contract: exactly one structured event per request carrying
/// method, path, status and correlation id, while sensitive request data (authorization headers,
/// cookies, query strings, bodies) never reaches the log.
/// </summary>
public class RequestLoggingMiddlewareTests
{
    private const string SecretToken = "Bearer sup3r-secret-token";
    private const string SecretCookie = "session=super-secret-cookie";
    private const string SecretQuery = "password=hunter2";
    private const string SecretBody = "{\"password\":\"hunter2\"}";

    [Fact]
    public async Task InvokeAsync_RecordsSingleStructuredEventWithRequestOutcome()
    {
        var logger = new CapturingLogger();
        var middleware = new RequestLoggingMiddleware(logger, new AnonymousCurrentUser());
        DefaultHttpContext context = CreateContext();

        await middleware.InvokeAsync(context, _ => Task.CompletedTask);

        LogRecord record = Assert.Single(logger.Records);
        Assert.Equal(LogLevel.Information, record.Level);
        Assert.Equal("GET", record.StateValue("Method"));
        Assert.Equal("/api/v1/system/status", record.StateValue("Path"));
        Assert.Equal("200", record.StateValue("StatusCode"));
        Assert.Equal("corr-123", record.StateValue("CorrelationId"));
    }

    [Fact]
    public async Task InvokeAsync_AuthorizationHeaderIsNeverLogged()
    {
        var logger = new CapturingLogger();
        var middleware = new RequestLoggingMiddleware(logger, new AnonymousCurrentUser());
        DefaultHttpContext context = CreateContext();
        context.Request.Headers.Authorization = SecretToken;
        context.Request.Headers.Cookie = SecretCookie;

        await middleware.InvokeAsync(context, _ => Task.CompletedTask);

        LogRecord record = Assert.Single(logger.Records);
        Assert.DoesNotContain(SecretToken, record.FormattedMessage);
        Assert.DoesNotContain(SecretToken, record.AllStateValues());
        Assert.DoesNotContain(SecretCookie, record.FormattedMessage);
        Assert.DoesNotContain(SecretCookie, record.AllStateValues());
    }

    [Fact]
    public async Task InvokeAsync_QueryStringAndBodyAreNeverLogged()
    {
        var logger = new CapturingLogger();
        var middleware = new RequestLoggingMiddleware(logger, new AnonymousCurrentUser());
        DefaultHttpContext context = CreateContext();
        context.Request.QueryString = new QueryString($"?{SecretQuery}");
        context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(SecretBody));

        await middleware.InvokeAsync(context, _ => Task.CompletedTask);

        LogRecord record = Assert.Single(logger.Records);
        Assert.DoesNotContain(SecretQuery, record.FormattedMessage);
        Assert.DoesNotContain(SecretBody, record.FormattedMessage);
        Assert.DoesNotContain(SecretQuery, record.AllStateValues());
        Assert.DoesNotContain(SecretBody, record.AllStateValues());
    }

    [Fact]
    public async Task InvokeAsync_WithoutCorrelationId_LogsEmptyCorrelationId()
    {
        var logger = new CapturingLogger();
        var middleware = new RequestLoggingMiddleware(logger, new AnonymousCurrentUser());
        DefaultHttpContext context = CreateContext();
        context.Items.Clear();

        await middleware.InvokeAsync(context, _ => Task.CompletedTask);

        LogRecord record = Assert.Single(logger.Records);
        Assert.Equal(string.Empty, record.StateValue("CorrelationId"));
    }

    private static DefaultHttpContext CreateContext()
    {
        var context = new DefaultHttpContext();
        context.Request.Method = "GET";
        context.Request.Path = "/api/v1/system/status";
        context.Items[CorrelationIdDefaults.ContextKey] = "corr-123";
        return context;
    }

    private sealed class AnonymousCurrentUser : ICurrentUser
    {
        public bool IsAuthenticated => false;

        public string? UserId => null;

        public string? Name => null;

        public IReadOnlyCollection<string> Roles => Array.Empty<string>();

        public bool IsInRole(string role) => false;
    }

    private sealed class CapturingLogger : ILogger<RequestLoggingMiddleware>
    {
        public List<LogRecord> Records { get; } = new List<LogRecord>();

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            object? capturedState = state is null ? null : state;
            Records.Add(new LogRecord(logLevel, eventId, formatter(state, exception), capturedState ?? "null"));
        }
    }

    private sealed record LogRecord(LogLevel Level, EventId EventId, string FormattedMessage, object State)
    {
        public string StateValue(string key)
        {
            foreach (KeyValuePair<string, object?> pair in StateAsEnumerable())
            {
                if (string.Equals(pair.Key, key, StringComparison.Ordinal))
                {
                    return pair.Value?.ToString() ?? string.Empty;
                }
            }

            return string.Empty;
        }

        public string AllStateValues()
            => string.Join('|', StateAsEnumerable().Select(pair => $"{pair.Key}={pair.Value}"));

        private IEnumerable<KeyValuePair<string, object?>> StateAsEnumerable()
        {
            if (State is IEnumerable<KeyValuePair<string, object?>> enumerable)
            {
                return enumerable;
            }

            return State is IEnumerable<KeyValuePair<string, object>> typed
                ? typed.Select(pair => new KeyValuePair<string, object?>(pair.Key, pair.Value))
                : Enumerable.Empty<KeyValuePair<string, object?>>();
        }
    }
}
