using LastHour.Api.ProblemDetails;
using LastHour.Api.Versioning;
using LastHour.BuildingBlocks.SharedKernel.Results;
using LastHour.Modules.Identity.Application.Commands;
using LastHour.Modules.Identity.Domain.Users;
using MediatR;

namespace LastHour.Api.Endpoints.Registration;

/// <summary>
/// Maps the registration endpoint. The endpoint is a thin orchestration point: it binds the
/// request, dispatches the application use case through the CQRS pipeline, and maps the
/// outcome onto the HTTP surface — <c>201 Created</c> with only safe account information on
/// success, RFC 7807 problem details on failure. No password, hash or security state ever
/// appears in a response.
/// </summary>
public static class RegistrationEndpoints
{
    /// <summary>
    /// Maps the registration endpoint onto the route table.
    /// </summary>
    /// <param name="app">The endpoint route builder to extend.</param>
    /// <returns>The same endpoint route builder, so calls can be chained.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="app"/> is <see langword="null"/>.</exception>
    public static IEndpointRouteBuilder MapRegistrationEndpoints(this IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.NewVersionedApi("Auth")
            .MapPost("api/v{version:apiVersion}/auth/register", async (
                RegisterUserRequest request,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                Result<UserId> result = await sender.Send(
                    new RegisterUserCommand(request.Email, request.Password),
                    cancellationToken);

                return result.Match(
                    onSuccess: userId => Results.Json(
                        new RegisterUserResponse(userId.Value, AccountStatus.Pending.ToString()),
                        statusCode: StatusCodes.Status201Created),
                    onFailure: error => Results.Problem(ErrorToProblemDetailsMapper.Map(error)));
            })
            .HasApiVersion(ApiVersions.V1)
            .WithName("RegisterUser")
            .WithTags("Auth")
            .Accepts<RegisterUserRequest>("application/json")
            .Produces<RegisterUserResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status409Conflict);

        return app;
    }

    /// <summary>
    /// The client-controlled registration payload. Only the email and password are accepted;
    /// identifiers, status, hashes and security state are server-owned and cannot be supplied.
    /// </summary>
    public sealed class RegisterUserRequest
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RegisterUserRequest"/> class.
        /// </summary>
        /// <param name="email">The account email address.</param>
        /// <param name="password">The plaintext password.</param>
        public RegisterUserRequest(string email, string password)
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
    /// The safe registration response: the new account identifier and its initial status.
    /// </summary>
    public sealed class RegisterUserResponse
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RegisterUserResponse"/> class.
        /// </summary>
        /// <param name="userId">The identifier of the created account.</param>
        /// <param name="status">The initial account status.</param>
        public RegisterUserResponse(Guid userId, string status)
        {
            UserId = userId;
            Status = status;
        }

        /// <summary>
        /// Gets the identifier of the created account.
        /// </summary>
        public Guid UserId { get; }

        /// <summary>
        /// Gets the initial account status.
        /// </summary>
        public string Status { get; }
    }
}
