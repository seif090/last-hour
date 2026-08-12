namespace LastHour.Api.ProblemDetails;

using LastHour.Api.Middleware;
using LastHour.BuildingBlocks.SharedKernel.Results;
using Microsoft.AspNetCore.Diagnostics;
using ProblemDetails = Microsoft.AspNetCore.Mvc.ProblemDetails;

/// <summary>
/// Handles exceptions that escape the request pipeline. The full exception, including its stack
/// trace, is logged together with the request's correlation id. The caller receives a sanitized
/// RFC 7807 <see cref="ProblemDetails"/> response that never contains a stack trace or a connection
/// string: in development the exception message is shown (with credentials redacted) so the cause
/// is visible; in production only a generic message is returned. The response always carries the
/// correlation id so the caller can reference the request when reporting the failure. Cancellations
/// are swallowed because there is no caller left to respond to.
/// </summary>
public sealed class ProblemDetailsExceptionHandler : IExceptionHandler
{
    private const string GenericDetail = "An unexpected error occurred while processing your request.";

    private static readonly Action<ILogger, string, string, Exception?> UnhandledException =
        LoggerMessage.Define<string, string>(
            LogLevel.Error,
            new EventId(6, nameof(ProblemDetailsExceptionHandler)),
            "Unhandled exception while processing request {RequestPath} with correlation id {CorrelationId}");

    private readonly ILogger<ProblemDetailsExceptionHandler> _logger;
    private readonly IHostEnvironment _environment;

    /// <summary>
    /// Initializes a new instance of the <see cref="ProblemDetailsExceptionHandler"/> class.
    /// </summary>
    /// <param name="logger">The logger used to record unhandled exceptions.</param>
    /// <param name="environment">The environment used to decide how much detail to expose.</param>
    public ProblemDetailsExceptionHandler(ILogger<ProblemDetailsExceptionHandler> logger, IHostEnvironment environment)
    {
        _logger = logger;
        _environment = environment;
    }

    /// <summary>
    /// Logs the exception and writes a problem details response for it.
    /// </summary>
    /// <param name="httpContext">The current HTTP context.</param>
    /// <param name="exception">The exception that escaped the pipeline.</param>
    /// <param name="cancellationToken">The token used to signal cancellation.</param>
    /// <returns><see langword="true"/> because the exception is always handled here.</returns>
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is OperationCanceledException)
        {
            return true;
        }

        string correlationId = httpContext.Items.TryGetValue(CorrelationIdDefaults.ContextKey, out object? value)
            && value is string id
            ? id
            : string.Empty;

        if (exception is BadHttpRequestException badRequest)
        {
            // Malformed requests (for example an unparseable JSON body) are client errors, not
            // server failures; they are answered with their own status code and sanitized body.
            UnhandledException(_logger, httpContext.Request.Path, correlationId, exception);

            var badRequestProblem = new ProblemDetails
            {
                Status = badRequest.StatusCode,
                Title = ErrorToProblemDetailsMapper.TitleFor(ErrorType.Validation),
                Type = ErrorToProblemDetailsMapper.TypeUriFor(ErrorType.Validation),
                Detail = _environment.IsDevelopment()
                    ? ConnectionStringRedactor.Redact(exception.Message)
                    : "The request is malformed and could not be processed.",
            };
            badRequestProblem.Extensions["code"] = "BadRequest";
            badRequestProblem.Extensions["correlationId"] = correlationId;

            httpContext.Response.StatusCode = badRequest.StatusCode;

            await httpContext.Response.WriteAsJsonAsync(badRequestProblem, options: null, contentType: "application/problem+json", cancellationToken);

            return true;
        }

        UnhandledException(_logger, httpContext.Request.Path, correlationId, exception);

        var problemDetails = new ProblemDetails
        {
            Status = StatusCodes.Status500InternalServerError,
            Title = ErrorToProblemDetailsMapper.TitleFor(ErrorType.Failure),
            Type = ErrorToProblemDetailsMapper.TypeUriFor(ErrorType.Failure),
            Detail = _environment.IsDevelopment()
                ? ConnectionStringRedactor.Redact(exception.Message)
                : GenericDetail,
        };
        problemDetails.Extensions["code"] = "UnhandledException";
        problemDetails.Extensions["correlationId"] = correlationId;

        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;

        await httpContext.Response.WriteAsJsonAsync(problemDetails, options: null, contentType: "application/problem+json", cancellationToken);

        return true;
    }
}
