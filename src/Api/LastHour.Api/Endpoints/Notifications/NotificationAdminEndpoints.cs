using System.Security.Claims;
using LastHour.Api.Endpoints.Notifications.Requests;
using LastHour.Api.ProblemDetails;
using LastHour.Api.Security.Authentication;
using LastHour.Api.Security.Authorization;
using LastHour.Api.Versioning;
using LastHour.BuildingBlocks.Application.Models;
using LastHour.BuildingBlocks.SharedKernel.Results;
using LastHour.Modules.Identity.Application.Permissions;
using LastHour.Modules.Notifications.Application.Commands;
using LastHour.Modules.Notifications.Application.Models;
using LastHour.Modules.Notifications.Application.Queries;
using LastHour.Modules.Notifications.Domain.Notifications;
using MediatR;

namespace LastHour.Api.Endpoints.Notifications;

/// <summary>
/// Maps administrative notification delivery operational endpoints onto the route table.
/// </summary>
public static class NotificationAdminEndpoints
{
    /// <summary>
    /// Maps administrative query and requeue endpoints onto the route table.
    /// </summary>
    public static IEndpointRouteBuilder MapNotificationAdminEndpoints(this IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        RouteGroupBuilder group = app.NewVersionedApi("NotificationAdmin")
            .MapGroup("api/v{version:apiVersion}/admin/notification-deliveries")
            .HasApiVersion(ApiVersions.V1)
            .WithTags("NotificationAdmin");

        group.MapGet(string.Empty, async (
            DeliveryStatus? status,
            DeliveryChannel? channel,
            Guid? notificationId,
            DateTime? createdFromUtc,
            DateTime? createdToUtc,
            int? page,
            int? pageNumber,
            int? pageSize,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            int effectivePage = pageNumber ?? page ?? 1;
            int effectivePageSize = pageSize ?? GetNotificationDeliveriesQuery.DefaultPageSize;

            Result<PagedResult<NotificationDeliverySummaryDto>> result = await sender.Send(
                new GetNotificationDeliveriesQuery(
                    status,
                    channel,
                    notificationId,
                    createdFromUtc,
                    createdToUtc,
                    effectivePage,
                    effectivePageSize),
                cancellationToken);

            return result.Match(
                onSuccess: paged => Results.Ok(paged),
                onFailure: error => Results.Problem(ErrorToProblemDetailsMapper.Map(error)));
        })
        .RequirePermission(PermissionCatalog.NotificationsRead.Code)
        .WithName("GetNotificationDeliveriesAdmin")
        .Produces<PagedResult<NotificationDeliverySummaryDto>>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden);

        group.MapGet("{deliveryId:guid}", async (
            Guid deliveryId,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            Result<NotificationDeliveryDetailDto> result = await sender.Send(
                new GetNotificationDeliveryDetailsQuery(deliveryId),
                cancellationToken);

            return result.Match(
                onSuccess: detail => Results.Ok(detail),
                onFailure: error => Results.Problem(ErrorToProblemDetailsMapper.Map(error)));
        })
        .RequirePermission(PermissionCatalog.NotificationsRead.Code)
        .WithName("GetNotificationDeliveryDetailsAdmin")
        .Produces<NotificationDeliveryDetailDto>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapGet("{deliveryId:guid}/attempts", async (
            Guid deliveryId,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            Result<IReadOnlyCollection<NotificationDeliveryAttemptDto>> result = await sender.Send(
                new GetNotificationDeliveryAttemptsQuery(deliveryId),
                cancellationToken);

            return result.Match(
                onSuccess: attempts => Results.Ok(attempts),
                onFailure: error => Results.Problem(ErrorToProblemDetailsMapper.Map(error)));
        })
        .RequirePermission(PermissionCatalog.NotificationsRead.Code)
        .WithName("GetNotificationDeliveryAttemptsAdmin")
        .Produces<IReadOnlyCollection<NotificationDeliveryAttemptDto>>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden);

        group.MapGet("{deliveryId:guid}/audit", async (
            Guid deliveryId,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            Result<IReadOnlyCollection<NotificationDeliveryAuditDto>> result = await sender.Send(
                new GetNotificationDeliveryAuditQuery(deliveryId),
                cancellationToken);

            return result.Match(
                onSuccess: audit => Results.Ok(audit),
                onFailure: error => Results.Problem(ErrorToProblemDetailsMapper.Map(error)));
        })
        .RequirePermission(PermissionCatalog.NotificationsRead.Code)
        .WithName("GetNotificationDeliveryAuditAdmin")
        .Produces<IReadOnlyCollection<NotificationDeliveryAuditDto>>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden);

        group.MapPost("{deliveryId:guid}/requeue", async (
            Guid deliveryId,
            RequeueDeliveryRequest request,
            ClaimsPrincipal user,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            Guid? adminUserId = user.GetUserId();
            if (adminUserId is null)
            {
                return Results.Problem(ErrorToProblemDetailsMapper.Map(Error.Unauthorized(
                    "AuthenticationFailed",
                    "Administrative user context is required.")));
            }

            Result result = await sender.Send(
                new RequeueNotificationDeliveryCommand(
                    deliveryId,
                    adminUserId.Value,
                    request?.Reason ?? string.Empty,
                    request?.CorrelationId),
                cancellationToken);

            return result.Match(
                onSuccess: () => Results.NoContent(),
                onFailure: error => Results.Problem(ErrorToProblemDetailsMapper.Map(error)));
        })
        .RequirePermission(PermissionCatalog.NotificationsManage.Code)
        .WithName("RequeueNotificationDeliveryAdmin")
        .Produces(StatusCodes.Status204NoContent)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPost("requeue", async (
            BatchRequeueDeliveryRequest request,
            ClaimsPrincipal user,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            Guid? adminUserId = user.GetUserId();
            if (adminUserId is null)
            {
                return Results.Problem(ErrorToProblemDetailsMapper.Map(Error.Unauthorized(
                    "AuthenticationFailed",
                    "Administrative user context is required.")));
            }

            Result<BatchRequeueResponseDto> result = await sender.Send(
                new RequeueNotificationDeliveriesCommand(
                    request?.DeliveryIds ?? Array.Empty<Guid>(),
                    adminUserId.Value,
                    request?.Reason ?? string.Empty,
                    request?.CorrelationId),
                cancellationToken);

            return result.Match(
                onSuccess: response => Results.Ok(response),
                onFailure: error => Results.Problem(ErrorToProblemDetailsMapper.Map(error)));
        })
        .RequirePermission(PermissionCatalog.NotificationsManage.Code)
        .WithName("RequeueNotificationDeliveriesBatchAdmin")
        .Produces<BatchRequeueResponseDto>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden);

        return app;
    }
}
