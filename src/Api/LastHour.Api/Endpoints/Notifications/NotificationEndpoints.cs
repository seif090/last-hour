using System.Security.Claims;
using LastHour.Api.ProblemDetails;
using LastHour.Api.Security.Authentication;
using LastHour.Api.Versioning;
using LastHour.BuildingBlocks.Application.Models;
using LastHour.BuildingBlocks.SharedKernel.Results;
using LastHour.Modules.Notifications.Application.Commands;
using LastHour.Modules.Notifications.Application.Models;
using LastHour.Modules.Notifications.Application.Queries;
using MediatR;

namespace LastHour.Api.Endpoints.Notifications;

/// <summary>
/// Maps the notifications feature surface: querying authenticated user notifications,
/// marking single notification as read, and marking all notifications as read.
/// </summary>
public static class NotificationEndpoints
{
    /// <summary>
    /// Maps the notification endpoints onto the route table.
    /// </summary>
    /// <param name="app">The endpoint route builder to extend.</param>
    /// <returns>The same endpoint route builder, so calls can be chained.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="app"/> is <see langword="null"/>.</exception>
    public static IEndpointRouteBuilder MapNotificationEndpoints(this IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.NewVersionedApi("Notifications")
            .MapGet("api/v{version:apiVersion}/notifications", async (
                bool? isRead,
                string? type,
                int? page,
                int? pageNumber,
                int? pageSize,
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

                Result<PagedResult<NotificationDto>> result = await sender.Send(
                    new GetMyNotificationsQuery(
                        userId.Value,
                        isRead,
                        type,
                        page ?? pageNumber ?? 1,
                        pageSize ?? GetMyNotificationsQuery.DefaultPageSize),
                    cancellationToken);

                return result.Match(
                    onSuccess: paged => Results.Ok(paged),
                    onFailure: error => Results.Problem(ErrorToProblemDetailsMapper.Map(error)));
            })
            .RequireAuthorization()
            .HasApiVersion(ApiVersions.V1)
            .WithName("GetMyNotifications")
            .WithTags("Notifications")
            .Produces<PagedResult<NotificationDto>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        app.NewVersionedApi("Notifications")
            .MapPut("api/v{version:apiVersion}/notifications/{id:guid}/read", async (
                Guid id,
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
                    new MarkNotificationAsReadCommand(id, userId.Value),
                    cancellationToken);

                return result.Match(
                    onSuccess: () => Results.NoContent(),
                    onFailure: error => Results.Problem(ErrorToProblemDetailsMapper.Map(error)));
            })
            .RequireAuthorization()
            .HasApiVersion(ApiVersions.V1)
            .WithName("MarkNotificationAsRead")
            .WithTags("Notifications")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound);

        app.NewVersionedApi("Notifications")
            .MapPatch("api/v{version:apiVersion}/notifications/{id:guid}/read", async (
                Guid id,
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
                    new MarkNotificationAsReadCommand(id, userId.Value),
                    cancellationToken);

                return result.Match(
                    onSuccess: () => Results.NoContent(),
                    onFailure: error => Results.Problem(ErrorToProblemDetailsMapper.Map(error)));
            })
            .RequireAuthorization()
            .HasApiVersion(ApiVersions.V1)
            .WithName("PatchNotificationAsRead")
            .WithTags("Notifications")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound);

        app.NewVersionedApi("Notifications")
            .MapPost("api/v{version:apiVersion}/notifications/read-all", async (
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

                Result<MarkAllNotificationsAsReadResponse> result = await sender.Send(
                    new MarkAllNotificationsAsReadCommand(userId.Value),
                    cancellationToken);

                return result.Match(
                    onSuccess: response => Results.Ok(response),
                    onFailure: error => Results.Problem(ErrorToProblemDetailsMapper.Map(error)));
            })
            .RequireAuthorization()
            .HasApiVersion(ApiVersions.V1)
            .WithName("MarkAllNotificationsAsRead")
            .WithTags("Notifications")
            .Produces<MarkAllNotificationsAsReadResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        app.NewVersionedApi("Notifications")
            .MapPatch("api/v{version:apiVersion}/notifications/read-all", async (
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

                Result<MarkAllNotificationsAsReadResponse> result = await sender.Send(
                    new MarkAllNotificationsAsReadCommand(userId.Value),
                    cancellationToken);

                return result.Match(
                    onSuccess: response => Results.Ok(response),
                    onFailure: error => Results.Problem(ErrorToProblemDetailsMapper.Map(error)));
            })
            .RequireAuthorization()
            .HasApiVersion(ApiVersions.V1)
            .WithName("PatchAllNotificationsAsRead")
            .WithTags("Notifications")
            .Produces<MarkAllNotificationsAsReadResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        app.NewVersionedApi("Notifications")
            .MapGet("api/v{version:apiVersion}/notification-preferences", async (
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

                Result<IReadOnlyList<NotificationPreferenceDto>> result = await sender.Send(
                    new GetMyNotificationPreferencesQuery(userId.Value),
                    cancellationToken);

                return result.Match(
                    onSuccess: preferences => Results.Ok(preferences),
                    onFailure: error => Results.Problem(ErrorToProblemDetailsMapper.Map(error)));
            })
            .RequireAuthorization()
            .HasApiVersion(ApiVersions.V1)
            .WithName("GetMyNotificationPreferences")
            .WithTags("Notifications")
            .Produces<IReadOnlyList<NotificationPreferenceDto>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        app.NewVersionedApi("Notifications")
            .MapPut("api/v{version:apiVersion}/notification-preferences/{notificationType}/{channel}", async (
                string notificationType,
                string channel,
                UpdatePreferenceRequest request,
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

                if (!Enum.TryParse<Modules.Notifications.Domain.Notifications.DeliveryChannel>(channel, ignoreCase: true, out var parsedChannel))
                {
                    return Results.Problem(ErrorToProblemDetailsMapper.Map(Error.Validation(
                        "NotificationPreference.InvalidChannel",
                        $"Delivery channel '{channel}' is invalid.")));
                }

                Result result = await sender.Send(
                    new UpdateNotificationPreferenceCommand(userId.Value, notificationType, parsedChannel, request.isEnabled),
                    cancellationToken);

                return result.Match(
                    onSuccess: () => Results.NoContent(),
                    onFailure: error => Results.Problem(ErrorToProblemDetailsMapper.Map(error)));
            })
            .RequireAuthorization()
            .HasApiVersion(ApiVersions.V1)
            .WithName("UpdateNotificationPreference")
            .WithTags("Notifications")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        app.NewVersionedApi("Notifications")
            .MapDelete("api/v{version:apiVersion}/notification-preferences/{notificationType}/{channel}", async (
                string notificationType,
                string channel,
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

                if (!Enum.TryParse<Modules.Notifications.Domain.Notifications.DeliveryChannel>(channel, ignoreCase: true, out var parsedChannel))
                {
                    return Results.Problem(ErrorToProblemDetailsMapper.Map(Error.Validation(
                        "NotificationPreference.InvalidChannel",
                        $"Delivery channel '{channel}' is invalid.")));
                }

                Result result = await sender.Send(
                    new DisableNotificationPreferenceCommand(userId.Value, notificationType, parsedChannel),
                    cancellationToken);

                return result.Match(
                    onSuccess: () => Results.NoContent(),
                    onFailure: error => Results.Problem(ErrorToProblemDetailsMapper.Map(error)));
            })
            .RequireAuthorization()
            .HasApiVersion(ApiVersions.V1)
            .WithName("DisableNotificationPreference")
            .WithTags("Notifications")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        app.NewVersionedApi("Notifications")
            .MapGet("api/v{version:apiVersion}/notification-templates", async (
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                Result<IReadOnlyList<NotificationTemplateDto>> result = await sender.Send(
                    new GetNotificationTemplatesQuery(),
                    cancellationToken);

                return result.Match(
                    onSuccess: templates => Results.Ok(templates),
                    onFailure: error => Results.Problem(ErrorToProblemDetailsMapper.Map(error)));
            })
            .RequireAuthorization()
            .HasApiVersion(ApiVersions.V1)
            .WithName("GetNotificationTemplates")
            .WithTags("Notifications")
            .Produces<IReadOnlyList<NotificationTemplateDto>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        return app;
    }
}
