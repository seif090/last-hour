using System.Text;
using System.Text.Json;
using LastHour.Api.Security.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace LastHour.Api.Tests.Security.Authorization;

/// <summary>
/// Exercises the authorization middleware result handler: successful requests pass through,
/// unauthenticated challenges keep the framework's 401 shape, and an authenticated caller who
/// fails a policy receives a consistent 403 problem details payload.
/// </summary>
public class AuthorizationProblemDetailsResultHandlerTests
{
    private static readonly AuthorizationPolicy Policy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();

    private sealed class StubAuthenticationService : IAuthenticationService
    {
        public bool ChallengeInvoked { get; private set; }

        public Task ChallengeAsync(HttpContext context, string? scheme, AuthenticationProperties? properties)
        {
            ChallengeInvoked = true;
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return Task.CompletedTask;
        }

        public Task<AuthenticateResult> AuthenticateAsync(HttpContext context, string? scheme)
            => Task.FromResult(AuthenticateResult.NoResult());

        public Task ForbidAsync(HttpContext context, string? scheme, AuthenticationProperties? properties)
            => Task.CompletedTask;

        public Task SignInAsync(HttpContext context, string? scheme, System.Security.Claims.ClaimsPrincipal principal, AuthenticationProperties? properties)
            => Task.CompletedTask;

        public Task SignOutAsync(HttpContext context, string? scheme, AuthenticationProperties? properties)
            => Task.CompletedTask;
    }

    private static DefaultHttpContext CreateContext()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IAuthenticationService, StubAuthenticationService>();
        var context = new DefaultHttpContext
        {
            RequestServices = services.BuildServiceProvider(),
            Response = { Body = new MemoryStream() },
        };
        return context;
    }

    private static string ReadBody(HttpContext context)
    {
        context.Response.Body.Position = 0;
        using var reader = new StreamReader(context.Response.Body, Encoding.UTF8);
        return reader.ReadToEnd();
    }

    [Fact]
    public async Task Handle_Succeeded_InvokesNextAndLeavesResponseUntouched()
    {
        var handler = new AuthorizationProblemDetailsResultHandler();
        HttpContext context = CreateContext();
        bool nextInvoked = false;

        await handler.HandleAsync(
            _ =>
            {
                nextInvoked = true;
                return Task.CompletedTask;
            },
            context,
            Policy,
            PolicyAuthorizationResult.Success());

        Assert.True(nextInvoked);
        Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
        Assert.Equal(0, context.Response.Body.Length);
    }

    [Fact]
    public async Task Handle_Forbidden_WritesProblemDetailsWith403()
    {
        var handler = new AuthorizationProblemDetailsResultHandler();
        HttpContext context = CreateContext();

        await handler.HandleAsync(
            _ => throw new InvalidOperationException("Next must not be invoked for a forbidden result."),
            context,
            Policy,
            PolicyAuthorizationResult.Forbid());

        Assert.Equal(StatusCodes.Status403Forbidden, context.Response.StatusCode);

        using JsonDocument document = JsonDocument.Parse(ReadBody(context));
        JsonElement root = document.RootElement;
        Assert.Equal(403, root.GetProperty("status").GetInt32());
        Assert.Equal("Forbidden", root.GetProperty("title").GetString());
        Assert.Equal("AuthorizationFailed", root.GetProperty("code").GetString());
        Assert.Equal("https://lasthour.dev/errors/forbidden", root.GetProperty("type").GetString());

        // No internal authorization details are exposed.
        Assert.False(root.TryGetProperty("permissions", out _));
        Assert.False(root.TryGetProperty("roles", out _));
    }

    [Fact]
    public async Task Handle_Forbidden_IncludesCorrelationIdWhenPresent()
    {
        var handler = new AuthorizationProblemDetailsResultHandler();
        HttpContext context = CreateContext();
        context.Items[LastHour.Api.Middleware.CorrelationIdDefaults.ContextKey] = "trace-123";

        await handler.HandleAsync(
            _ => Task.CompletedTask,
            context,
            Policy,
            PolicyAuthorizationResult.Forbid());

        using JsonDocument document = JsonDocument.Parse(ReadBody(context));
        Assert.Equal("trace-123", document.RootElement.GetProperty("correlationId").GetString());
    }

    [Fact]
    public async Task Handle_Challenged_DelegatesToDefaultHandlerPreserving401()
    {
        var handler = new AuthorizationProblemDetailsResultHandler();
        HttpContext context = CreateContext();
        var authentication = (StubAuthenticationService)context.RequestServices
            .GetRequiredService<IAuthenticationService>();

        await handler.HandleAsync(
            _ => throw new InvalidOperationException("Next must not be invoked for a challenge."),
            context,
            Policy,
            PolicyAuthorizationResult.Challenge());

        Assert.True(authentication.ChallengeInvoked);
        Assert.Equal(StatusCodes.Status401Unauthorized, context.Response.StatusCode);
    }
}
