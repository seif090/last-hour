using LastHour.Api.ProblemDetails;
using LastHour.Api.Security.Authorization;
using LastHour.Api.Versioning;
using LastHour.BuildingBlocks.SharedKernel.Results;
using LastHour.Modules.Catalog.Application.Commands;
using LastHour.Modules.Catalog.Application.Models;
using LastHour.Modules.Catalog.Application.Queries.Categories;
using LastHour.Modules.Catalog.Domain.Categories;
using LastHour.Modules.Identity.Application.Permissions;
using MediatR;

namespace LastHour.Api.Endpoints.Catalog;

/// <summary>
/// Maps the catalog category surface: create, list, get by identifier and update. Every route
/// is protected by permission-based authorization: read operations require
/// <c>Catalog.Read</c> and mutating operations require <c>Catalog.Manage</c>. The endpoints
/// stay thin — they bind HTTP parameters, dispatch through MediatR and map the outcome onto
/// the HTTP surface. No business logic, no persistence access and no manual validation live
/// here.
/// </summary>
public static class CategoryEndpoints
{
    /// <summary>
    /// Maps the catalog category endpoints onto the route table.
    /// </summary>
    /// <param name="app">The endpoint route builder to extend.</param>
    /// <returns>The same endpoint route builder, so calls can be chained.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="app"/> is <see langword="null"/>.</exception>
    public static IEndpointRouteBuilder MapCategoryEndpoints(this IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.NewVersionedApi("Catalog")
            .MapPost("api/v{version:apiVersion}/categories", async (
                CreateCategoryRequest request,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                Result<CategoryId> result = await sender.Send(
                    new CreateCategoryCommand(request.Name, request.Description, request.ParentCategoryId),
                    cancellationToken);

                return result.Match(
                    onSuccess: categoryId => Results.Json(
                        new CategoryCreatedResponse(categoryId.Value, request.Name),
                        statusCode: StatusCodes.Status201Created),
                    onFailure: error => Results.Problem(ErrorToProblemDetailsMapper.Map(error)));
            })
            .RequirePermission(PermissionCatalog.CatalogManage.Code)
            .HasApiVersion(ApiVersions.V1)
            .WithName("CreateCategory")
            .WithTags("Catalog")
            .Accepts<CreateCategoryRequest>("application/json")
            .Produces<CategoryCreatedResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden);

        app.NewVersionedApi("Catalog")
            .MapGet("api/v{version:apiVersion}/categories", async (
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                Result<IReadOnlyList<CategoryDto>> result = await sender.Send(
                    new GetCategoriesQuery(),
                    cancellationToken);

                return result.Match(
                    onSuccess: categories => Results.Ok(categories),
                    onFailure: error => Results.Problem(ErrorToProblemDetailsMapper.Map(error)));
            })
            .RequirePermission(PermissionCatalog.CatalogRead.Code)
            .HasApiVersion(ApiVersions.V1)
            .WithName("GetCategories")
            .WithTags("Catalog")
            .Produces<IReadOnlyList<CategoryDto>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden);

        app.NewVersionedApi("Catalog")
            .MapGet("api/v{version:apiVersion}/categories/{id:guid}", async (
                Guid id,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                Result<CategoryDto> result = await sender.Send(
                    new GetCategoryByIdQuery(id),
                    cancellationToken);

                return result.Match(
                    onSuccess: category => Results.Ok(category),
                    onFailure: error => Results.Problem(ErrorToProblemDetailsMapper.Map(error)));
            })
            .RequirePermission(PermissionCatalog.CatalogRead.Code)
            .HasApiVersion(ApiVersions.V1)
            .WithName("GetCategoryById")
            .WithTags("Catalog")
            .Produces<CategoryDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);

        app.NewVersionedApi("Catalog")
            .MapPut("api/v{version:apiVersion}/categories/{id:guid}", async (
                Guid id,
                UpdateCategoryRequest request,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                Result result = await sender.Send(
                    new UpdateCategoryCommand(id, request.Name, request.Description, request.ParentCategoryId),
                    cancellationToken);

                return result.Match(
                    onSuccess: () => Results.NoContent(),
                    onFailure: error => Results.Problem(ErrorToProblemDetailsMapper.Map(error)));
            })
            .RequirePermission(PermissionCatalog.CatalogManage.Code)
            .HasApiVersion(ApiVersions.V1)
            .WithName("UpdateCategory")
            .WithTags("Catalog")
            .Accepts<UpdateCategoryRequest>("application/json")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);

        return app;
    }

    /// <summary>
    /// The client-controlled category-creation payload. Only the name and optional fields are
    /// accepted; identity and hierarchy validation are owned by the domain.
    /// </summary>
    public sealed class CreateCategoryRequest
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CreateCategoryRequest"/> class.
        /// </summary>
        /// <param name="name">The category name.</param>
        /// <param name="description">An optional human-readable description.</param>
        /// <param name="parentCategoryId">An optional parent category identifier.</param>
        public CreateCategoryRequest(string name, string? description, Guid? parentCategoryId)
        {
            Name = name;
            Description = description;
            ParentCategoryId = parentCategoryId;
        }

        /// <summary>
        /// Gets the category name.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the optional human-readable description.
        /// </summary>
        public string? Description { get; }

        /// <summary>
        /// Gets the optional parent category identifier.
        /// </summary>
        public Guid? ParentCategoryId { get; }
    }

    /// <summary>
    /// The payload confirming a category was created. It carries the stable identifier and the
    /// display name only — no internal persistence state.
    /// </summary>
    public sealed class CategoryCreatedResponse
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CategoryCreatedResponse"/> class.
        /// </summary>
        /// <param name="categoryId">The created category's identifier.</param>
        /// <param name="name">The created category's display name.</param>
        public CategoryCreatedResponse(Guid categoryId, string name)
        {
            CategoryId = categoryId;
            Name = name;
        }

        /// <summary>
        /// Gets the created category's identifier.
        /// </summary>
        public Guid CategoryId { get; }

        /// <summary>
        /// Gets the created category's display name.
        /// </summary>
        public string Name { get; }
    }

    /// <summary>
    /// The client-controlled category-update payload.
    /// </summary>
    public sealed class UpdateCategoryRequest
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="UpdateCategoryRequest"/> class.
        /// </summary>
        /// <param name="name">The updated category name.</param>
        /// <param name="description">The updated optional description.</param>
        /// <param name="parentCategoryId">The updated optional parent category identifier.</param>
        public UpdateCategoryRequest(string name, string? description, Guid? parentCategoryId)
        {
            Name = name;
            Description = description;
            ParentCategoryId = parentCategoryId;
        }

        /// <summary>
        /// Gets the updated category name.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the updated optional description.
        /// </summary>
        public string? Description { get; }

        /// <summary>
        /// Gets the updated optional parent category identifier.
        /// </summary>
        public Guid? ParentCategoryId { get; }
    }
}
