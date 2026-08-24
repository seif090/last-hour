using FluentValidation;

namespace LastHour.Modules.Notifications.Application.Queries;

/// <summary>
/// Validates the structural shape of a notifications query request.
/// </summary>
public sealed class GetMyNotificationsQueryValidator : AbstractValidator<GetMyNotificationsQuery>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GetMyNotificationsQueryValidator"/> class.
    /// </summary>
    public GetMyNotificationsQueryValidator()
    {
        RuleFor(query => query.UserId)
            .NotEmpty()
            .WithErrorCode("UserIdRequired")
            .WithMessage("User identifier is required.");

        RuleFor(query => query.PageNumber)
            .GreaterThanOrEqualTo(1)
            .WithErrorCode("PageNumberOutOfRange")
            .WithMessage("Page number must be at least 1.");

        RuleFor(query => query.PageSize)
            .InclusiveBetween(1, GetMyNotificationsQuery.MaxPageSize)
            .WithErrorCode("PageSizeOutOfRange")
            .WithMessage($"Page size must be between 1 and {GetMyNotificationsQuery.MaxPageSize}.");
    }
}
