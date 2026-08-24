using LastHour.Api.ProblemDetails;
using LastHour.Api.Security.Authorization;
using LastHour.Api.Versioning;
using LastHour.BuildingBlocks.SharedKernel.Results;
using LastHour.Modules.Identity.Application.Commands;
using LastHour.Modules.Identity.Application.Permissions;
using LastHour.Modules.Identity.Application.Queries;
using LastHour.Modules.Identity.Domain.Roles;
using MediatR;

namespace LastHour.Api.Endpoints.Authorization;

/// <summary>
/// Maps the role-management surface — the first endpoints protected by the permission-based
/// authorization enforcement. Role creation and role assignment are privilege-sensitive
/// operations (they shape what other users can do), so every route carries an explicit
/// <see cref="RequirePermissionAttribute"/> policy resolved from the centralized permission
/// catalog: <c>Roles.Manage</c> for mutating operations and <c>Roles.Read</c> for reads.
/// Identity for the operations comes from the validated access token; the target user and role
/// identifiers come from the route. The endpoints stay deliberately small — no user CRUD, no
/// permission-grant surface — matching exactly the use cases the identity module exposes.
/// </summary>
public static class AuthorizationEndpoints
{
    /// <summary>
    /// Maps the authorization endpoints onto the route table.
    /// </summary>
    /// <param name="app">The endpoint route builder to extend.</param>
    /// <returns>The same endpoint route builder, so calls can be chained.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="app"/> is <see langword="null"/>.</exception>
    public static IEndpointRouteBuilder MapAuthorizationEndpoints(this IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.NewVersionedApi("Roles")
            .MapPost("api/v{version:apiVersion}/roles", async (
                CreateRoleRequest request,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                Result<RoleId> result = await sender.Send(
                    new CreateRoleCommand(request.Name, request.Description),
                    cancellationToken);

                return result.Match(
                    onSuccess: roleId => Results.Json(
                        new RoleCreatedResponse(roleId.Value, request.Name),
                        statusCode: StatusCodes.Status201Created),
                    onFailure: error => Results.Problem(ErrorToProblemDetailsMapper.Map(error)));
            })
            .RequirePermission(PermissionCatalog.RolesManage.Code)
            .HasApiVersion(ApiVersions.V1)
            .WithName("CreateRole")
            .WithTags("Roles")
            .Accepts<CreateRoleRequest>("application/json")
            .Produces<RoleCreatedResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden);

        app.NewVersionedApi("Roles")
            .MapGet("api/v{version:apiVersion}/users/{userId}/roles", async (
                Guid userId,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                Result<IReadOnlyList<RoleInfo>> result = await sender.Send(
                    new GetUserRolesQuery(userId),
                    cancellationToken);

                return result.Match(
                    onSuccess: roles => Results.Ok(roles.Select(role => new RoleResponse(
                        role.RoleId,
                        role.Name))),
                    onFailure: error => Results.Problem(ErrorToProblemDetailsMapper.Map(error)));
            })
            .RequirePermission(PermissionCatalog.RolesRead.Code)
            .HasApiVersion(ApiVersions.V1)
            .WithName("GetUserRoles")
            .WithTags("Roles")
            .Produces<List<RoleResponse>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);

        app.NewVersionedApi("Roles")
            .MapPut("api/v{version:apiVersion}/users/{userId}/roles/{roleId}", async (
                Guid userId,
                Guid roleId,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                Result result = await sender.Send(
                    new AssignRoleToUserCommand(userId, roleId),
                    cancellationToken);

                return result.Match(
                    onSuccess: () => Results.NoContent(),
                    onFailure: error => Results.Problem(ErrorToProblemDetailsMapper.Map(error)));
            })
            .RequirePermission(PermissionCatalog.RolesManage.Code)
            .HasApiVersion(ApiVersions.V1)
            .WithName("AssignRoleToUser")
            .WithTags("Roles")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        app.NewVersionedApi("Roles")
            .MapDelete("api/v{version:apiVersion}/users/{userId}/roles/{roleId}", async (
                Guid userId,
                Guid roleId,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                Result result = await sender.Send(
                    new RemoveRoleFromUserCommand(userId, roleId),
                    cancellationToken);

                return result.Match(
                    onSuccess: () => Results.NoContent(),
                    onFailure: error => Results.Problem(ErrorToProblemDetailsMapper.Map(error)));
            })
            .RequirePermission(PermissionCatalog.RolesManage.Code)
            .HasApiVersion(ApiVersions.V1)
            .WithName("RemoveRoleFromUser")
            .WithTags("Roles")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        return app;
    }

    /// <summary>
    /// The client-controlled role-creation payload. Only the name and an optional description
    /// are accepted; role identity and normalization are owned by the domain.
    /// </summary>
    public sealed class CreateRoleRequest
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CreateRoleRequest"/> class.
        /// </summary>
        /// <param name="name">The role name, normalized by the domain's single rule.</param>
        /// <param name="description">An optional human-readable purpose.</param>
        public CreateRoleRequest(string name, string? description)
        {
            Name = name;
            Description = description;
        }

        /// <summary>
        /// Gets the role name.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the optional human-readable purpose.
        /// </summary>
        public string? Description { get; }
    }

    /// <summary>
    /// The safe projection of a role returned to callers.
    /// </summary>
    public sealed class RoleResponse
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RoleResponse"/> class.
        /// </summary>
        /// <param name="roleId">The role identifier.</param>
        /// <param name="name">The role display name.</param>
        public RoleResponse(Guid roleId, string name)
        {
            RoleId = roleId;
            Name = name;
        }

        /// <summary>
        /// Gets the role identifier.
        /// </summary>
        public Guid RoleId { get; }

        /// <summary>
        /// Gets the role display name.
        /// </summary>
        public string Name { get; }
    }

    /// <summary>
    /// The payload confirming a role was created. It carries the stable role identifier and the
    /// display name only — no internal persistence state.
    /// </summary>
    public sealed class RoleCreatedResponse
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RoleCreatedResponse"/> class.
        /// </summary>
        /// <param name="roleId">The created role's identifier.</param>
        /// <param name="name">The created role's display name.</param>
        public RoleCreatedResponse(Guid roleId, string name)
        {
            RoleId = roleId;
            Name = name;
        }

        /// <summary>
        /// Gets the created role's identifier.
        /// </summary>
        public Guid RoleId { get; }

        /// <summary>
        /// Gets the created role's display name.
        /// </summary>
        public string Name { get; }
    }
}
