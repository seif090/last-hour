using FluentValidation;

namespace LastHour.Modules.Notifications.Application.Queries;

/// <summary>
/// Validator for <see cref="GetNotificationDeliveryAuditQuery"/>.
/// </summary>
public sealed class GetNotificationDeliveryAuditQueryValidator : AbstractValidator<GetNotificationDeliveryAuditQuery>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GetNotificationDeliveryAuditQueryValidator"/> class.
    /// </summary>
    public GetNotificationDeliveryAuditQueryValidator()
    {
        RuleFor(q => q.DeliveryId)
            .NotEmpty()
            .WithMessage("DeliveryId cannot be empty.");
    }
}
