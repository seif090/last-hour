using System.Security.Claims;
using LastHour.Api.ProblemDetails;
using LastHour.Api.Security.Authentication;
using LastHour.Api.Versioning;
using LastHour.BuildingBlocks.SharedKernel.Results;
using LastHour.Modules.Identity.Application.Commands;
using LastHour.Modules.Identity.Application.Queries;
using MediatR;

namespace LastHour.Api.Endpoints.Authentication;

/// <summary>
/// Maps the authentication surface: login (credential verification plus access and refresh
/// token issuance), refresh (atomic token rotation), logout (session revocation, idempotent),
/// and the current-user endpoint that verifies authenticated access by echoing the subject
/// identifier from the validated access token. Every response carries only safe data — no
/// password material, hashes, security state or internal details — and every authentication
/// failure surfaces the same safe problem details payload.
/// </summary>
public static class AuthEndpoints
{
    /// <summary>
    /// Maps the authentication endpoints onto the route table.
    /// </summary>
    /// <param name="app">The endpoint route builder to extend.</param>
    /// <returns>The same endpoint route builder, so calls can be chained.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="app"/> is <see langword="null"/>.</exception>
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.NewVersionedApi("Auth")
            .MapPost("api/v{version:apiVersion}/auth/login", async (
                LoginRequest request,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                Result<LoginResult> result = await sender.Send(
                    new LoginCommand(request.Email, request.Password),
                    cancellationToken);

                return result.Match(
                    onSuccess: login => Results.Ok(new TokenResponse(
                        login.AccessToken,
                        login.RefreshToken,
                        "Bearer",
                        ExpiresIn(login.ExpiresAtUtc))),
                    onFailure: error => Results.Problem(ErrorToProblemDetailsMapper.Map(error)));
            })
            .HasApiVersion(ApiVersions.V1)
            .WithName("Login")
            .WithTags("Auth")
            .Accepts<LoginRequest>("application/json")
            .Produces<TokenResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        app.NewVersionedApi("Auth")
            .MapPost("api/v{version:apiVersion}/auth/refresh", async (
                RefreshRequest request,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                Result<RefreshTokenResult> result = await sender.Send(
                    new RefreshTokenCommand(request.RefreshToken),
                    cancellationToken);

                return result.Match(
                    onSuccess: refresh => Results.Ok(new TokenResponse(
                        refresh.AccessToken,
                        refresh.RefreshToken,
                        "Bearer",
                        ExpiresIn(refresh.ExpiresAtUtc))),
                    onFailure: error => Results.Problem(ErrorToProblemDetailsMapper.Map(error)));
            })
            .HasApiVersion(ApiVersions.V1)
            .WithName("RefreshToken")
            .WithTags("Auth")
            .Accepts<RefreshRequest>("application/json")
            .Produces<TokenResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status409Conflict);

        app.NewVersionedApi("Auth")
            .MapPost("api/v{version:apiVersion}/auth/logout", async (
                LogoutRequest request,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                Result result = await sender.Send(
                    new LogoutCommand(request.RefreshToken),
                    cancellationToken);

                return result.Match(
                    onSuccess: () => Results.NoContent(),
                    onFailure: error => Results.Problem(ErrorToProblemDetailsMapper.Map(error)));
            })
            .HasApiVersion(ApiVersions.V1)
            .WithName("Logout")
            .WithTags("Auth")
            .Accepts<LogoutRequest>("application/json")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest);

        app.NewVersionedApi("Auth")
            .MapGet("api/v{version:apiVersion}/auth/me", (ClaimsPrincipal user) =>
            {
                Guid? userId = user.GetUserId();

                return userId is null
                    ? Results.Problem(ErrorToProblemDetailsMapper.Map(Error.Unauthorized(
                        "AuthenticationFailed",
                        "The access token does not identify a subject.")))
                    : Results.Ok(new CurrentUserResponse(userId.Value));
            })
            .RequireAuthorization()
            .HasApiVersion(ApiVersions.V1)
            .WithName("GetCurrentUser")
            .WithTags("Auth")
            .Produces<CurrentUserResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        app.NewVersionedApi("Auth")
            .MapPost("api/v{version:apiVersion}/auth/change-password", async (
                ChangePasswordRequest request,
                ClaimsPrincipal user,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                Guid? userId = user.GetUserId();
                if (userId is null)
                {
                    return Results.Problem(ErrorToProblemDetailsMapper.Map(Error.Unauthorized(
                        "AuthenticationFailed",
                        "The access token does not identify a subject.")));
                }

                Result result = await sender.Send(
                    new ChangePasswordCommand(userId.Value, request.CurrentPassword, request.NewPassword),
                    cancellationToken);

                return result.Match(
                    onSuccess: () => Results.NoContent(),
                    onFailure: error => Results.Problem(ErrorToProblemDetailsMapper.Map(error)));
            })
            .RequireAuthorization()
            .HasApiVersion(ApiVersions.V1)
            .WithName("ChangePassword")
            .WithTags("Auth")
            .Accepts<ChangePasswordRequest>("application/json")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        app.NewVersionedApi("Auth")
            .MapPost("api/v{version:apiVersion}/auth/logout-all", async (
                ClaimsPrincipal user,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                Guid? userId = user.GetUserId();
                if (userId is null)
                {
                    return Results.Problem(ErrorToProblemDetailsMapper.Map(Error.Unauthorized(
                        "AuthenticationFailed",
                        "The access token does not identify a subject.")));
                }

                Result result = await sender.Send(new LogoutAllCommand(userId.Value), cancellationToken);

                return result.Match(
                    onSuccess: () => Results.NoContent(),
                    onFailure: error => Results.Problem(ErrorToProblemDetailsMapper.Map(error)));
            })
            .RequireAuthorization()
            .HasApiVersion(ApiVersions.V1)
            .WithName("LogoutAll")
            .WithTags("Auth")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        app.NewVersionedApi("Auth")
            .MapGet("api/v{version:apiVersion}/auth/sessions", async (
                ClaimsPrincipal user,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                Guid? userId = user.GetUserId();
                if (userId is null)
                {
                    return Results.Problem(ErrorToProblemDetailsMapper.Map(Error.Unauthorized(
                        "AuthenticationFailed",
                        "The access token does not identify a subject.")));
                }

                Result<IReadOnlyList<SessionInfo>> result = await sender.Send(
                    new GetSessionsQuery(userId.Value),
                    cancellationToken);

                return result.Match(
                    onSuccess: sessions => Results.Ok(sessions.Select(session => new SessionResponse(
                        session.SessionId,
                        session.CreatedAtUtc,
                        session.ExpiresAtUtc,
                        session.IsActive))),
                    onFailure: error => Results.Problem(ErrorToProblemDetailsMapper.Map(error)));
            })
            .RequireAuthorization()
            .HasApiVersion(ApiVersions.V1)
            .WithName("GetSessions")
            .WithTags("Auth")
            .Produces<List<SessionResponse>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        return app;
    }

    private static int ExpiresIn(DateTimeOffset expiresAtUtc)
        => (int)Math.Ceiling((expiresAtUtc - DateTimeOffset.UtcNow).TotalSeconds);

    /// <summary>
    /// The client-controlled login payload. Only the email and password are accepted.
    /// </summary>
    public sealed class LoginRequest
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LoginRequest"/> class.
        /// </summary>
        /// <param name="email">The account email address.</param>
        /// <param name="password">The plaintext password to verify.</param>
        public LoginRequest(string email, string password)
        {
            Email = email;
            Password = password;
        }

        /// <summary>
        /// Gets the account email address.
        /// </summary>
        public string Email { get; }

        /// <summary>
        /// Gets the plaintext password.
        /// </summary>
        public string Password { get; }
    }

    /// <summary>
    /// The client-controlled refresh payload: the raw refresh token to rotate.
    /// </summary>
    public sealed class RefreshRequest
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RefreshRequest"/> class.
        /// </summary>
        /// <param name="refreshToken">The raw refresh token to validate and rotate.</param>
        public RefreshRequest(string refreshToken)
        {
            RefreshToken = refreshToken;
        }

        /// <summary>
        /// Gets the raw refresh token.
        /// </summary>
        public string RefreshToken { get; }
    }

    /// <summary>
    /// The client-controlled logout payload: the raw refresh token of the session to end.
    /// </summary>
    public sealed class LogoutRequest
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LogoutRequest"/> class.
        /// </summary>
        /// <param name="refreshToken">The raw refresh token of the session to end.</param>
        public LogoutRequest(string refreshToken)
        {
            RefreshToken = refreshToken;
        }

        /// <summary>
        /// Gets the raw refresh token.
        /// </summary>
        public string RefreshToken { get; }
    }

    /// <summary>
    /// The client-controlled password-change payload. Only the two passwords are accepted — the
    /// user identity comes from the validated access token, never from this body.
    /// </summary>
    public sealed class ChangePasswordRequest
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ChangePasswordRequest"/> class.
        /// </summary>
        /// <param name="currentPassword">The current password, verified before any change.</param>
        /// <param name="newPassword">The new password, validated against the policy before hashing.</param>
        public ChangePasswordRequest(string currentPassword, string newPassword)
        {
            CurrentPassword = currentPassword;
            NewPassword = newPassword;
        }

        /// <summary>
        /// Gets the current password to verify.
        /// </summary>
        public string CurrentPassword { get; }

        /// <summary>
        /// Gets the new password to validate and store.
        /// </summary>
        public string NewPassword { get; }
    }

    /// <summary>
    /// The safe metadata of one session exposed by the session-inspection endpoint. It carries
    /// no token material, hashes or internal state.
    /// </summary>
    public sealed class SessionResponse
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SessionResponse"/> class.
        /// </summary>
        /// <param name="sessionId">The session identifier.</param>
        /// <param name="createdAtUtc">The moment the session was created, in UTC.</param>
        /// <param name="expiresAtUtc">The moment the session expires, in UTC.</param>
        /// <param name="active">Whether the session can currently be used to refresh.</param>
        public SessionResponse(Guid sessionId, DateTime createdAtUtc, DateTime expiresAtUtc, bool active)
        {
            SessionId = sessionId;
            CreatedAtUtc = createdAtUtc;
            ExpiresAtUtc = expiresAtUtc;
            Active = active;
        }

        /// <summary>
        /// Gets the session identifier.
        /// </summary>
        public Guid SessionId { get; }

        /// <summary>
        /// Gets the moment the session was created, in UTC.
        /// </summary>
        public DateTime CreatedAtUtc { get; }

        /// <summary>
        /// Gets the moment the session expires, in UTC.
        /// </summary>
        public DateTime ExpiresAtUtc { get; }

        /// <summary>
        /// Gets a value indicating whether the session can currently be used to refresh.
        /// </summary>
        public bool Active { get; }
    }

    /// <summary>
    /// The safe token response: the access token, the raw refresh token, the bearer type and
    /// the access-token lifetime in seconds. No user data or security state is included.
    /// </summary>
    public sealed class TokenResponse
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TokenResponse"/> class.
        /// </summary>
        /// <param name="accessToken">The issued access token.</param>
        /// <param name="refreshToken">The issued refresh token.</param>
        /// <param name="tokenType">The token type, always <c>Bearer</c>.</param>
        /// <param name="expiresIn">The access-token lifetime in seconds.</param>
        public TokenResponse(string accessToken, string refreshToken, string tokenType, int expiresIn)
        {
            AccessToken = accessToken;
            RefreshToken = refreshToken;
            TokenType = tokenType;
            ExpiresIn = expiresIn;
        }

        /// <summary>
        /// Gets the issued access token.
        /// </summary>
        public string AccessToken { get; }

        /// <summary>
        /// Gets the issued refresh token.
        /// </summary>
        public string RefreshToken { get; }

        /// <summary>
        /// Gets the token type.
        /// </summary>
        public string TokenType { get; }

        /// <summary>
        /// Gets the access-token lifetime in seconds.
        /// </summary>
        public int ExpiresIn { get; }
    }

    /// <summary>
    /// The authenticated caller's identifier, echoed from the validated token. This endpoint
    /// exposes nothing else by design.
    /// </summary>
    public sealed class CurrentUserResponse
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CurrentUserResponse"/> class.
        /// </summary>
        /// <param name="userId">The authenticated user's identifier.</param>
        public CurrentUserResponse(Guid userId)
        {
            UserId = userId;
        }

        /// <summary>
        /// Gets the authenticated user's identifier.
        /// </summary>
        public Guid UserId { get; }
    }
}
