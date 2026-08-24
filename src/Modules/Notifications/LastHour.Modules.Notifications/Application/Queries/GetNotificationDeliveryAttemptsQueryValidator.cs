using FluentValidation;

namespace LastHour.Modules.Notifications.Application.Queries;

/// <summary>
/// Validator for <see cref="GetNotificationDeliveryAttemptsQuery"/>.
/// </summary>
public sealed class GetNotificationDeliveryAttemptsQueryValidator : AbstractValidator<GetNotificationDeliveryAttemptsQuery>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GetNotificationDeliveryAttemptsQueryValidator"/> class.
    /// </summary>
    public GetNotificationDeliveryAttemptsQueryValidator()
    {
        RuleFor(q => q.DeliveryId)
            .NotEmpty()
            .WithMessage("DeliveryId cannot be empty.");
    }
}
