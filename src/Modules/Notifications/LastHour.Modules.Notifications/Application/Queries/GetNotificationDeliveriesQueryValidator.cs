using FluentValidation;

namespace LastHour.Modules.Notifications.Application.Queries;

/// <summary>
/// Validator for <see cref="GetNotificationDeliveriesQuery"/>.
/// </summary>
public sealed class GetNotificationDeliveriesQueryValidator : AbstractValidator<GetNotificationDeliveriesQuery>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GetNotificationDeliveriesQueryValidator"/> class.
    /// </summary>
    public GetNotificationDeliveriesQueryValidator()
    {
        RuleFor(q => q.PageNumber)
            .GreaterThanOrEqualTo(1)
            .WithMessage("Page number must be greater than or equal to 1.");

        RuleFor(q => q.PageSize)
            .InclusiveBetween(1, GetNotificationDeliveriesQuery.MaxPageSize)
            .WithMessage($"Page size must be between 1 and {GetNotificationDeliveriesQuery.MaxPageSize}.");

        RuleFor(q => q)
            .Must(q => !q.CreatedFromUtc.HasValue || !q.CreatedToUtc.HasValue || q.CreatedFromUtc.Value <= q.CreatedToUtc.Value)
            .WithMessage("CreatedFromUtc cannot be later than CreatedToUtc.");
    }
}
