using LastHour.Api.ProblemDetails;
using LastHour.Api.Security.Authorization;
using LastHour.Api.Versioning;
using LastHour.BuildingBlocks.Application.Models;
using LastHour.BuildingBlocks.Infrastructure.Persistence.Outbox.Commands;
using LastHour.BuildingBlocks.Infrastructure.Persistence.Outbox.Queries;
using LastHour.BuildingBlocks.SharedKernel.Results;
using LastHour.Modules.Identity.Application.Permissions;
using MediatR;

namespace LastHour.Api.Endpoints.Outbox;

/// <summary>
/// Maps administrative outbox operational endpoints onto the route table.
/// </summary>
public static class OutboxEndpoints
{
    /// <summary>
    /// Maps the outbox management and diagnostic query endpoints onto the route table.
    /// </summary>
    /// <param name="app">The endpoint route builder to extend.</param>
    /// <returns>The same endpoint route builder, so calls can be chained.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="app"/> is <see langword="null"/>.</exception>
    public static IEndpointRouteBuilder MapOutboxEndpoints(this IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.NewVersionedApi("Outbox")
            .MapGet("api/v{version:apiVersion}/outbox", async (
                OutboxMessageStatus? status,
                string? eventType,
                DateTime? occurredFromUtc,
                DateTime? occurredToUtc,
                int? pageNumber,
                int? pageSize,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                Result<PagedResult<OutboxMessageSummaryDto>> result = await sender.Send(
                    new GetOutboxMessagesQuery(
                        status,
                        eventType,
                        occurredFromUtc,
                        occurredToUtc,
                        pageNumber ?? 1,
                        pageSize ?? GetOutboxMessagesQuery.DefaultPageSize),
                    cancellationToken);

                return result.Match(
                    onSuccess: paged => Results.Ok(paged),
                    onFailure: error => Results.Problem(ErrorToProblemDetailsMapper.Map(error)));
            })
            .RequirePermission(PermissionCatalog.OutboxRead.Code)
            .HasApiVersion(ApiVersions.V1)
            .WithName("GetOutboxMessages")
            .WithTags("Outbox")
            .Produces<PagedResult<OutboxMessageSummaryDto>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden);

        app.NewVersionedApi("Outbox")
            .MapGet("api/v{version:apiVersion}/outbox/{id:guid}", async (
                Guid id,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                Result<OutboxMessageDetailDto> result = await sender.Send(
                    new GetOutboxMessageByIdQuery(id),
                    cancellationToken);

                return result.Match(
                    onSuccess: detail => Results.Ok(detail),
                    onFailure: error => Results.Problem(ErrorToProblemDetailsMapper.Map(error)));
            })
            .RequirePermission(PermissionCatalog.OutboxRead.Code)
            .HasApiVersion(ApiVersions.V1)
            .WithName("GetOutboxMessageById")
            .WithTags("Outbox")
            .Produces<OutboxMessageDetailDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);

        app.NewVersionedApi("Outbox")
            .MapPost("api/v{version:apiVersion}/outbox/{id:guid}/requeue", async (
                Guid id,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                Result result = await sender.Send(new RequeueOutboxMessageCommand(id), cancellationToken);

                return result.Match(
                    onSuccess: () => Results.NoContent(),
                    onFailure: error => Results.Problem(ErrorToProblemDetailsMapper.Map(error)));
            })
            .RequirePermission(PermissionCatalog.OutboxManage.Code)
            .HasApiVersion(ApiVersions.V1)
            .WithName("RequeueOutboxMessage")
            .WithTags("Outbox")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        return app;
    }
}
