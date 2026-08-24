using FluentValidation;

namespace LastHour.Modules.Notifications.Application.Queries;

/// <summary>
/// Validator for <see cref="GetNotificationDeliveryDetailsQuery"/>.
/// </summary>
public sealed class GetNotificationDeliveryDetailsQueryValidator : AbstractValidator<GetNotificationDeliveryDetailsQuery>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GetNotificationDeliveryDetailsQueryValidator"/> class.
    /// </summary>
    public GetNotificationDeliveryDetailsQueryValidator()
    {
        RuleFor(q => q.DeliveryId)
            .NotEmpty()
            .WithMessage("DeliveryId cannot be empty.");
    }
}
