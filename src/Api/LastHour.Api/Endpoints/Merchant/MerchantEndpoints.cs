using System.Security.Claims;
using LastHour.Api.ProblemDetails;
using LastHour.Api.Security.Authentication;
using LastHour.Api.Security.Authorization;
using LastHour.Api.Versioning;
using LastHour.BuildingBlocks.SharedKernel.Results;
using LastHour.Modules.Identity.Application.Permissions;
using LastHour.Modules.Merchant.Application.Commands;
using LastHour.Modules.Merchant.Application.Models;
using LastHour.Modules.Merchant.Application.Queries;
using LastHour.Modules.Merchant.Domain.Merchants;
using MediatR;

namespace LastHour.Api.Endpoints.Merchant;

/// <summary>
/// Maps the merchant feature surface: registration, get by identifier, search, profile updates,
/// and lifecycle state transitions (verify, suspend, activate, close). Endpoints bind HTTP
/// parameters, dispatch through MediatR and map the outcome onto the HTTP surface. No business
/// logic, no persistence access and no manual validation live here.
/// </summary>
public static class MerchantEndpoints
{
    /// <summary>
    /// Maps the merchant endpoints onto the route table.
    /// </summary>
    /// <param name="app">The endpoint route builder to extend.</param>
    /// <returns>The same endpoint route builder, so calls can be chained.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="app"/> is <see langword="null"/>.</exception>
    public static IEndpointRouteBuilder MapMerchantEndpoints(this IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.NewVersionedApi("Merchants")
            .MapPost("api/v{version:apiVersion}/merchants", async (
                RegisterMerchantRequest request,
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

                Result<MerchantId> result = await sender.Send(
                    new RegisterMerchantCommand(
                        userId.Value,
                        request.Name,
                        request.Street,
                        request.City,
                        request.StateOrProvince,
                        request.PostalCode,
                        request.Country,
                        request.Phone),
                    cancellationToken);

                return result.Match(
                    onSuccess: merchantId => Results.Json(
                        new MerchantCreatedResponse(merchantId.Value, request.Name),
                        statusCode: StatusCodes.Status201Created),
                    onFailure: error => Results.Problem(ErrorToProblemDetailsMapper.Map(error)));
            })
            .RequireAuthorization()
            .HasApiVersion(ApiVersions.V1)
            .WithName("RegisterMerchant")
            .WithTags("Merchants")
            .Accepts<RegisterMerchantRequest>("application/json")
            .Produces<MerchantCreatedResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status409Conflict);

        app.NewVersionedApi("Merchants")
            .MapGet("api/v{version:apiVersion}/merchants/{id:guid}", async (
                Guid id,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                Result<MerchantDto> result = await sender.Send(
                    new GetMerchantByIdQuery(id),
                    cancellationToken);

                return result.Match(
                    onSuccess: merchant => Results.Ok(merchant),
                    onFailure: error => Results.Problem(ErrorToProblemDetailsMapper.Map(error)));
            })
            .RequirePermission(PermissionCatalog.MerchantsRead.Code)
            .HasApiVersion(ApiVersions.V1)
            .WithName("GetMerchantById")
            .WithTags("Merchants")
            .Produces<MerchantDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);

        app.NewVersionedApi("Merchants")
            .MapGet("api/v{version:apiVersion}/merchants", async (
                string? search,
                string? searchTerm,
                MerchantStatus? status,
                MerchantSortField? sortBy,
                MerchantSortDirection? sortDirection,
                int? page,
                int? pageNumber,
                int? pageSize,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                Result<PagedResult<MerchantDto>> result = await sender.Send(
                    new SearchMerchantsQuery(
                        search ?? searchTerm,
                        status,
                        sortBy ?? SearchMerchantsQuery.DefaultSortBy,
                        sortDirection ?? SearchMerchantsQuery.DefaultSortDirection,
                        page ?? pageNumber ?? 1,
                        pageSize ?? SearchMerchantsQuery.DefaultPageSize),
                    cancellationToken);

                return result.Match(
                    onSuccess: paged => Results.Ok(paged),
                    onFailure: error => Results.Problem(ErrorToProblemDetailsMapper.Map(error)));
            })
            .RequirePermission(PermissionCatalog.MerchantsRead.Code)
            .HasApiVersion(ApiVersions.V1)
            .WithName("SearchMerchants")
            .WithTags("Merchants")
            .Produces<PagedResult<MerchantDto>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden);

        app.NewVersionedApi("Merchants")
            .MapPut("api/v{version:apiVersion}/merchants/{id:guid}", async (
                Guid id,
                UpdateMerchantProfileRequest request,
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
                    new UpdateMerchantProfileCommand(
                        id,
                        userId.Value,
                        request.Name,
                        request.Street,
                        request.City,
                        request.StateOrProvince,
                        request.PostalCode,
                        request.Country,
                        request.Phone),
                    cancellationToken);

                return result.Match(
                    onSuccess: () => Results.NoContent(),
                    onFailure: error => Results.Problem(ErrorToProblemDetailsMapper.Map(error)));
            })
            .RequireAuthorization()
            .HasApiVersion(ApiVersions.V1)
            .WithName("UpdateMerchantProfile")
            .WithTags("Merchants")
            .Accepts<UpdateMerchantProfileRequest>("application/json")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);

        app.NewVersionedApi("Merchants")
            .MapPost("api/v{version:apiVersion}/merchants/{id:guid}/verify", async (
                Guid id,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                Result result = await sender.Send(
                    new VerifyMerchantCommand(id),
                    cancellationToken);

                return result.Match(
                    onSuccess: () => Results.NoContent(),
                    onFailure: error => Results.Problem(ErrorToProblemDetailsMapper.Map(error)));
            })
            .RequirePermission(PermissionCatalog.MerchantsManage.Code)
            .HasApiVersion(ApiVersions.V1)
            .WithName("VerifyMerchant")
            .WithTags("Merchants")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);

        app.NewVersionedApi("Merchants")
            .MapPost("api/v{version:apiVersion}/merchants/{id:guid}/suspend", async (
                Guid id,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                Result result = await sender.Send(
                    new SuspendMerchantCommand(id),
                    cancellationToken);

                return result.Match(
                    onSuccess: () => Results.NoContent(),
                    onFailure: error => Results.Problem(ErrorToProblemDetailsMapper.Map(error)));
            })
            .RequirePermission(PermissionCatalog.MerchantsManage.Code)
            .HasApiVersion(ApiVersions.V1)
            .WithName("SuspendMerchant")
            .WithTags("Merchants")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);

        app.NewVersionedApi("Merchants")
            .MapPost("api/v{version:apiVersion}/merchants/{id:guid}/activate", async (
                Guid id,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                Result result = await sender.Send(
                    new ActivateMerchantCommand(id),
                    cancellationToken);

                return result.Match(
                    onSuccess: () => Results.NoContent(),
                    onFailure: error => Results.Problem(ErrorToProblemDetailsMapper.Map(error)));
            })
            .RequirePermission(PermissionCatalog.MerchantsManage.Code)
            .HasApiVersion(ApiVersions.V1)
            .WithName("ActivateMerchant")
            .WithTags("Merchants")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);

        app.NewVersionedApi("Merchants")
            .MapPost("api/v{version:apiVersion}/merchants/{id:guid}/close", async (
                Guid id,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                Result result = await sender.Send(
                    new CloseMerchantCommand(id),
                    cancellationToken);

                return result.Match(
                    onSuccess: () => Results.NoContent(),
                    onFailure: error => Results.Problem(ErrorToProblemDetailsMapper.Map(error)));
            })
            .RequirePermission(PermissionCatalog.MerchantsManage.Code)
            .HasApiVersion(ApiVersions.V1)
            .WithName("CloseMerchant")
            .WithTags("Merchants")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);

        return app;
    }

    /// <summary>
    /// The client-controlled merchant registration payload.
    /// </summary>
    public sealed class RegisterMerchantRequest
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RegisterMerchantRequest"/> class.
        /// </summary>
        /// <param name="name">The merchant display name.</param>
        /// <param name="street">The street address.</param>
        /// <param name="city">The city name.</param>
        /// <param name="stateOrProvince">The optional state or province.</param>
        /// <param name="postalCode">The optional postal code.</param>
        /// <param name="country">The country name.</param>
        /// <param name="phone">The contact phone number.</param>
        public RegisterMerchantRequest(
            string name,
            string street,
            string city,
            string? stateOrProvince,
            string? postalCode,
            string country,
            string phone)
        {
            Name = name;
            Street = street;
            City = city;
            StateOrProvince = stateOrProvince;
            PostalCode = postalCode;
            Country = country;
            Phone = phone;
        }

        /// <summary>
        /// Gets the merchant display name.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the street address.
        /// </summary>
        public string Street { get; }

        /// <summary>
        /// Gets the city name.
        /// </summary>
        public string City { get; }

        /// <summary>
        /// Gets the optional state or province.
        /// </summary>
        public string? StateOrProvince { get; }

        /// <summary>
        /// Gets the optional postal code.
        /// </summary>
        public string? PostalCode { get; }

        /// <summary>
        /// Gets the country name.
        /// </summary>
        public string Country { get; }

        /// <summary>
        /// Gets the contact phone number.
        /// </summary>
        public string Phone { get; }
    }

    /// <summary>
    /// The payload confirming a merchant was registered.
    /// </summary>
    public sealed class MerchantCreatedResponse
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MerchantCreatedResponse"/> class.
        /// </summary>
        /// <param name="merchantId">The created merchant identifier.</param>
        /// <param name="name">The merchant display name.</param>
        public MerchantCreatedResponse(Guid merchantId, string name)
        {
            MerchantId = merchantId;
            Name = name;
        }

        /// <summary>
        /// Gets the created merchant identifier.
        /// </summary>
        public Guid MerchantId { get; }

        /// <summary>
        /// Gets the merchant display name.
        /// </summary>
        public string Name { get; }
    }

    /// <summary>
    /// The client-controlled merchant profile update payload.
    /// </summary>
    public sealed class UpdateMerchantProfileRequest
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="UpdateMerchantProfileRequest"/> class.
        /// </summary>
        /// <param name="name">The updated merchant display name.</param>
        /// <param name="street">The updated street address.</param>
        /// <param name="city">The updated city name.</param>
        /// <param name="stateOrProvince">The updated optional state or province.</param>
        /// <param name="postalCode">The updated optional postal code.</param>
        /// <param name="country">The updated country name.</param>
        /// <param name="phone">The updated contact phone number.</param>
        public UpdateMerchantProfileRequest(
            string name,
            string street,
            string city,
            string? stateOrProvince,
            string? postalCode,
            string country,
            string phone)
        {
            Name = name;
            Street = street;
            City = city;
            StateOrProvince = stateOrProvince;
            PostalCode = postalCode;
            Country = country;
            Phone = phone;
        }

        /// <summary>
        /// Gets the updated merchant display name.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the updated street address.
        /// </summary>
        public string Street { get; }

        /// <summary>
        /// Gets the updated city name.
        /// </summary>
        public string City { get; }

        /// <summary>
        /// Gets the updated optional state or province.
        /// </summary>
        public string? StateOrProvince { get; }

        /// <summary>
        /// Gets the updated optional postal code.
        /// </summary>
        public string? PostalCode { get; }

        /// <summary>
        /// Gets the updated country name.
        /// </summary>
        public string Country { get; }

        /// <summary>
        /// Gets the updated contact phone number.
        /// </summary>
        public string Phone { get; }
    }
}
