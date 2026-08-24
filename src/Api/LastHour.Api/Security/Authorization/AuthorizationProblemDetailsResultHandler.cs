using LastHour.Api.Middleware;
using LastHour.Api.ProblemDetails;
using LastHour.BuildingBlocks.SharedKernel.Results;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;

namespace LastHour.Api.Security.Authorization;

/// <summary>
/// Shapes the outcome of the authorization middleware. Successful requests pass straight
/// through; unauthenticated requests are delegated to the framework's default handler so the
/// challenge (401 plus the bearer <c>WWW-Authenticate</c> header) behaves exactly as before;
/// and an authenticated caller who fails a policy receives a consistent RFC 7807 403 payload
/// in the same shape as every other API error — with a stable <c>code</c> and the request's
/// correlation id, but no internal permission graph, role structure or database details.
/// </summary>
public sealed class AuthorizationProblemDetailsResultHandler : IAuthorizationMiddlewareResultHandler
{
    private readonly AuthorizationMiddlewareResultHandler _defaultHandler = new AuthorizationMiddlewareResultHandler();

    /// <inheritdoc/>
    public async Task HandleAsync(
        RequestDelegate next,
        HttpContext context,
        AuthorizationPolicy policy,
        PolicyAuthorizationResult authorizeResult)
    {
        if (authorizeResult.Succeeded)
        {
            await next(context).ConfigureAwait(false);
            return;
        }

        if (authorizeResult.Challenged)
        {
            // Unauthenticated or token rejected: preserve the framework's 401 challenge
            // (status code plus WWW-Authenticate header) untouched.
            await _defaultHandler.HandleAsync(next, context, policy, authorizeResult).ConfigureAwait(false);
            return;
        }

        Microsoft.AspNetCore.Mvc.ProblemDetails problemDetails = ErrorToProblemDetailsMapper.Map(Error.Forbidden(
            "AuthorizationFailed",
            "You do not have permission to perform this action."));

        if (context.Items.TryGetValue(CorrelationIdDefaults.ContextKey, out object? value)
            && value is string correlationId)
        {
            problemDetails.Extensions["correlationId"] = correlationId;
        }

        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        await context.Response.WriteAsJsonAsync(
            problemDetails,
            problemDetails.GetType(),
            cancellationToken: context.RequestAborted).ConfigureAwait(false);
    }
}
