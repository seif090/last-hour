using LastHour.Api.ProblemDetails;
using LastHour.Api.Security.Authorization;
using LastHour.Api.Versioning;
using LastHour.BuildingBlocks.SharedKernel.Results;
using LastHour.Modules.Catalog.Application.Commands;
using LastHour.Modules.Catalog.Application.Models;
using LastHour.Modules.Catalog.Application.Queries.Products;
using LastHour.Modules.Catalog.Domain.Products;
using LastHour.Modules.Identity.Application.Permissions;
using MediatR;

namespace LastHour.Api.Endpoints.Catalog;

/// <summary>
/// Maps the catalog product surface: create, get by identifier, search, update, price change,
/// SKU change, category re-assignment and soft delete. Every route is protected by
/// permission-based authorization: read operations require <c>Catalog.Read</c> and mutating
/// operations require <c>Catalog.Manage</c>. Endpoints bind HTTP parameters, dispatch through
/// MediatR and map the outcome onto the HTTP surface. No business logic, no persistence access
/// and no manual validation live here.
/// </summary>
public static class ProductEndpoints
{
    /// <summary>
    /// Maps the catalog product endpoints onto the route table.
    /// </summary>
    /// <param name="app">The endpoint route builder to extend.</param>
    /// <returns>The same endpoint route builder, so calls can be chained.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="app"/> is <see langword="null"/>.</exception>
    public static IEndpointRouteBuilder MapProductEndpoints(this IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.NewVersionedApi("Catalog")
            .MapPost("api/v{version:apiVersion}/products", async (
                CreateProductRequest request,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                Result<ProductId> result = await sender.Send(
                    new CreateProductCommand(
                        request.Sku,
                        request.Name,
                        request.PriceAmount,
                        request.PriceCurrency,
                        request.CategoryId,
                        request.Description),
                    cancellationToken);

                return result.Match(
                    onSuccess: productId => Results.Json(
                        new ProductCreatedResponse(productId.Value, request.Sku),
                        statusCode: StatusCodes.Status201Created),
                    onFailure: error => Results.Problem(ErrorToProblemDetailsMapper.Map(error)));
            })
            .RequirePermission(PermissionCatalog.CatalogManage.Code)
            .HasApiVersion(ApiVersions.V1)
            .WithName("CreateProduct")
            .WithTags("Catalog")
            .Accepts<CreateProductRequest>("application/json")
            .Produces<ProductCreatedResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden);

        app.NewVersionedApi("Catalog")
            .MapGet("api/v{version:apiVersion}/products/{id:guid}", async (
                Guid id,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                Result<ProductDto> result = await sender.Send(
                    new GetProductByIdQuery(id),
                    cancellationToken);

                return result.Match(
                    onSuccess: product => Results.Ok(product),
                    onFailure: error => Results.Problem(ErrorToProblemDetailsMapper.Map(error)));
            })
            .RequirePermission(PermissionCatalog.CatalogRead.Code)
            .HasApiVersion(ApiVersions.V1)
            .WithName("GetProductById")
            .WithTags("Catalog")
            .Produces<ProductDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);

        app.NewVersionedApi("Catalog")
            .MapGet("api/v{version:apiVersion}/products", async (
                int? pageNumber,
                int? pageSize,
                string? search,
                Guid? categoryId,
                string? sortBy,
                string? sortDirection,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                Result<PagedResult<ProductDto>> result = await sender.Send(
                    new SearchProductsQuery(
                        pageNumber ?? 1,
                        pageSize ?? SearchProductsQuery.DefaultPageSize,
                        search,
                        categoryId,
                        sortBy ?? SearchProductsQuery.DefaultSortBy,
                        sortDirection ?? SearchProductsQuery.DefaultSortDirection),
                    cancellationToken);

                return result.Match(
                    onSuccess: paged => Results.Ok(paged),
                    onFailure: error => Results.Problem(ErrorToProblemDetailsMapper.Map(error)));
            })
            .RequirePermission(PermissionCatalog.CatalogRead.Code)
            .HasApiVersion(ApiVersions.V1)
            .WithName("SearchProducts")
            .WithTags("Catalog")
            .Produces<PagedResult<ProductDto>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden);

        app.NewVersionedApi("Catalog")
            .MapPut("api/v{version:apiVersion}/products/{id:guid}", async (
                Guid id,
                UpdateProductRequest request,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                Result result = await sender.Send(
                    new UpdateProductCommand(id, request.Name, request.Description),
                    cancellationToken);

                return result.Match(
                    onSuccess: () => Results.NoContent(),
                    onFailure: error => Results.Problem(ErrorToProblemDetailsMapper.Map(error)));
            })
            .RequirePermission(PermissionCatalog.CatalogManage.Code)
            .HasApiVersion(ApiVersions.V1)
            .WithName("UpdateProduct")
            .WithTags("Catalog")
            .Accepts<UpdateProductRequest>("application/json")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);

        app.NewVersionedApi("Catalog")
            .MapPut("api/v{version:apiVersion}/products/{id:guid}/price", async (
                Guid id,
                ChangeProductPriceRequest request,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                Result result = await sender.Send(
                    new ChangeProductPriceCommand(id, request.Amount, request.Currency),
                    cancellationToken);

                return result.Match(
                    onSuccess: () => Results.NoContent(),
                    onFailure: error => Results.Problem(ErrorToProblemDetailsMapper.Map(error)));
            })
            .RequirePermission(PermissionCatalog.CatalogManage.Code)
            .HasApiVersion(ApiVersions.V1)
            .WithName("ChangeProductPrice")
            .WithTags("Catalog")
            .Accepts<ChangeProductPriceRequest>("application/json")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);

        app.NewVersionedApi("Catalog")
            .MapPut("api/v{version:apiVersion}/products/{id:guid}/sku", async (
                Guid id,
                ChangeProductSkuRequest request,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                Result result = await sender.Send(
                    new ChangeProductSkuCommand(id, request.Sku),
                    cancellationToken);

                return result.Match(
                    onSuccess: () => Results.NoContent(),
                    onFailure: error => Results.Problem(ErrorToProblemDetailsMapper.Map(error)));
            })
            .RequirePermission(PermissionCatalog.CatalogManage.Code)
            .HasApiVersion(ApiVersions.V1)
            .WithName("ChangeProductSku")
            .WithTags("Catalog")
            .Accepts<ChangeProductSkuRequest>("application/json")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);

        app.NewVersionedApi("Catalog")
            .MapPut("api/v{version:apiVersion}/products/{id:guid}/category", async (
                Guid id,
                ChangeProductCategoryRequest request,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                Result result = await sender.Send(
                    new ChangeProductCategoryCommand(id, request.CategoryId),
                    cancellationToken);

                return result.Match(
                    onSuccess: () => Results.NoContent(),
                    onFailure: error => Results.Problem(ErrorToProblemDetailsMapper.Map(error)));
            })
            .RequirePermission(PermissionCatalog.CatalogManage.Code)
            .HasApiVersion(ApiVersions.V1)
            .WithName("ChangeProductCategory")
            .WithTags("Catalog")
            .Accepts<ChangeProductCategoryRequest>("application/json")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);

        app.NewVersionedApi("Catalog")
            .MapDelete("api/v{version:apiVersion}/products/{id:guid}", async (
                Guid id,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                Result result = await sender.Send(
                    new DeleteProductCommand(id),
                    cancellationToken);

                return result.Match(
                    onSuccess: () => Results.NoContent(),
                    onFailure: error => Results.Problem(ErrorToProblemDetailsMapper.Map(error)));
            })
            .RequirePermission(PermissionCatalog.CatalogManage.Code)
            .HasApiVersion(ApiVersions.V1)
            .WithName("DeleteProduct")
            .WithTags("Catalog")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);

        return app;
    }

    /// <summary>
    /// The client-controlled product-creation payload.
    /// </summary>
    public sealed class CreateProductRequest
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CreateProductRequest"/> class.
        /// </summary>
        /// <param name="sku">The product Stock Keeping Unit (SKU).</param>
        /// <param name="name">The product name.</param>
        /// <param name="priceAmount">The base price amount.</param>
        /// <param name="priceCurrency">The ISO 4217 currency code.</param>
        /// <param name="categoryId">The identifier of the category this product belongs to.</param>
        /// <param name="description">An optional product description.</param>
        public CreateProductRequest(
            string sku,
            string name,
            decimal priceAmount,
            string priceCurrency,
            Guid categoryId,
            string? description)
        {
            Sku = sku;
            Name = name;
            PriceAmount = priceAmount;
            PriceCurrency = priceCurrency;
            CategoryId = categoryId;
            Description = description;
        }

        /// <summary>
        /// Gets the product SKU.
        /// </summary>
        public string Sku { get; }

        /// <summary>
        /// Gets the product name.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the base price amount.
        /// </summary>
        public decimal PriceAmount { get; }

        /// <summary>
        /// Gets the ISO 4217 currency code.
        /// </summary>
        public string PriceCurrency { get; }

        /// <summary>
        /// Gets the category identifier.
        /// </summary>
        public Guid CategoryId { get; }

        /// <summary>
        /// Gets the optional product description.
        /// </summary>
        public string? Description { get; }
    }

    /// <summary>
    /// The payload confirming a product was created. It carries the stable identifier and the
    /// SKU only — no internal persistence state.
    /// </summary>
    public sealed class ProductCreatedResponse
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ProductCreatedResponse"/> class.
        /// </summary>
        /// <param name="productId">The created product's identifier.</param>
        /// <param name="sku">The product's Stock Keeping Unit.</param>
        public ProductCreatedResponse(Guid productId, string sku)
        {
            ProductId = productId;
            Sku = sku;
        }

        /// <summary>
        /// Gets the created product's identifier.
        /// </summary>
        public Guid ProductId { get; }

        /// <summary>
        /// Gets the product's Stock Keeping Unit.
        /// </summary>
        public string Sku { get; }
    }

    /// <summary>
    /// The client-controlled product-update payload.
    /// </summary>
    public sealed class UpdateProductRequest
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="UpdateProductRequest"/> class.
        /// </summary>
        /// <param name="name">The updated product name.</param>
        /// <param name="description">The updated optional description.</param>
        public UpdateProductRequest(string name, string? description)
        {
            Name = name;
            Description = description;
        }

        /// <summary>
        /// Gets the updated product name.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the updated optional description.
        /// </summary>
        public string? Description { get; }
    }

    /// <summary>
    /// The client-controlled price-change payload.
    /// </summary>
    public sealed class ChangeProductPriceRequest
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ChangeProductPriceRequest"/> class.
        /// </summary>
        /// <param name="amount">The new price amount.</param>
        /// <param name="currency">The ISO 4217 currency code.</param>
        public ChangeProductPriceRequest(decimal amount, string currency)
        {
            Amount = amount;
            Currency = currency;
        }

        /// <summary>
        /// Gets the new price amount.
        /// </summary>
        public decimal Amount { get; }

        /// <summary>
        /// Gets the ISO 4217 currency code.
        /// </summary>
        public string Currency { get; }
    }

    /// <summary>
    /// The client-controlled SKU-change payload.
    /// </summary>
    public sealed class ChangeProductSkuRequest
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ChangeProductSkuRequest"/> class.
        /// </summary>
        /// <param name="sku">The new SKU.</param>
        public ChangeProductSkuRequest(string sku)
        {
            Sku = sku;
        }

        /// <summary>
        /// Gets the new SKU.
        /// </summary>
        public string Sku { get; }
    }

    /// <summary>
    /// The client-controlled category-change payload.
    /// </summary>
    public sealed class ChangeProductCategoryRequest
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ChangeProductCategoryRequest"/> class.
        /// </summary>
        /// <param name="categoryId">The new category identifier.</param>
        public ChangeProductCategoryRequest(Guid categoryId)
        {
            CategoryId = categoryId;
        }

        /// <summary>
        /// Gets the new category identifier.
        /// </summary>
        public Guid CategoryId { get; }
    }
}
